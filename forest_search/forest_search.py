"""숲나들e 월별 예약현황에서 토요일·공휴일(대체공휴일 포함)에 예약 가능한 '숙소'를 찾는다.

사용법
  python forest_search.py --login          # 최초 1회(또는 세션 만료 시) 브라우저 창에서 로그인
  python forest_search.py                  # 헤드리스로 탐색 (기본: 서울/인천/경기, 충북, 대전/충남)
  python forest_search.py --regions 1,3 --months 202611 --show

옵션
  --regions   시/도 값(쉼표 구분). 1=서울/인천/경기 2=강원 3=충북 4=대전/충남 5=전북 6=전남광주 7=대구/경북 8=부산/경남 9=제주
  --months    조회할 월(YYYYMM, 쉼표 구분). 생략하면 사이트가 제공하는 모든 월
  --include-wait  대기 예약(대N)도 결과에 포함
  --show      브라우저 창을 띄워서 실행 (디버깅용)
  --delay     조회 사이 대기 초 (기본 1.5초, 서버 부하 방지)

주의: 사이트 이용약관을 지켜 개인 확인 용도로만, 낮은 빈도로 사용한다.
"""
import argparse
import csv
import datetime as dt
import json
import os
import re
import shutil
import sys
import time

import holidays
from playwright.sync_api import sync_playwright, TimeoutError as PwTimeout

# exe(PyInstaller)로 실행하면 __file__이 임시 폴더라서 exe가 있는 폴더를 기준으로 한다.
FROZEN = getattr(sys, "frozen", False)
BASE_DIR = os.path.dirname(sys.executable if FROZEN else os.path.abspath(__file__))
PROFILE = os.path.join(BASE_DIR, "profile")
# 로그인 세션 쿠키는 만료일이 없어 브라우저 종료 시 사라지므로 별도 파일로 저장해 재사용한다.
STATE_FILE = os.path.join(BASE_DIR, "state.json")
URL = "https://www.foresttrip.go.kr/rep/or/sssn/monthRsrvtSmplStatus.do"
REGION_NAMES = {"1": "서울/인천/경기", "2": "강원", "3": "충북", "4": "대전/충남", "5": "전북",
                "6": "전남광주", "7": "대구/경북", "8": "부산/경남", "9": "제주"}
FACILITY_LABELS = ("숙소", "야영장")
PRIVATE_PREFIX = "[사립]"


DAY_KEYS = {"mon": 0, "tue": 1, "wed": 2, "thu": 3, "fri": 4, "sat": 5, "sun": 6}
DAY_NAMES = "월화수목금토일"


def get_disk_usage_info(target_path):
    """지정 경로의 디스크 사용량 정보(총용량, 사용량, 여유공간, 사용률)를 반환."""
    try:
        total, used, free = shutil.disk_usage(target_path)
        total_gb = total / (1024 ** 3)
        used_gb = used / (1024 ** 3)
        free_gb = free / (1024 ** 3)
        pct = (used / total * 100) if total else 0.0
        return {
            "path": os.path.abspath(target_path),
            "total_gb": total_gb,
            "used_gb": used_gb,
            "free_gb": free_gb,
            "pct": pct
        }
    except Exception:
        return {
            "path": os.path.abspath(target_path),
            "total_gb": 0.0,
            "used_gb": 0.0,
            "free_gb": 0.0,
            "pct": 0.0
        }


def target_dates(months, weekdays=(5,), with_holidays=True):
    """조회 월 범위의 지정 요일 + 공휴일(대체공휴일 포함). {date: 사유}"""
    years = sorted({int(m[:4]) for m in months})
    kr = holidays.KR(years=years)
    result = {}
    for m in months:
        y, mo = int(m[:4]), int(m[4:])
        d = dt.date(y, mo, 1)
        while d.month == mo:
            reasons = []
            if d.weekday() in weekdays:
                reasons.append(f"{DAY_NAMES[d.weekday()]}요일")
            if with_holidays and d in kr:
                reasons.append(kr.get(d))
            if reasons:
                result[d] = ", ".join(reasons)
            d += dt.timedelta(days=1)
    return result


