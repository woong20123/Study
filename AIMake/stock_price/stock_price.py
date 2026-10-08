"""
미국 주식 현재가 조회 (yfinance)

사용법:
    python stock_price.py AAPL MSFT NVDA          # 한 번 조회
    python stock_price.py                          # 티커를 입력받아 조회(빈 줄이면 종료)
    python stock_price.py AAPL TSLA --watch 60     # 60초마다 반복 조회(Ctrl+C로 종료)
    python stock_price.py AAPL --csv prices.csv    # 결과를 CSV에 누적 저장
    python stock_price.py KO AAPL -d               # 최근 5년 배당수익률(배당금 ÷ 평균 주가)도 출력
    python stock_price.py AAPL --no-cache          # 캐시 무시하고 새로 조회

캐시: 조회 결과를 스크립트 폴더의 cache.json 에 저장하고 최대 1시간(--cache-ttl, 기본 3600초) 재사용한다.
      --watch 는 매번 새로 조회한다(캐시를 쓰면 1시간 동안 같은 값만 보이므로).
주식 분할: 주가·배당금은 현재 주식 수 기준으로 환산된 값이다(yfinance 분할 반영). 분할 이력은 배당 표에 표시하고,
      분할 직후 전일 종가가 분할 전 값으로 남아 있으면 분할 비율로 보정한다.

주의: yfinance는 Yahoo Finance의 비공식 라이브러리라 시세가 지연되거나 일시적으로 실패할 수 있다.
"""
import argparse
import csv
import json
import logging
import os
import ssl
import sys
import time
import unicodedata
from datetime import datetime, timedelta

import certifi
import pandas as pd
import yfinance as yf
from curl_cffi import requests as curl_requests

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
CA_BUNDLE = os.path.join(BASE_DIR, 'ca_bundle.pem')
CACHE_FILE = os.path.join(BASE_DIR, 'cache.json')
MAX_CACHE_TTL = 3600  # 캐시는 최대 1시간

# 분할 직후 전일 종가 보정: 변동률이 이 값(%)을 넘을 때만 분할 이력을 추가로 조회한다
SPLIT_CHECK_CHANGE_PCT = 30.0
# 이 기간(일) 안의 분할만 '전일 종가가 아직 분할 전 값일 수 있는' 분할로 본다
SPLIT_RECENT_DAYS = 5

# 없는 티커를 조회하면 yfinance가 ERROR 로그를 직접 찍는다. 결과 표에 사유를 따로 표시하므로 숨긴다.
logging.getLogger('yfinance').setLevel(logging.CRITICAL)


# ---------------------------------------------------------------------------
# 네트워크 세션
# ---------------------------------------------------------------------------
def make_session():
    """
    yfinance가 쓰는 curl_cffi 세션을 만든다.

    curl_cffi는 자체 CA 목록(certifi)만 믿기 때문에, 회사망처럼 SSL 검사 장비가 인증서를 바꿔 끼우는 환경에서는
    'SSL certificate problem: unable to get local issuer certificate'로 실패한다.
    Windows 인증서 저장소(ROOT/CA)의 인증서를 certifi 목록에 더한 묶음을 만들어 검증에 쓴다(검증을 끄지 않는다).
    """
    pems = []
    if hasattr(ssl, 'enum_certificates'):  # Windows 전용 API
        for store in ('ROOT', 'CA'):
            for der, encoding, _trust in ssl.enum_certificates(store):
                if encoding == 'x509_asn':
                    pems.append(ssl.DER_cert_to_PEM_cert(der))
    if not pems:
        return curl_requests.Session(impersonate='chrome')

    with open(certifi.where(), encoding='utf-8') as f:
        bundle = f.read() + '\n' + ''.join(pems)
    with open(CA_BUNDLE, 'w', encoding='utf-8') as f:
        f.write(bundle)
    return curl_requests.Session(impersonate='chrome', verify=CA_BUNDLE)


_session = None


def session():
    global _session
    if _session is None:
        _session = make_session()
    return _session


