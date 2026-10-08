"""
목표 주가 역산 → 분기별 매입 목표가

규칙
    목표 주가        = 목표 EPS × 목표 PER                       (목표 연도 말 기준)
    필요 주가 상승률 = 목표 수익률 − 최근 5년 평균 배당수익률   (연율, 입력 시점 값으로 고정)
    분기별 매입 목표가 = 목표 주가 ÷ (1 + 필요 주가 상승률) ^ (그 분기 기준일 → 목표일까지 남은 년수)
    → 매입 목표가 이하로 사면 '주가 상승 + 배당'으로 목표 수익률을 기대할 수 있다.

사용법
    python target_price.py set KO --year 2030 --eps 10.5 --per 18 --return 10   # 목표 저장 + 분기 일정 출력
    python target_price.py show                # 저장된 모든 목표: 이번 분기 매입 목표가 vs 현재가
    python target_price.py show KO --all       # KO의 전체 분기 일정
    python target_price.py list                # 저장된 목표 목록
    python target_price.py delete KO           # 목표 삭제

목표는 스크립트 폴더의 targets.json 에 저장한다. 시세·배당은 stock_price.py 를 재사용한다(1시간 캐시 포함).
"""
import argparse
import json
import os
import sys
from datetime import date, datetime

import stock_price as sp

TARGETS_FILE = os.path.join(sp.BASE_DIR, 'targets.json')
DAYS_PER_YEAR = 365.25


# ---------------------------------------------------------------------------
# 계산 (순수 함수, 네트워크 없음)
# ---------------------------------------------------------------------------
def quarter_of(d):
    return (d.month - 1) // 3 + 1


def quarter_start(year, q):
    return date(year, 3 * (q - 1) + 1, 1)


def quarter_schedule(input_date, target_date):
    """
    입력일이 속한 분기부터 목표일이 속한 분기까지 (분기 라벨, 기준일) 목록.
    첫 분기의 기준일은 입력일, 이후는 각 분기 첫날.
    """
    rows = []
    y, q = input_date.year, quarter_of(input_date)
    while True:
        start = quarter_start(y, q)
        if start > target_date:
            break
        rows.append((f'{y}Q{q}', max(start, input_date)))
        y, q = (y + 1, 1) if q == 4 else (y, q + 1)
    return rows


def buy_price(target_price, growth_pct, on, target_date):
    """기준일 on 에 사서 목표일에 target_price 가 되려면 허용되는 최대 매입가."""
    years_left = max((target_date - on).days, 0) / DAYS_PER_YEAR
    return target_price / (1 + growth_pct / 100) ** years_left


def build_target(symbol, year, eps, per, return_pct, dividend_yield_pct, input_date):
    target_date = date(year, 12, 31)
    if target_date <= input_date:
        raise ValueError(f'목표 연도({year})가 입력일({input_date}) 이후여야 합니다')
    return {
        'symbol': symbol,
        'input_date': input_date.isoformat(),
        'target_year': year,
        'target_date': target_date.isoformat(),
        'eps': eps,
        'per': per,
        'target_price': eps * per,
        'return_pct': return_pct,
        'dividend_yield_pct': dividend_yield_pct,          # 입력 시점 5년 평균(고정)
        'growth_pct': return_pct - dividend_yield_pct,     # 필요 주가 상승률
    }


def schedule_rows(t):
    input_date = date.fromisoformat(t['input_date'])
    target_date = date.fromisoformat(t['target_date'])
    return [(label, on, buy_price(t['target_price'], t['growth_pct'], on, target_date))
            for label, on in quarter_schedule(input_date, target_date)]


# ---------------------------------------------------------------------------
# 저장소
# ---------------------------------------------------------------------------
def load_targets():
    try:
        with open(TARGETS_FILE, encoding='utf-8') as f:
            return json.load(f)
    except (OSError, ValueError):
        return {}


def save_targets(targets):
    tmp = TARGETS_FILE + '.tmp'
    with open(tmp, 'w', encoding='utf-8') as f:
        json.dump(targets, f, ensure_ascii=False, indent=2)
    os.replace(tmp, TARGETS_FILE)


# ---------------------------------------------------------------------------
# 출력
# ---------------------------------------------------------------------------
def print_summary(t, current_div=None):
    print(f"\n[{t['symbol']}] {t['target_year']}년 목표 (입력일 {t['input_date']})")
    print(f"  목표 주가        = EPS {t['eps']:g} × PER {t['per']:g} = {t['target_price']:,.2f}  ({t['target_date']} 기준)")
    print(f"  필요 주가 상승률 = 목표 수익률 {t['return_pct']:g}% − 5년 평균 배당수익률 {t['dividend_yield_pct']:.2f}%"
          f" = 연 {t['growth_pct']:.2f}%")
    if current_div is not None and abs(current_div - t['dividend_yield_pct']) >= 0.01:
        print(f"  (참고) 현재 5년 평균 배당수익률은 {current_div:.2f}% — 일정은 입력 시점 값으로 고정")
    if t['growth_pct'] <= 0:
        print('  ※ 배당만으로 목표 수익률을 넘으므로 매입 목표가가 목표 주가보다 높게 나옵니다')