def wait_idle(page, timeout=30000):
    """blockUI 로딩 오버레이가 사라질 때까지 대기."""
    page.wait_for_timeout(300)
    page.wait_for_function("() => !document.querySelector('.blockUI.blockOverlay')", timeout=timeout)


def select_and_wait_options(page, select_id, value, child_id, min_options=2, timeout=15000):
    page.select_option(f"#{select_id}", value)
    try:
        page.wait_for_function(
            f"() => document.querySelectorAll('#{child_id} option').length >= {min_options}", timeout=timeout)
    except PwTimeout:
        return False
    wait_idle(page)
    return True


def options_of(page, select_id):
    return page.eval_on_selector_all(
        f"#{select_id} option", "els => els.map(e => ({value: e.value, text: e.textContent.trim()}))")


def is_login_required(page):
    return page.locator("#fripPotForm").count() > 0 and page.locator("#srchSido").count() == 0


def do_login(ctx, page):
    page.goto(URL, wait_until="domcontentloaded")
    print("브라우저 창에서 로그인해 주세요. (최대 10분 대기)", flush=True)
    deadline = time.time() + 600
    while time.time() < deadline:
        time.sleep(3)
        try:
            if page.locator("#srchSido").count() > 0:
                ctx.storage_state(path=STATE_FILE)
                print(f"로그인 확인. 세션을 저장했습니다: {STATE_FILE}")
                return True
            if "monthRsrvtSmplStatus" not in page.url and page.locator("text=로그아웃").count() > 0:
                page.goto(URL, wait_until="domcontentloaded")
        except Exception:
            pass
    print("로그인 대기 시간 초과")
    return False