# ---------------------------------------------------------------------------
# 파일 캐시 (최대 1시간)
# ---------------------------------------------------------------------------
class Cache:
    """
    {"quote:AAPL": {"ts": epoch, "data": {...}}, "dividend:AAPL": {...}} 형태의 JSON 파일 캐시.
    실패 결과(error)는 저장하지 않는다. 손상된 파일은 무시하고 새로 만든다.
    """

    def __init__(self, path, ttl, enabled=True):
        self.path = path
        self.ttl = min(ttl, MAX_CACHE_TTL)
        self.enabled = enabled and self.ttl > 0
        self._data = self._load() if self.enabled else {}

    def _load(self):
        try:
            with open(self.path, encoding='utf-8') as f:
                data = json.load(f)
            return data if isinstance(data, dict) else {}
        except (OSError, ValueError):
            return {}

    def _save(self):
        # 만료된 항목은 저장할 때 정리한다. 쓰기는 임시 파일 → 교체로 원자적으로 한다.
        now = time.time()
        self._data = {k: v for k, v in self._data.items() if now - v.get('ts', 0) < MAX_CACHE_TTL}
        tmp = self.path + '.tmp'
        with open(tmp, 'w', encoding='utf-8') as f:
            json.dump(self._data, f, ensure_ascii=False)
        os.replace(tmp, self.path)

    def get(self, kind, symbol):
        """유효한 캐시면 (data, 경과초)를, 없거나 만료면 (None, None)을 돌려준다."""
        if not self.enabled:
            return None, None
        entry = self._data.get(f'{kind}:{symbol}')
        if not entry:
            return None, None
        age = time.time() - entry.get('ts', 0)
        if age < 0 or age >= self.ttl:
            return None, None
        return entry['data'], age

    def put(self, kind, symbol, data):
        if not self.enabled or 'error' in data:
            return
        self._data[f'{kind}:{symbol}'] = {'ts': time.time(), 'data': data}
        self._save()

    def invalidate(self, symbol):
        if not self.enabled:
            return
        keys = [k for k in self._data if k.split(':', 1)[1] == symbol]
        for k in keys:
            del self._data[k]
        if keys:
            self._save()


# ---------------------------------------------------------------------------
# 주식 분할
# ---------------------------------------------------------------------------
def split_ratio_text(ratio):
    """yfinance Stock Splits 값(새 주식 수 ÷ 기존 주식 수)을 '10:1' 같은 표기로 바꾼다."""
    if ratio >= 1:
        return f'{ratio:g}:1'
    return f'1:{1 / ratio:g} (역분할)'


def adjust_prev_close_for_split(price, prev_close, splits, now):
    """
    분할 직후 전일 종가가 분할 전 기준으로 남아 있으면 분할 비율로 나눠 현재 주식 수 기준으로 맞춘다.

    splits: [(datetime, ratio), ...]  ratio = 새 주식 수 ÷ 기존 주식 수 (10:1 분할이면 10, 1:10 역분할이면 0.1)
    반환: (보정된 전일 종가, 적용한 분할 또는 None)

    보정 조건: 최근 SPLIT_RECENT_DAYS 일 안의 분할이 있고, 보정 후 변동률이 보정 전보다 0에 가까울 때만 보정한다.
    (yfinance가 이미 보정한 값이면 나누면 오히려 멀어지므로 건드리지 않는다)
    """
    if not prev_close or not splits:
        return prev_close, None
    recent = [(d, r) for d, r in splits if r and timedelta(0) <= now - d <= timedelta(days=SPLIT_RECENT_DAYS)]
    if not recent:
        return prev_close, None
    split_date, ratio = max(recent, key=lambda x: x[0])
    adjusted = prev_close / ratio
    if abs(price / adjusted - 1) < abs(price / prev_close - 1):
        return adjusted, (split_date, ratio)
    return prev_close, None


def recent_splits(ticker):
    """티커의 분할 이력을 [(naive datetime, ratio)]로 돌려준다."""
    s = ticker.splits
    return [(d.tz_localize(None).to_pydatetime() if d.tzinfo else d.to_pydatetime(), float(r)) for d, r in s.items()]


# ---------------------------------------------------------------------------
# 시세
# ---------------------------------------------------------------------------
def error_message(e):
    msg = str(e).splitlines()[0][:80] if str(e) else type(e).__name__
    if 'SSL certificate' in msg:
        return 'SSL 인증서 검증 실패(회사망 프록시 CA 확인 필요)'
    if isinstance(e, (KeyError, TypeError, ValueError)):
        return '시세 없음(티커 확인 필요)'
    return msg