def print_schedule(t, current_price=None, today=None, show_all=True):
    today = today or date.today()
    rows = schedule_rows(t)
    cur_label = f'{today.year}Q{quarter_of(today)}'
    print('  ' + sp.pad('분기', 8) + sp.pad('기준일', 12) + sp.pad('매입 목표가', 13, True) + sp.pad('현재가 대비', 13, True))
    print('  ' + '-' * 48)
    shown = 0
    for label, on, price in rows:
        is_current = label == cur_label
        if not show_all and not is_current:
            continue
        gap = ''
        if is_current and current_price:
            gap = f'{(current_price / price - 1) * 100:+.1f}%'
        mark = ' ◀ 이번 분기' if is_current else ''
        print(f"  {label:<8}{on.isoformat():<12}{price:>13,.2f}{gap:>13}{mark}")
        shown += 1
    if not shown:
        print('  (이번 분기는 일정 범위 밖입니다. --all 로 전체 일정을 보세요)')
    if current_price:
        cur = next((p for label, _, p in rows if label == cur_label), None)
        if cur is not None:
            verdict = '매입 목표가 이하 → 매수 조건 충족' if current_price <= cur else '매입 목표가 초과 → 대기'
            print(f"  현재가 {current_price:,.2f} / 이번 분기 매입 목표가 {cur:,.2f} → {verdict}")


# ---------------------------------------------------------------------------
# 명령
# ---------------------------------------------------------------------------
def current_dividend_yield(cache, symbol):
    r = sp.cached_fetch(cache, 'dividend', symbol, sp.fetch_dividend_yield)
    if 'error' in r:
        raise RuntimeError(f"{symbol} 배당 데이터 조회 실패: {r['error']}")
    return r['avg_yield_pct'] or 0.0


def current_price(cache, symbol):
    q = sp.cached_fetch(cache, 'quote', symbol, sp.fetch_quote)
    return None if 'error' in q else q['price']


def cmd_set(args, cache):
    symbol = args.symbol.upper()
    div = args.dividend_yield if args.dividend_yield is not None else current_dividend_yield(cache, symbol)
    t = build_target(symbol, args.year, args.eps, args.per, args.ret, div, date.today())
    targets = load_targets()
    replaced = symbol in targets
    targets[symbol] = t
    save_targets(targets)
    print(f"{symbol} 목표를 {'갱신' if replaced else '저장'}했습니다 → {TARGETS_FILE}")
    print_summary(t)
    print_schedule(t, current_price(cache, symbol))


def cmd_show(args, cache):
    targets = load_targets()
    symbols = [s.upper() for s in args.symbols] or sorted(targets)
    if not symbols:
        print('저장된 목표가 없습니다. 먼저 set 으로 목표를 입력하세요.')
        return
    for s in symbols:
        t = targets.get(s)
        if not t:
            print(f'\n[{s}] 저장된 목표가 없습니다.')
            continue
        div_now = None
        try:
            div_now = current_dividend_yield(cache, s)
        except RuntimeError:
            pass
        print_summary(t, div_now)
        print_schedule(t, current_price(cache, s), show_all=args.all)


def cmd_list(args, cache):
    targets = load_targets()
    if not targets:
        print('저장된 목표가 없습니다.')
        return
    print(sp.pad('티커', 8) + sp.pad('목표연도', 9) + sp.pad('EPS', 8, True) + sp.pad('PER', 7, True)
          + sp.pad('목표주가', 11, True) + sp.pad('수익률', 8, True) + sp.pad('배당', 8, True) + '  입력일')
    for s, t in sorted(targets.items()):
        print(f"{s:<8}{t['target_year']:<9}{t['eps']:>8g}{t['per']:>7g}{t['target_price']:>11,.2f}"
              f"{t['return_pct']:>7g}%{t['dividend_yield_pct']:>7.2f}%  {t['input_date']}")


def cmd_delete(args, cache):
    targets = load_targets()
    s = args.symbol.upper()
    if targets.pop(s, None) is None:
        print(f'{s} 목표가 없습니다.')
        return
    save_targets(targets)
    print(f'{s} 목표를 삭제했습니다.')


def main():
    ap = argparse.ArgumentParser(description='목표 EPS·PER·수익률로 분기별 매입 목표가를 역산한다.')
    ap.add_argument('--no-cache', action='store_true', help='시세·배당 캐시를 쓰지 않는다')
    sub = ap.add_subparsers(dest='cmd', required=True)

    p = sub.add_parser('set', help='목표 저장 + 분기 일정 출력')
    p.add_argument('symbol')
    p.add_argument('--year', type=int, required=True, help='목표 연도(그해 12월 31일 기준)')
    p.add_argument('--eps', type=float, required=True, help='목표 연도 EPS')
    p.add_argument('--per', type=float, required=True, help='목표 PER')
    p.add_argument('--return', dest='ret', type=float, required=True, help='목표 연 수익률(%%)')
    p.add_argument('--dividend-yield', type=float, help='배당수익률(%%)을 직접 지정. 생략하면 최근 5년 평균을 조회')
    p.set_defaults(func=cmd_set)

    p = sub.add_parser('show', help='이번 분기 매입 목표가 vs 현재가')
    p.add_argument('symbols', nargs='*')
    p.add_argument('--all', action='store_true', help='전체 분기 일정 출력')
    p.set_defaults(func=cmd_show)

    p = sub.add_parser('list', help='저장된 목표 목록')
    p.set_defaults(func=cmd_list)

    p = sub.add_parser('delete', help='목표 삭제')
    p.add_argument('symbol')
    p.set_defaults(func=cmd_delete)

    args = ap.parse_args()
    if args.cmd == 'set':
        for name, v in (('eps', args.eps), ('per', args.per)):
            if v <= 0:
                ap.error(f'--{name} 는 0보다 커야 합니다')
        if args.year <= date.today().year - 1:
            ap.error('--year 는 올해 이후여야 합니다')

    cache = sp.Cache(sp.CACHE_FILE, sp.MAX_CACHE_TTL, enabled=not args.no_cache)
    try:
        args.func(args, cache)
    except (RuntimeError, ValueError) as e:
        print(f'오류: {e}', file=sys.stderr)
        sys.exit(1)


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')
    main()