def search_month(page, month, include_wait, dates, max_capacity=0):
    """현재 선택된 휴양림·숙소로 해당 월을 조회하고 대상 날짜의 가능 객실을 반환."""
    page.select_option("#monthSelectBox", month)
    page.click("#searchBtn")
    try:
        page.wait_for_function(
            "() => document.querySelectorAll('#monthRsrvtList .list_left ul li').length > 0", timeout=30000)
    except PwTimeout:
        return []
    wait_idle(page, timeout=60000)
    page.wait_for_timeout(500)

    # 왼쪽 객실 목록(li: "[분류]객실명 (N인/면적)")과 오른쪽 날짜 표(tr)는 같은 순서로 붙는다.
    rows = page.evaluate("""() => {
        const lis = [...document.querySelectorAll('#monthRsrvtList .list_left ul li')];
        const trs = [...document.querySelectorAll('#dayListTbody tr')];
        return lis.map((li, i) => ({
            label: li.textContent.trim(),
            cells: trs[i] ? [...trs[i].querySelectorAll('span.apt_mark')].map(e => ({
                title: e.getAttribute('title') || '', text: e.textContent.trim()})) : []
        }));
    }""")
    found = []
    for row in rows:
        m = re.search(r"\((\d+)인", row["label"])
        capacity = int(m.group(1)) if m else None
        if max_capacity and capacity is not None and capacity > max_capacity:
            continue
        for c in row["cells"]:
            text = c["text"]
            if text == "예" or (include_wait and text.startswith("대")):
                # title = "{객실명} YYYY.MM.DD"
                name, _, ymd = c["title"].rpartition(" ")
                try:
                    day = dt.datetime.strptime(ymd, "%Y.%m.%d").date()
                except ValueError:
                    continue
                if day in dates:
                    found.append({"date": day, "reason": dates[day],
                                  "room": f"{name}({capacity}인)" if capacity else name,
                                  "status": "예약가능" if text == "예" else f"대기({text})"})
    return found


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--regions", default="1,3,4")
    ap.add_argument("--months", default="")
    ap.add_argument("--include-wait", action="store_true")
    ap.add_argument("--show", action="store_true")
    ap.add_argument("--login", action="store_true")
    ap.add_argument("--delay", type=float, default=1.5)
    ap.add_argument("--days", default="sat", help="대상 요일(쉼표 구분): mon,tue,wed,thu,fri,sat,sun")
    ap.add_argument("--no-holidays", action="store_true", help="공휴일·대체공휴일 제외")
    ap.add_argument("--max-capacity", type=int, default=5, help="기준인원 상한(기본 5 → 6인 이상 제외, 0=제한 없음)")
    ap.add_argument("--facility", default="숙소", help="숙박시설: 숙소, 야영장, all (쉼표 구분 가능)")
    ap.add_argument("--include-private", action="store_true", help="[사립] 휴양림도 포함")
    ap.add_argument("--max-forests", type=int, default=0, help="조회할 휴양림 수 상한(테스트용, 0=전체)")
    args = ap.parse_args()
    facilities = list(FACILITY_LABELS) if args.facility.strip().lower() == "all" else [x.strip() for x in args.facility.split(",") if x.strip() in FACILITY_LABELS]
    if not facilities:
        print(f"--facility 값이 올바르지 않습니다: {args.facility} (숙소, 야영장, all)"); sys.exit(1)
    weekdays = tuple(DAY_KEYS[x.strip().lower()] for x in args.days.split(",") if x.strip())

    with sync_playwright() as p:
        ctx = p.chromium.launch_persistent_context(
            PROFILE, channel="chrome", headless=not (args.show or args.login), locale="ko-KR")
        ctx.on("dialog", lambda d: (print("[알림]", d.message), _safe(d.accept)))
        page = ctx.pages[0] if ctx.pages else ctx.new_page()

        if not args.login and os.path.exists(STATE_FILE):
            with open(STATE_FILE, encoding="utf-8") as f:
                ctx.add_cookies(json.load(f).get("cookies", []))

        if args.login:
            ok = do_login(ctx, page)
            ctx.close()
            sys.exit(0 if ok else 1)

        page.goto(URL, wait_until="domcontentloaded")
        page.wait_for_timeout(2000)
        if is_login_required(page):
            # 세션이 없거나 만료됨 → 브라우저 창을 띄워 로그인 받은 뒤 그 창으로 이어서 조회
            print("로그인이 필요합니다. 브라우저 창을 엽니다.")
            ctx.close()
            ctx = p.chromium.launch_persistent_context(PROFILE, channel="chrome", headless=False, locale="ko-KR")
            ctx.on("dialog", lambda d: (print("[알림]", d.message), _safe(d.accept)))
            page = ctx.pages[0] if ctx.pages else ctx.new_page()
            if not do_login(ctx, page):
                ctx.close()
                sys.exit(2)
            page.goto(URL, wait_until="domcontentloaded")
            page.wait_for_timeout(2000)
        wait_idle(page)
        ctx.storage_state(path=STATE_FILE)  # 세션 연장분 반영

        all_months =[o["value"] for o in options_of(page, "monthSelectBox") if o["value"]]
        months = [m for m in args.months.split(",") if m] or all_months
        months = [m for m in months if m in all_months]
        dates = target_dates(months, weekdays, not args.no_holidays)
        print(f"조회 월: {', '.join(months)}")
        print("대상 날짜:", ", ".join(f"{d:%m/%d}({r})" for d, r in sorted(dates.items())))

        results = []
        searched = 0
        for region in [r.strip() for r in args.regions.split(",") if r.strip()]:
            if args.max_forests and searched >= args.max_forests:
                break
            rname = REGION_NAMES.get(region, region)
            if not select_and_wait_options(page, "srchSido", region, "srchInstt"):
                print(f"[{rname}] 휴양림 목록 없음")
                continue
            instts = [o for o in options_of(page, "srchInstt") if o["value"]]
            if not args.include_private:
                skipped = [o["text"] for o in instts if o["text"].startswith(PRIVATE_PREFIX)]
                instts = [o for o in instts if not o["text"].startswith(PRIVATE_PREFIX)]
            else:
                skipped = []
            print(f"\n[{rname}] 휴양림 {len(instts)}곳" + (f" (사립 {len(skipped)}곳 제외)" if skipped else ""))

            for instt in instts:
                if args.max_forests and searched >= args.max_forests:
                    break
                if not select_and_wait_options(page, "srchInstt", instt["value"], "srchForest"):
                    continue
                searched += 1
                forest_options = options_of(page, "srchForest")

                for facility in facilities:
                    forest = next((o for o in forest_options if o["text"] == facility), None)
                    if not forest:
                        print(f"  - {instt['text']}: '{facility}' 없음, 건너뜀")
                        continue
                    page.select_option("#srchForest", forest["value"])
                    wait_idle(page)

                    # 정원 제한은 숙소에만 적용한다 (야영장 사이트는 정원 기준이 달라 일괄 제외하지 않음)
                    max_cap = args.max_capacity if facility == "숙소" else 0
                    hits = []
                    for month in months:
                        try:
                            hits += search_month(page, month, args.include_wait, dates, max_cap)
                        except Exception as e:
                            print(f"  - {instt['text']} [{facility}] {month} 조회 실패: {e}")
                        time.sleep(args.delay)

                    print(f"  - {instt['text']} [{facility}]: {len(hits)}건")
                    for h in hits:
                        results.append({"region": rname, "forest": instt["text"], "facility": facility, **h})

        ctx.close()

    print("\n================ 결과 ================")
    if not results:
        print("대상 날짜에 예약 가능한 숙소가 없습니다.")
    results.sort(key=lambda r: (r["date"], r["region"], r["forest"], r["facility"], r["room"]))
    cur = None
    for r in results:
        if r["date"] != cur:
            cur = r["date"]
            print(f"\n■ {cur:%Y-%m-%d} ({'월화수목금토일'[cur.weekday()]}) - {r['reason']}")
        print(f"   [{r['region']}] {r['forest']} [{r['facility']}] / {r['room']} - {r['status']}")

    stamp = f"{dt.datetime.now():%Y%m%d_%H%M%S}"
    out = os.path.join(BASE_DIR, f"result_{stamp}.csv")
    with open(out, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=["date", "reason", "region", "forest", "facility", "room", "status"])
        w.writeheader()
        w.writerows(results)
    print(f"\nCSV 저장: {out}")

    usage_info = get_disk_usage_info(BASE_DIR)
    html_path = os.path.join(BASE_DIR, f"result_{stamp}.html")
    write_html(html_path, results, months, dates, args, facilities, usage_info)
    if FROZEN:
        import webbrowser
        webbrowser.open("file:///" + html_path.replace("\\", "/"))
    print(f"HTML 저장: {html_path}")

    # 화면 하단에 폴더 위치와 사용량 표시
    print("\n" + "=" * 60)
    print(f"📁 폴더 위치 : {usage_info['path']}")
    print(f"💾 사용량     : {usage_info['used_gb']:.2f} GB / {usage_info['total_gb']:.2f} GB ({usage_info['pct']:.1f}% 사용 중, 여유: {usage_info['free_gb']:.2f} GB)")
    print("=" * 60)