def fetch_quote(symbol):
    """티커 하나의 시세를 dict로 돌려준다. 실패하면 error 키에 이유를 담는다."""
    try:
        t = yf.Ticker(symbol, session=session())
        # fast_info.get()은 camelCase 키(lastPrice)만 받으므로 속성으로 읽는다
        info = t.fast_info
        price = info.last_price
        prev = info.previous_close
        if price is None or price != price:  # None 또는 NaN → 없는 티커일 가능성이 높다
            return {'symbol': symbol, 'error': '시세 없음(티커 확인 필요)'}

        split_note = None
        if prev and abs(price / prev - 1) * 100 >= SPLIT_CHECK_CHANGE_PCT:
            # 변동이 비정상적으로 크면 분할 직후일 수 있다 → 분할 이력을 확인해 전일 종가를 보정
            prev, applied = adjust_prev_close_for_split(price, prev, recent_splits(t), datetime.now())
            if applied:
                split_note = f"{applied[0]:%Y-%m-%d} {split_ratio_text(applied[1])} 분할로 전일 종가 보정"

        change = price - prev if prev else None
        return {
            'symbol': symbol,
            'price': price,
            'prev_close': prev,
            'change': change,
            'change_pct': change / prev * 100 if prev else None,
            'currency': info.currency or '',
            'exchange': info.exchange or '',
            'split_note': split_note,
            'fetched_at': time.time(),
        }
    except Exception as e:  # 없는 티커·네트워크 오류는 티커 단위로 격리한다
        return {'symbol': symbol, 'error': error_message(e)}


# ---------------------------------------------------------------------------
# 배당수익률
# ---------------------------------------------------------------------------
def fetch_dividend_yield(symbol, years=5):
    """
    최근 years년의 배당수익률(배당금 ÷ 주가)을 1년 구간별로 계산한다.

    - 구간: 오늘부터 1년씩 거슬러 올라간 구간(달력 연도가 아님 → 진행 중인 올해 때문에 왜곡되지 않는다)
    - 구간 수익률 = 구간 내 배당금 합계 ÷ 구간 일별 종가 평균
    - 주가는 auto_adjust=False 종가(분할만 반영, 배당 미반영)를 쓴다. 배당 반영 수정주가를 쓰면 과거 주가가
      배당만큼 깎여 수익률이 부풀려진다. 배당금은 같은 history 의 Dividends 열을 써서 기준을 맞춘다.
    - 주식 분할: Close·Dividends 모두 현재 주식 수 기준으로 환산돼 있다(예: NVDA 2024 10:1 분할 전 배당 $0.04 → 0.004).
      같은 기준끼리 나누므로 분할이 있어도 수익률은 왜곡되지 않는다. 기간 내 분할 이력은 함께 돌려준다.
    """
    try:
        t = yf.Ticker(symbol, session=session())
        hist = t.history(period=f'{years + 1}y', auto_adjust=False)
        if hist.empty:
            return {'symbol': symbol, 'error': '주가 이력 없음(티커 확인 필요)'}

        end = hist.index.max()
        start = end - pd.DateOffset(years=years)
        periods = []
        for i in range(years):
            hi = end - pd.DateOffset(years=i)
            lo = end - pd.DateOffset(years=i + 1)
            part = hist[(hist.index > lo) & (hist.index <= hi)]
            if part.empty:
                break
            dividend = float(part['Dividends'].sum())
            avg_price = float(part['Close'].mean())
            periods.append({
                'from': (lo + pd.Timedelta(days=1)).strftime('%Y-%m-%d'),
                'to': hi.strftime('%Y-%m-%d'),
                'dividend': dividend,
                'count': int((part['Dividends'] > 0).sum()),
                'avg_price': avg_price,
                'yield_pct': dividend / avg_price * 100 if avg_price else None,
            })

        splits = []
        if 'Stock Splits' in hist.columns:
            s = hist.loc[(hist['Stock Splits'] > 0) & (hist.index > start), 'Stock Splits']
            splits = [{'date': d.strftime('%Y-%m-%d'), 'ratio': float(r)} for d, r in s.items()]

        yields = [p['yield_pct'] for p in periods if p['yield_pct'] is not None]
        yahoo_5y = None
        try:
            yahoo_5y = t.info.get('fiveYearAvgDividendYield')  # Yahoo 제공값(%, 대조용)
        except Exception:
            pass
        return {
            'symbol': symbol,
            'periods': periods,
            'splits': splits,
            'avg_yield_pct': sum(yields) / len(yields) if yields else None,
            'avg_dividend': sum(p['dividend'] for p in periods) / len(periods) if periods else None,
            'avg_price': sum(p['avg_price'] for p in periods) / len(periods) if periods else None,
            'yahoo_5y_pct': yahoo_5y,
            'years': len(periods),
            'fetched_at': time.time(),
        }
    except Exception as e:
        return {'symbol': symbol, 'error': error_message(e)}


# ---------------------------------------------------------------------------
# 출력
# ---------------------------------------------------------------------------
def fmt_num(value, digits=2, sign=False):
    if value is None:
        return '-'
    return f'{value:+,.{digits}f}' if sign else f'{value:,.{digits}f}'


def fmt_age(seconds):
    return f'{int(seconds // 60)}분 전' if seconds >= 60 else f'{int(seconds)}초 전'


def pad(text, width, right=False):
    """한글처럼 화면에서 2칸을 차지하는 문자를 고려해 폭을 맞춘다."""
    shown = sum(2 if unicodedata.east_asian_width(c) in 'WF' else 1 for c in text)
    space = ' ' * max(0, width - shown)
    return space + text if right else text + space


def print_table(quotes):
    now = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    print(f'\n[{now}]')
    header = (pad('티커', 8) + pad('현재가', 12, True) + pad('전일종가', 12, True) + pad('변동', 10, True)
              + pad('변동률', 9, True) + '  ' + pad('통화', 5) + pad('거래소', 8))
    print(header)
    print('-' * 70)
    notes = []
    for q in quotes:
        if 'error' in q:
            print(f"{q['symbol']:<8}  {q['error']}")
            continue
        pct = '-' if q['change_pct'] is None else f"{q['change_pct']:+.2f}%"
        print(f"{q['symbol']:<8}{fmt_num(q['price']):>12}{fmt_num(q['prev_close']):>12}"
              f"{fmt_num(q['change'], sign=True):>10}{pct:>9}  {q['currency']:<5}{q['exchange']:<8}")
        if q.get('cache_age') is not None:
            notes.append(f"{q['symbol']} 캐시({fmt_age(q['cache_age'])} 조회값)")
        if q.get('split_note'):
            notes.append(f"{q['symbol']} {q['split_note']}")
    for n in notes:
        print(f'  ※ {n}')
    sys.stdout.flush()  # --watch를 파이프·로그로 받을 때도 매 회차 바로 보이게


def print_dividend_table(result):
    print(f"\n[{result['symbol']}] 최근 {result.get('years', 0)}년 배당수익률 (배당금 합계 ÷ 구간 평균 주가)")
    if 'error' in result:
        print(f"  {result['error']}")
        return
    print('  ' + pad('구간', 25) + pad('배당횟수', 9, True) + pad('배당금', 10, True)
          + pad('평균주가', 12, True) + pad('수익률', 9, True))
    print('  ' + '-' * 65)
    for p in result['periods']:
        y = '-' if p['yield_pct'] is None else f"{p['yield_pct']:.2f}%"
        print(f"  {p['from'] + ' ~ ' + p['to']:<25}{p['count']:>9}{fmt_num(p['dividend'], 4):>10}"
              f"{fmt_num(p['avg_price']):>12}{y:>9}")
    print('  ' + '-' * 65)
    avg = '-' if result['avg_yield_pct'] is None else f"{result['avg_yield_pct']:.2f}%"
    print('  ' + pad(f"{result['years']}년 평균", 34) + f"{fmt_num(result['avg_dividend'], 4):>10}"
          f"{fmt_num(result['avg_price']):>12}{avg:>9}")
    if result.get('splits'):
        items = ', '.join(f"{s['date']} {split_ratio_text(s['ratio'])}" for s in result['splits'])
        print(f'  ※ 기간 내 주식 분할: {items} → 배당금·주가는 현재 주식 수 기준으로 환산된 값')
    if result['avg_dividend'] == 0:
        print('  ※ 이 기간 배당 지급 기록이 없습니다(무배당 종목)')
    if result.get('cache_age') is not None:
        print(f"  ※ 캐시({fmt_age(result['cache_age'])} 조회값)")
    if result['yahoo_5y_pct'] is not None:
        print(f"  (참고) Yahoo 5년 평균 배당수익률: {result['yahoo_5y_pct']:.2f}%")
    sys.stdout.flush()