def write_html(path, results, months, dates, args, facilities, usage_info=None):
    """결과를 로컬 HTML 리포트로 저장한다 (외부 업로드 없음)."""
    from html import escape
    from collections import defaultdict, OrderedDict

    if usage_info is None:
        usage_info = get_disk_usage_info(BASE_DIR)

    by_date = OrderedDict()
    for r in sorted(results, key=lambda r: (r["date"], r["region"], r["forest"], r["facility"], r["room"])):
        by_date.setdefault(r["date"], []).append(r)

    forest_count = len({(r["region"], r["forest"]) for r in results})
    region_count = defaultdict(int)
    for r in results:
        region_count[r["region"]] += 1
    regions = [REGION_NAMES.get(x.strip(), x) for x in args.regions.split(",") if x.strip()]

    rows = []
    for day, items in by_date.items():
        wd = "월화수목금토일"[day.weekday()]
        grouped = OrderedDict()
        for it in items:
            grouped.setdefault((it["region"], it["forest"], it["facility"]), []).append(it)
        body = []
        for (region, forest, facility), rooms in grouped.items():
            chips = "".join(
                f'<span class="chip {"wait" if r["status"] != "예약가능" else ""}">{escape(r["room"])}'
                f'{" · " + escape(r["status"]) if r["status"] != "예약가능" else ""}</span>' for r in rooms)
            body.append(f'<tr data-region="{escape(region)}"><td class="reg">{escape(region)}</td>'
                        f'<td class="forest">{escape(forest)}</td><td class="fac">{escape(facility)}</td>'
                        f'<td class="num">{len(rooms)}</td><td class="rooms">{chips}</td></tr>')
        forest_n = len({(k[0], k[1]) for k in grouped})
        rows.append(
            f'<section class="day"><h2>{day:%Y-%m-%d} ({wd}) <span class="reason">{escape(items[0]["reason"])}</span>'
            f'<span class="cnt">{len(items)}실 · {forest_n}곳</span></h2>'
            f'<table><thead><tr><th>지역</th><th>휴양림</th><th>시설</th><th class="num">객실</th><th>객실명</th></tr></thead>'
            f'<tbody>{"".join(body)}</tbody></table></section>')

    no_hit_dates = [d for d in sorted(dates) if d not in by_date]
    no_hit = "".join(f'<li>{d:%m/%d}({"월화수목금토일"[d.weekday()]}) {escape(dates[d])}</li>' for d in no_hit_dates)
    filters = "".join(f'<label><input type="checkbox" value="{escape(r)}" checked> {escape(r)}</label>' for r in regions)

    html = f"""<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>숲나들e 빈 숙소</title>
<style>
:root {{ --bg:#f6f7f5; --card:#fff; --fg:#1f2a24; --muted:#6b7a71; --line:#e3e7e4; --accent:#2f7d4f; --wait:#b7791f; }}
@media (prefers-color-scheme: dark) {{ :root {{ --bg:#141a16; --card:#1c241f; --fg:#e6ece8; --muted:#9aa9a0; --line:#2c3630; --accent:#5cc28a; --wait:#e0a84a; }} }}
* {{ box-sizing:border-box; }}
body {{ margin:0; background:var(--bg); color:var(--fg); font-family:"Malgun Gothic","Apple SD Gothic Neo",sans-serif; }}
main {{ max-width:1100px; margin:0 auto; padding:24px 16px 48px; }}
h1 {{ font-size:22px; margin:0 0 4px; }}
.meta {{ color:var(--muted); font-size:13px; margin-bottom:16px; }}
.stats {{ display:flex; gap:12px; flex-wrap:wrap; margin-bottom:16px; }}
.stat {{ background:var(--card); border:1px solid var(--line); border-radius:10px; padding:10px 14px; min-width:120px; }}
.stat b {{ display:block; font-size:20px; color:var(--accent); }}
.filters {{ margin:8px 0 20px; display:flex; gap:14px; flex-wrap:wrap; font-size:14px; }}
.day {{ background:var(--card); border:1px solid var(--line); border-radius:12px; margin-bottom:16px; overflow:hidden; }}
.day h2 {{ font-size:16px; margin:0; padding:12px 14px; border-bottom:1px solid var(--line); display:flex; gap:10px; align-items:center; flex-wrap:wrap; }}
.reason {{ font-size:12px; font-weight:normal; color:var(--accent); border:1px solid var(--accent); border-radius:999px; padding:1px 8px; }}
.cnt {{ margin-left:auto; font-size:12px; font-weight:normal; color:var(--muted); }}
table {{ width:100%; border-collapse:collapse; font-size:14px; }}
th, td {{ text-align:left; padding:8px 12px; border-bottom:1px solid var(--line); vertical-align:top; }}
th {{ color:var(--muted); font-weight:normal; font-size:12px; }}
td.num, th.num {{ text-align:right; width:56px; font-variant-numeric:tabular-nums; }}
td.reg {{ white-space:nowrap; color:var(--muted); width:120px; }}
td.forest {{ width:260px; }}
td.fac {{ white-space:nowrap; color:var(--muted); width:64px; }}
.chip {{ display:inline-block; margin:2px 4px 2px 0; padding:2px 8px; border-radius:6px; background:color-mix(in srgb, var(--accent) 14%, transparent); font-size:12px; }}
.chip.wait {{ background:color-mix(in srgb, var(--wait) 18%, transparent); }}
.none {{ color:var(--muted); font-size:13px; }}
.none ul {{ columns:3; padding-left:18px; }}

/* 화면 하단 폴더 위치 및 사용량 표시 */
.footer-info {{ margin-top: 36px; padding-top: 20px; border-top: 1px solid var(--line); }}
.footer-card {{ background: var(--card); border: 1px solid var(--line); border-radius: 12px; padding: 16px 20px; }}
.footer-head {{ font-weight: bold; font-size: 15px; margin-bottom: 12px; color: var(--accent); display: flex; align-items: center; gap: 8px; }}
.footer-item {{ display: flex; gap: 10px; margin-bottom: 8px; font-size: 13px; align-items: baseline; flex-wrap: wrap; }}
.footer-item .lbl {{ color: var(--muted); min-width: 90px; font-weight: 500; }}
.footer-item .val {{ font-family: Consolas, "Courier New", monospace; word-break: break-all; color: var(--fg); }}
.progress-bar {{ width: 100%; height: 8px; background: var(--line); border-radius: 4px; overflow: hidden; margin-top: 10px; }}
.progress-fill {{ height: 100%; background: var(--accent); border-radius: 4px; transition: width 0.3s ease; }}

@media (max-width:640px) {{ td.reg, th:first-child {{ display:none; }} td.forest {{ width:auto; }} .none ul {{ columns:1; }} }}
</style></head><body><main>
<h1>숲나들e {"·".join(facilities)} · {"·".join(DAY_NAMES[w] + "요일" for w in sorted(set(DAY_KEYS[x.strip().lower()] for x in args.days.split(",") if x.strip())))}{"" if args.no_holidays else "·공휴일"} 빈 숙소</h1>
<div class="meta">조회 시각 {dt.datetime.now():%Y-%m-%d %H:%M} · 대상 월 {", ".join(months)} · 지역 {", ".join(regions)} · 숙박시설: {", ".join(facilities)}{f" · 숙소 {args.max_capacity}인 이하" if args.max_capacity and "숙소" in facilities else ""}{"" if args.include_private else " · 사립 제외"}{" · 대기 포함" if args.include_wait else ""}</div>
<div class="stats">
  <div class="stat"><b>{len(results)}</b>빈 객실(날짜 기준)</div>
  <div class="stat"><b>{len(by_date)}</b>가능 날짜 / 대상 {len(dates)}일</div>
  <div class="stat"><b>{forest_count}</b>휴양림</div>
  {"".join(f'<div class="stat"><b>{c}</b>{escape(r)}</div>' for r, c in region_count.items())}
</div>
<div class="filters">{filters}</div>
{"".join(rows) if rows else '<p class="none">대상 날짜에 예약 가능한 숙소가 없습니다.</p>'}
{f'<div class="none"><h3>빈 숙소가 없는 대상 날짜</h3><ul>{no_hit}</ul></div>' if no_hit else ''}

<!-- 화면 하단 폴더 위치 및 사용량 카드 -->
<footer class="footer-info">
  <div class="footer-card">
    <div class="footer-head">📁 저장 폴더 위치 및 디스크 사용량</div>
    <div class="footer-item">
      <span class="lbl">폴더 위치:</span>
      <span class="val">{escape(usage_info["path"])}</span>
    </div>
    <div class="footer-item">
      <span class="lbl">디스크 사용량:</span>
      <span class="val"><b>{usage_info["used_gb"]:.2f} GB</b> / {usage_info["total_gb"]:.2f} GB ({usage_info["pct"]:.1f}% 사용 중 · 여유 공간 <b>{usage_info["free_gb"]:.2f} GB</b>)</span>
    </div>
    <div class="progress-bar">
      <div class="progress-fill" style="width: {usage_info['pct']:.1f}%;"></div>
    </div>
  </div>
</footer>
</main>
<script>
document.querySelectorAll('.filters input').forEach(cb => cb.addEventListener('change', () => {{
  const on = new Set([...document.querySelectorAll('.filters input:checked')].map(x => x.value));
  document.querySelectorAll('tr[data-region]').forEach(tr => tr.style.display = on.has(tr.dataset.region) ? '' : 'none');
  document.querySelectorAll('section.day').forEach(s => s.style.display = [...s.querySelectorAll('tr[data-region]')].some(tr => tr.style.display !== 'none') ? '' : 'none');
}}));
</script></body></html>"""
    with open(path, "w", encoding="utf-8") as f:
        f.write(html)


def _safe(fn):
    try:
        fn()
    except Exception:
        pass


if __name__ == "__main__":
    try:
        main()
    finally:
        # exe를 더블클릭으로 실행한 경우 콘솔 창이 바로 닫히지 않게 한다.
        if FROZEN:
            input("\n엔터를 누르면 종료합니다...")