def append_csv(path, quotes):
    fields = ['time', 'symbol', 'price', 'prev_close', 'change', 'change_pct', 'currency', 'exchange', 'error']
    new_file = not os.path.exists(path)
    now = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    with open(path, 'a', newline='', encoding='utf-8-sig') as f:
        w = csv.DictWriter(f, fieldnames=fields)
        if new_file:
            w.writeheader()
        for q in quotes:
            row = {k: q.get(k, '') for k in fields if k != 'time'}
            for k in ('price', 'prev_close', 'change', 'change_pct'):
                if isinstance(row[k], float):
                    row[k] = round(row[k], 4)
            w.writerow({'time': now, **row})


# ---------------------------------------------------------------------------
# 실행
# ---------------------------------------------------------------------------
def cached_fetch(cache, kind, symbol, fetch, use_cache=True):
    if use_cache:
        data, age = cache.get(kind, symbol)
        if data is not None:
            return {**data, 'cache_age': age}
    data = fetch(symbol)
    cache.put(kind, symbol, data)
    return data


def run_once(symbols, cache, csv_path=None, dividend=False, use_cache=True):
    quotes = [cached_fetch(cache, 'quote', s, fetch_quote, use_cache) for s in symbols]
    print_table(quotes)
    if csv_path:
        append_csv(csv_path, quotes)
    if dividend:
        for q in quotes:
            if 'error' in q:  # 없는 티커는 시세 표에서 이미 안내했다
                continue
            if q.get('split_note'):
                # 막 분할된 종목은 캐시된 배당 이력이 분할 전 기준일 수 있으므로 버리고 새로 받는다
                cache.invalidate(q['symbol'])
                cache.put('quote', q['symbol'], {k: v for k, v in q.items() if k != 'cache_age'})
            print_dividend_table(cached_fetch(cache, 'dividend', q['symbol'], fetch_dividend_yield))
    return quotes


def parse_symbols(text):
    return [s.strip().upper() for s in text.replace(',', ' ').split() if s.strip()]


def main():
    ap = argparse.ArgumentParser(description='yfinance로 미국 주식 현재가를 조회한다.')
    ap.add_argument('symbols', nargs='*', help='티커 목록 (예: AAPL MSFT). 생략하면 입력받는다')
    ap.add_argument('--watch', type=int, metavar='SEC', help='SEC초마다 반복 조회(캐시 미사용)')
    ap.add_argument('--csv', metavar='PATH', help='조회 결과를 CSV에 누적 저장')
    ap.add_argument('-d', '--dividend', action='store_true', help='최근 5년 배당수익률(배당금 ÷ 평균 주가)도 함께 출력')
    ap.add_argument('--cache-ttl', type=int, default=MAX_CACHE_TTL, metavar='SEC',
                    help=f'캐시 유효 시간(초, 최대 {MAX_CACHE_TTL})')
    ap.add_argument('--no-cache', action='store_true', help='캐시를 읽지도 쓰지도 않는다')
    args = ap.parse_args()

    if args.watch is not None and args.watch < 5:
        ap.error('--watch 간격은 5초 이상으로 지정하세요(과도한 요청 방지)')
    if args.watch is not None and args.dividend:
        ap.error('--dividend 는 --watch 와 함께 쓸 수 없습니다(5년 이력은 반복 조회할 필요가 없음)')
    if not 0 <= args.cache_ttl <= MAX_CACHE_TTL:
        ap.error(f'--cache-ttl 은 0~{MAX_CACHE_TTL}초 사이로 지정하세요(캐시는 최대 1시간)')

    cache = Cache(CACHE_FILE, args.cache_ttl, enabled=not args.no_cache)

    if not args.symbols:
        # 대화형 모드: 한 줄에 여러 티커(공백/쉼표 구분), 빈 줄이면 종료
        print('티커를 입력하세요 (예: AAPL MSFT, 빈 줄이면 종료)')
        while True:
            try:
                line = input('> ').strip()
            except (EOFError, KeyboardInterrupt):
                break
            if not line:
                break
            run_once(parse_symbols(line), cache, args.csv, args.dividend)
        return

    symbols = parse_symbols(' '.join(args.symbols))
    if args.watch is None:
        quotes = run_once(symbols, cache, args.csv, args.dividend)
        sys.exit(1 if all('error' in q for q in quotes) else 0)

    try:
        while True:
            # 반복 조회는 매번 새 값을 받는다(결과는 캐시에 저장해 다른 실행에서 재사용)
            run_once(symbols, cache, args.csv, use_cache=False)
            time.sleep(args.watch)
    except KeyboardInterrupt:
        print('\n종료합니다.')


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')
    main()
