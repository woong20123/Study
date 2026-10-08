"""
재구성 가이드(docs/rebuild-guide.html) 생성기.

실제 소스 파일을 읽어 HTML에 그대로 넣는다. 코드를 손으로 옮겨 적지 않으므로 문서와 코드가 어긋나지 않는다.
코드를 고친 뒤에는 다시 실행해서 문서를 갱신한다.

    python docs/tools/make_rebuild_guide.py
"""
import html
import json
import os
import subprocess
from datetime import date

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'docs', 'rebuild-guide.html')

# (경로, 설명) — 복원 순서대로
FILES = [
    ('StockTarget.sln', '솔루션. docs 솔루션 폴더 포함'),
    ('src/StockTarget.Core/StockTarget.Core.csproj', 'Core 라이브러리 프로젝트 (net9.0, Microsoft.Data.Sqlite)'),
    ('src/StockTarget.Core/Models.cs', '레코드: 시세·배당·분할·목표·매수금액·분기 행'),
    ('src/StockTarget.Core/Calculators.cs', '순수 계산: 목표가 역산, 5년 배당수익률, 분할 보정, 원화 매수금액 환산'),
    ('src/StockTarget.Core/YahooChartClient.cs', 'Yahoo chart API 호출과 JSON 파싱'),
    ('src/StockTarget.Core/StockDatabase.cs', 'SQLite 스키마·목표·확인 이력·캐시·설정(기본 매수금액)'),
    ('src/StockTarget.Core/StockService.cs', '캐시 우선 조회, 분할 보정, USD/KRW 환율, 확인 이력 기록'),
    ('src/StockTarget.Core/Reservations.cs', '다음 주 계단식 LOC 예약 주문표, 접수·중복 방지'),
    ('src/StockTarget.Core/KiwoomClient.cs', '키움 REST API: 토큰, 거래소구분, 미국주식 예약 매수'),
    ('src/StockTarget.Core/StartupOptions.cs', '구동 옵션 --db, --kiwoom off|mock|real'),
    ('src/StockTarget.Core/Backup.cs', '기기 간 백업 JSON 형식(v2, 기본 매수금액 포함)과 검증'),
    ('src/StockTarget.Core/Cloud.cs', 'Google OAuth(PKCE) · Firebase 로그인 · Realtime Database 저장/읽기'),
    ('src/StockTarget.App/StockTarget.App.csproj', 'WPF 앱 프로젝트 (net9.0-windows, DPAPI 패키지)'),
    ('src/StockTarget.App/AssemblyInfo.cs', 'WPF 템플릿 기본 파일'),
    ('src/StockTarget.App/App.xaml', '앱 리소스: 두 창이 함께 쓰는 표·판정 색 스타일'),
    ('src/StockTarget.App/App.xaml.cs', '시작: DB·서비스·뷰모델 구성, --db 인자'),
    ('src/StockTarget.App/MainWindow.xaml', '화면 레이아웃'),
    ('src/StockTarget.App/MainWindow.xaml.cs', '화면 코드 비하인드'),
    ('src/StockTarget.App/ReservationWindow.xaml', 'LOC 예약 매수 주문표 창'),
    ('src/StockTarget.App/ReservationWindow.xaml.cs', '예약 창 코드 비하인드'),
    ('src/StockTarget.App/ViewModels/Mvvm.cs', 'ObservableObject, AsyncCommand, RelayCommand'),
    ('src/StockTarget.App/ViewModels/TargetRowViewModel.cs', '목표 목록 한 행'),
    ('src/StockTarget.App/ViewModels/MainViewModel.cs', '메인 화면 뷰모델'),
    ('src/StockTarget.App/ViewModels/MainViewModel.Backup.cs', '데이터 메뉴: JSON 백업·복원, Firebase 저장·복원'),
    ('src/StockTarget.App/ViewModels/MainViewModel.Defaults.cs', '기본 매수금액 입력·저장, 목표 입력칸 기본값 채움'),
    ('src/StockTarget.App/Services/CloudAuth.cs', '브라우저 Google 로그인(루프백), 로그인 정보 DPAPI 저장'),
    ('src/StockTarget.App/ViewModels/ReservationViewModel.cs', '예약 창 뷰모델: 주문표 계산·접수 확인'),
    ('tests/StockTarget.Tests/StockTarget.Tests.csproj', 'xUnit 테스트 프로젝트'),
    ('tests/StockTarget.Tests/CoreTests.cs', '단위 테스트 63개 + 실데이터 테스트 2개'),
    ('tests/StockTarget.Tests/ReservationTests.cs', '예약 주문표(도달 단계) · 키움 요청(가짜 서버) · 구동 옵션 테스트 33개'),
    ('tests/StockTarget.Tests/BackupTests.cs', '백업 왕복 · 기본 매수금액 · Google/Firebase 요청(가짜 서버) 테스트 19개'),
]

LANG = {'.cs': 'C#', '.xaml': 'XAML', '.csproj': 'XML', '.sln': 'sln'}


def read(rel):
    with open(os.path.join(ROOT, rel), encoding='utf-8-sig') as f:
        return f.read().replace('\r\n', '\n')


def tool_versions():
    try:
        return subprocess.run(['dotnet', '--version'], capture_output=True, text=True, timeout=30).stdout.strip()
    except Exception:
        return '9.0.x'


def file_block(i, rel, desc, content):
    lines = content.count('\n') + 1
    ext = os.path.splitext(rel)[1]
    return f'''
<details class="file" id="f{i}">
  <summary><code>{html.escape(rel)}</code><span class="desc">{html.escape(desc)}</span>
    <span class="lines">{LANG.get(ext, '')} · {lines}줄</span></summary>
  <div class="actions">
    <button type="button" onclick="copyFile({i})">복사</button>
    <button type="button" onclick="saveFile({i})">파일로 저장</button>
  </div>
  <pre><code>{html.escape(content)}</code></pre>
</details>'''


def main():
    files = [(rel, desc, read(rel)) for rel, desc in FILES]
    total_lines = sum(c.count('\n') + 1 for _, _, c in files)
    data = json.dumps([{'path': rel, 'content': c} for rel, _, c in files], ensure_ascii=False)
    data = data.replace('</', '<\\/')  # </script> 조기 종료 방지
    blocks = '\n'.join(file_block(i, rel, desc, c) for i, (rel, desc, c) in enumerate(files))
    toc_files = '\n'.join(f'<li><a href="#f{i}"><code>{html.escape(rel)}</code></a></li>' for i, (rel, _, _) in enumerate(files))

    page = TEMPLATE.format(
        today=date.today().isoformat(),
        dotnet=html.escape(tool_versions()),
        file_count=len(files),
        total_lines=f'{total_lines:,}',
        toc_files=toc_files,
        blocks=blocks,
        data=data,
    )
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write(page)
    print(f'written {OUT} ({len(files)} files, {total_lines} lines)')


TEMPLATE = r'''<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>StockTarget 재구성 가이드</title>
<style>
  :root {{
    --bg:#fff; --fg:#1f2328; --muted:#656d76; --line:#d8dee4; --panel:#f6f8fa; --accent:#0969da;
    --ok:#1a7f37; --ok-bg:#dafbe1; --warn:#9a6700; --warn-bg:#fff8c5; --info-bg:#ddf4ff; --code:#f3f4f6;
  }}
  @media (prefers-color-scheme: dark) {{
    :root:not([data-theme="light"]) {{
      --bg:#0d1117; --fg:#e6edf3; --muted:#8d96a0; --line:#30363d; --panel:#161b22; --accent:#4493f8;
      --ok:#3fb950; --ok-bg:#12261e; --warn:#d29922; --warn-bg:#272115; --info-bg:#121d2f; --code:#1b2028;
    }}
  }}
  :root[data-theme="dark"] {{
    --bg:#0d1117; --fg:#e6edf3; --muted:#8d96a0; --line:#30363d; --panel:#161b22; --accent:#4493f8;
    --ok:#3fb950; --ok-bg:#12261e; --warn:#d29922; --warn-bg:#272115; --info-bg:#121d2f; --code:#1b2028;
  }}
  * {{ box-sizing:border-box; }}
  body {{ margin:0; background:var(--bg); color:var(--fg); font:15px/1.65 "Malgun Gothic","Segoe UI",system-ui,sans-serif; }}
  main {{ max-width:1100px; margin:0 auto; padding:32px 16px 64px; }}
  h1 {{ font-size:26px; margin:0 0 4px; }}
  h2 {{ font-size:20px; margin:40px 0 12px; padding-bottom:6px; border-bottom:1px solid var(--line); }}
  h3 {{ font-size:16px; margin:22px 0 8px; }}
  .meta {{ color:var(--muted); font-size:13px; margin-bottom:20px; }}
  a {{ color:var(--accent); }}
  code, pre {{ font-family:Consolas,"D2Coding",monospace; font-size:13px; }}
  :not(pre) > code {{ background:var(--code); padding:1px 5px; border-radius:4px; }}
  pre {{ background:var(--code); padding:12px 14px; border-radius:6px; overflow-x:auto; line-height:1.5; margin:0; }}
  .panel {{ border:1px solid var(--line); border-left-width:4px; border-radius:6px; padding:12px 16px; margin:14px 0; }}
  .panel.info {{ background:var(--info-bg); border-left-color:var(--accent); }}
  .panel.ok {{ background:var(--ok-bg); border-left-color:var(--ok); }}
  .panel.warn {{ background:var(--warn-bg); border-left-color:var(--warn); }}
  .panel p:first-child {{ margin-top:0; }} .panel p:last-child {{ margin-bottom:0; }}
  .table-wrap {{ overflow-x:auto; margin:12px 0; }}
  table {{ border-collapse:collapse; width:100%; font-size:14px; }}
  th, td {{ border:1px solid var(--line); padding:7px 10px; text-align:left; vertical-align:top; }}
  th {{ background:var(--panel); white-space:nowrap; }}
  td.num {{ text-align:right; font-variant-numeric:tabular-nums; }}
  caption {{ caption-side:bottom; text-align:left; color:var(--muted); font-size:12px; padding-top:6px; }}
  nav.toc {{ background:var(--panel); border:1px solid var(--line); border-radius:6px; padding:12px 20px; }}
  nav.toc ol {{ margin:4px 0; padding-left:20px; }}
  nav.toc ul {{ columns:2; margin:4px 0; padding-left:18px; font-size:13px; }}
  details.file {{ border:1px solid var(--line); border-radius:6px; margin:8px 0; background:var(--panel); }}
  details.file summary {{ cursor:pointer; padding:8px 12px; display:flex; gap:12px; flex-wrap:wrap; align-items:baseline; }}
  details.file .desc {{ color:var(--muted); font-size:13px; }}
  details.file .lines {{ margin-left:auto; color:var(--muted); font-size:12px; }}
  details.file .actions {{ padding:0 12px 8px; display:flex; gap:6px; }}
  details.file pre {{ border-radius:0 0 6px 6px; max-height:640px; }}
  button {{ border:1px solid var(--line); background:var(--bg); color:var(--fg); border-radius:6px; padding:4px 12px; cursor:pointer; font-size:13px; }}
  button.primary {{ background:var(--accent); color:#fff; border-color:var(--accent); padding:8px 16px; font-size:14px; }}
  .toast {{ position:fixed; bottom:20px; left:50%; transform:translateX(-50%); background:var(--fg); color:var(--bg);
            padding:8px 16px; border-radius:6px; font-size:13px; opacity:0; transition:opacity .2s; pointer-events:none; }}
  .toast.show {{ opacity:.92; }}
  .theme-toggle {{ position:fixed; top:12px; right:12px; font-size:12px; }}
  ol.steps > li {{ margin-bottom:10px; }}
</style>
</head>
<body>
<button class="theme-toggle" type="button" onclick="toggleTheme()">테마</button>
<main>
  <h1>StockTarget 재구성 가이드</h1>
  <div class="meta">생성일 {today} · 생성기 <code>docs/tools/make_rebuild_guide.py</code> · 소스 {file_count}개 / {total_lines}줄 · 확인한 .NET SDK {dotnet}</div>

  <div class="panel ok">
    <p><strong>요약</strong> — 이 문서만 있으면 다른 PC에서 같은 프로그램을 다시 만들 수 있다.
      가장 빠른 방법은 아래 <strong>[복원 스크립트 저장]</strong>으로 PowerShell 스크립트를 받아 실행하는 것이다(모든 파일 생성 → 빌드 → 테스트).
      직접 만들려면 3장의 단계를 따르고, 6장의 파일을 복사한다.</p>
    <p><button class="primary" type="button" onclick="saveRestoreScript()">복원 스크립트 저장 (restore-stocktarget.ps1)</button></p>
  </div>

  <nav class="toc"><strong>목차</strong>
    <ol>
      <li><a href="#what">무엇을 만드는가</a></li>
      <li><a href="#env">필요 환경</a></li>
      <li><a href="#steps">재구성 절차</a></li>
      <li><a href="#spec">핵심 명세 (계산 · API · DB)</a></li>
      <li><a href="#verify">검증 기준값</a></li>
      <li><a href="#files">전체 소스 코드</a></li>
      <li><a href="#pitfalls">주의사항</a></li>
    </ol>
  </nav>

  <h2 id="what">1. 무엇을 만드는가</h2>
  <ul>
    <li>목표 연도의 <strong>EPS × PER</strong>로 목표 주가를 정하고, <strong>목표 수익률 − 최근 5년 평균 배당수익률</strong>을 필요 주가 상승률로 삼아
      입력 시점부터 목표 연도까지 <strong>분기별 매입 목표가</strong>를 역산한다.</li>
    <li>미국 주식 시세·배당·분할은 Yahoo Finance chart API에서 받고, 조회 결과는 최대 1시간 캐시한다.</li>
    <li>목표·분기 확인 이력·캐시는 SQLite에 저장하고, WPF 화면에서 목록·분기 일정·5년 배당·확인 이력을 본다.</li>
  </ul>
  <div class="table-wrap"><table>
    <thead><tr><th>프로젝트</th><th>대상</th><th>역할</th></tr></thead>
    <tbody>
      <tr><td><code>StockTarget.Core</code></td><td>net9.0 클래스 라이브러리</td><td>계산(순수 함수), Yahoo 조회, SQLite 저장, 서비스</td></tr>
      <tr><td><code>StockTarget.App</code></td><td>net9.0-windows WPF</td><td>화면(MVVM, 외부 MVVM 라이브러리 없음)</td></tr>
      <tr><td><code>StockTarget.Tests</code></td><td>net9.0 xUnit</td><td>단위 테스트 115개 + 실데이터 테스트 2개</td></tr>
    </tbody>
  </table></div>

  <h2 id="env">2. 필요 환경</h2>
  <div class="table-wrap"><table>
    <thead><tr><th>항목</th><th>요구</th><th>확인 방법</th></tr></thead>
    <tbody>
      <tr><td>OS</td><td>Windows 10/11 (WPF)</td><td>-</td></tr>
      <tr><td>.NET SDK</td><td>9.0 이상</td><td><code>dotnet --list-sdks</code></td></tr>
      <tr><td>NuGet</td><td><code>api.nuget.org</code> 접근 (Microsoft.Data.Sqlite, xUnit 패키지)</td><td><code>dotnet restore</code></td></tr>
      <tr><td>네트워크</td><td><code>query2.finance.yahoo.com</code> HTTPS 접근</td><td>앱 실행 후 현재가 표시 여부</td></tr>
      <tr><td>(선택) Visual Studio 2022</td><td>.NET 데스크톱 개발 워크로드</td><td><code>StockTarget.sln</code> 열기</td></tr>
    </tbody>
  </table></div>

  <h2 id="steps">3. 재구성 절차</h2>
  <h3>방법 A — 복원 스크립트 (권장)</h3>
  <ol class="steps">
    <li>위의 <strong>[복원 스크립트 저장]</strong>으로 <code>restore-stocktarget.ps1</code>을 받는다(UTF-8 BOM으로 저장되므로 Windows PowerShell 5.1에서도 한글이 깨지지 않는다).</li>
    <li>만들 위치에서 실행한다:
      <pre><code>powershell -ExecutionPolicy Bypass -File restore-stocktarget.ps1 -Root D:\work\StockTarget</code></pre></li>
    <li>스크립트가 파일 {file_count}개를 만들고 <code>dotnet build</code>와 <code>dotnet test</code>까지 실행한다. 실패하면 거기서 멈춘다.</li>
    <li>솔루션의 <code>docs</code> 폴더 항목(이 문서·작업 진행 문서·화면 이미지)은 코드가 아니라 복원되지 않는다. Visual Studio에서는 "찾을 수 없는 파일"로 보이며 빌드에는 영향이 없다.</li>
    <li>실행: <code>dotnet run --project D:\work\StockTarget\src\StockTarget.App</code></li>
  </ol>

  <h3>방법 B — 직접 구성</h3>
  <pre><code>mkdir StockTarget &amp;&amp; cd StockTarget
dotnet new sln -n StockTarget
dotnet new classlib -n StockTarget.Core  -o src/StockTarget.Core  -f net9.0
dotnet new wpf      -n StockTarget.App   -o src/StockTarget.App   -f net9.0
dotnet new xunit    -n StockTarget.Tests -o tests/StockTarget.Tests -f net9.0
dotnet sln add src/StockTarget.Core src/StockTarget.App tests/StockTarget.Tests
dotnet add src/StockTarget.App reference src/StockTarget.Core
dotnet add tests/StockTarget.Tests reference src/StockTarget.Core
dotnet add src/StockTarget.Core package Microsoft.Data.Sqlite
del src\StockTarget.Core\Class1.cs tests\StockTarget.Tests\UnitTest1.cs

:: 6장의 파일 내용으로 각 파일을 만들거나 덮어쓴다 (csproj 포함)

dotnet build
dotnet test
dotnet run --project src/StockTarget.App</code></pre>

  <h2 id="spec">4. 핵심 명세</h2>
  <h3>4-1. 계산 규칙</h3>
  <pre><code>목표 주가          = 목표 EPS × 목표 PER, 또는 직접 입력값     (목표 연도 12월 31일, 직접 입력이 우선)
  - 직접 입력이면 EPS·PER은 0으로 저장하고 목표 주가 &gt; 0 만 검사 (TargetPlan.TargetPriceInput)
필요 주가 상승률 g = 목표 수익률 − 최근 5년 평균 배당수익률    (입력 시점 값으로 고정해 저장)
분기별 매입 목표가 = 목표 주가 ÷ (1 + g) ^ (기준일 → 목표일 일수 ÷ 365.25)
  - 분기 범위: 입력일이 속한 분기 ~ 목표일이 속한 분기
  - 기준일: 첫 분기는 입력일, 이후는 각 분기 첫날 (1/1, 4/1, 7/1, 10/1)

판정 (현재가 vs 이번 분기 매입 목표가 b, 경계값은 해당 단계 포함)  — TargetCalculator.Classify
  - 현재가 ≤ b × 0.80   → 강력매수  (파란색)     StrongBuyPct = 20
  - 현재가 ≤ b × 0.90   → 필수매수  (하늘색)     MustBuyPct   = 10
  - 현재가 ≤ b          → 매수      (녹색)
  - 현재가 ≤ b × 1.03   → 매입 대기 (연한 녹색)  NearPct      = 3
  - 그 위               → 대기      (회색)

5년 평균 배당수익률 = 1년 구간 5개 각각의 (구간 배당금 합계 ÷ 구간 일별 종가 평균) 의 평균
  - 구간: 마지막 거래일 end 기준 (end-1년, end], (end-2년, end-1년], ...  (달력 연도 아님)
  - 종가: 분할만 반영, 배당 미반영 (chart API indicators.quote.close)
  - 배당: chart API events.dividends.amount (분할 반영 금액)

분할 직후 전일 종가 보정
  - |현재가 ÷ 전일 종가 − 1| ≥ 30% 일 때만 최근 1개월 분할 조회
  - 최근 5일 안의 분할이 있고, 전일 종가 ÷ 분할비율 로 바꾸면 변동률이 0에 더 가까울 때만 보정
  - 분할비율 = numerator ÷ denominator (10:1 → 10, 1:10 역분할 → 0.1)

매수 단계별 매수금액 (원화 입력 → 달러 표시)  — BuyAmounts, Money
  - 단계: 매수 BuyKrw / 필수매수 MustBuyKrw / 강력매수 StrongBuyKrw (비우면 null)
  - 입력 해석: 쉼표·₩·원·공백 제거, 끝의 만(×10,000)·억(×100,000,000), 음수·문자는 ArgumentException
  - 환율: Yahoo KRW=X 현재가(1달러당 원), 시세 캐시와 동일. 달러 = 원화 ÷ 환율
  - 목록 '매수금액' 열: 현재 판정 단계 금액 "₩1,000,000 ($747.67)", 매입 대기·대기면 빈칸

다음 주 LOC 예약 매수  — ReservationPlanner, ReservationService, KiwoomClient
  - 기간: 실행일 기준 다음 주 월~금 (월요일에 실행해도 그다음 주), 분기 경계를 넘으면 분기별로 나눔
  - 주문가: 매수 b / 필수매수 b × 0.90 / 강력매수 b × 0.80, 센트 단위 내림
  - 금액: 매수 / 필수매수 − 매수 / 강력매수 − 필수매수 (계단식, 비운 단계는 건너뜀)
  - 수량: 금액 ÷ 환율 ÷ 주문가, 1주 단위 내림. 0주면 제외
  - 키움: au10001 토큰 → usa10098 거래소(ND/NY/NA) → ust21200 (rsrv_ord_tp=2 기간예약 잔량주문, trde_tp=30 LOC)
  - 키: 환경변수 KIWOOM_APPKEY, KIWOOM_SECRETKEY, KIWOOM_ENV(real이면 실전, 기본 모의투자)
  - 중복 방지: reservations 테이블에 (티커, 단계, 시작일, 종료일, 환경)이 있으면 다시 보내지 않음

백업 · Firebase 동기화  — BackupData, FirebaseClient, GoogleLoopbackSignIn
  - JSON v1: format "stocktarget-backup", targets / priceChecks / reservations (캐시 제외, null 생략, camelCase)
  - 복원: 현재 데이터를 backups\before-restore-*.json 으로 자동 백업 → 한 트랜잭션으로 통째로 교체
  - Google 로그인: 데스크톱 OAuth 클라이언트 + PKCE + http://127.0.0.1:{{임시포트}}/ 리디렉션 → Google ID 토큰
  - Firebase: accounts:signInWithIdp(providerId=google.com) → uid · idToken · refreshToken, securetoken으로 갱신
  - 저장 위치: {{DatabaseUrl}}/users/{{uid}}/stocktarget.json?auth={{idToken}} (PUT 저장, GET 읽기, null이면 없음)
  - 설정 %LOCALAPPDATA%\StockTarget\firebase.json, 로그인 정보 firebase-session.dat (DPAPI 현재 사용자)</code></pre>

  <h3>4-2. Yahoo Finance chart API</h3>
  <div class="table-wrap"><table>
    <thead><tr><th>용도</th><th>요청</th></tr></thead>
    <tbody>
      <tr><td>현재가</td><td><code>GET https://query2.finance.yahoo.com/v8/finance/chart/{{SYMBOL}}?range=1d&amp;interval=1d</code></td></tr>
      <tr><td>5년 배당</td><td><code>...?range=6y&amp;interval=1d&amp;events=div,split</code></td></tr>
      <tr><td>최근 분할</td><td><code>...?range=1mo&amp;interval=1d&amp;events=div,split</code></td></tr>
      <tr><td>USD/KRW 환율</td><td><code>.../chart/KRW=X?range=1d&amp;interval=1d</code></td></tr>
    </tbody>
    <caption>헤더: 브라우저 User-Agent 필수(없으면 429). 비공식 API라 형식이 바뀔 수 있다.</caption>
  </table></div>
  <div class="table-wrap"><table>
    <thead><tr><th>JSON 경로</th><th>사용처</th></tr></thead>
    <tbody>
      <tr><td><code>chart.result[0].meta.regularMarketPrice</code></td><td>현재가</td></tr>
      <tr><td><code>chart.result[0].meta.chartPreviousClose</code> (없으면 <code>previousClose</code>)</td><td>전일 종가</td></tr>
      <tr><td><code>meta.currency</code>, <code>meta.exchangeName</code>, <code>meta.longName</code>/<code>shortName</code>, <code>meta.gmtoffset</code></td><td>통화·거래소·종목명·현지 날짜 계산</td></tr>
      <tr><td><code>chart.result[0].timestamp[]</code> + <code>indicators.quote[0].close[]</code></td><td>일별 종가(null은 건너뜀)</td></tr>
      <tr><td><code>events.dividends.{{ts}}.amount</code>, <code>.date</code></td><td>배당</td></tr>
      <tr><td><code>events.splits.{{ts}}.numerator</code>, <code>.denominator</code>, <code>.date</code></td><td>분할</td></tr>
      <tr><td><code>chart.error.description</code></td><td>없는 티커 등 오류 메시지</td></tr>
    </tbody>
  </table></div>

  <h3>4-3. SQLite 스키마</h3>
  <pre><code>CREATE TABLE IF NOT EXISTS targets (
    symbol TEXT PRIMARY KEY, input_date TEXT NOT NULL, target_year INTEGER NOT NULL,
    eps REAL NOT NULL, per REAL NOT NULL, return_pct REAL NOT NULL,
    dividend_yield_pct REAL NOT NULL, memo TEXT, updated_at TEXT NOT NULL,
    buy_krw REAL, must_buy_krw REAL, strong_buy_krw REAL, target_price REAL);
CREATE TABLE IF NOT EXISTS reservations (
    symbol TEXT NOT NULL, stage TEXT NOT NULL, start_date TEXT NOT NULL, end_date TEXT NOT NULL,
    price REAL NOT NULL, quantity INTEGER NOT NULL, amount_krw REAL NOT NULL, env TEXT NOT NULL,
    reservation_no TEXT NOT NULL, scheduled_date TEXT NOT NULL, created_at TEXT NOT NULL,
    PRIMARY KEY (symbol, stage, start_date, end_date, env));
CREATE TABLE IF NOT EXISTS price_checks (
    symbol TEXT NOT NULL, check_date TEXT NOT NULL, quarter TEXT NOT NULL,
    price REAL NOT NULL, buy_price REAL NOT NULL, PRIMARY KEY (symbol, check_date));
CREATE TABLE IF NOT EXISTS cache (
    key TEXT PRIMARY KEY, fetched_at INTEGER NOT NULL, payload TEXT NOT NULL);</code></pre>
  <ul>
    <li>DB 기본 경로 <code>%LOCALAPPDATA%\StockTarget\stocktarget.db</code>, 앱 인자 <code>--db &lt;경로&gt;</code>로 변경.</li>
    <li>매수금액·<code>target_price</code> 컬럼이 없는 이전 DB는 앱 시작 시 <code>ALTER TABLE ... ADD COLUMN</code>으로 추가(<code>pragma_table_info</code> 확인).</li>
    <li>캐시 키 <code>quote:SYMBOL</code>, <code>dividend:SYMBOL</code>. TTL 상한 3600초. 실패 결과는 저장하지 않는다.</li>
    <li>목표 삭제 시 해당 티커의 <code>price_checks</code>도 함께 삭제. 확인 이력은 티커·날짜당 1행.</li>
  </ul>

  <h2 id="verify">5. 검증 기준값</h2>
  <p>재구성 후 아래 값이 나오면 같은 로직이다(입력일 2026-10-08, 조회일 기준 데이터).</p>
  <div class="table-wrap"><table>
    <thead><tr><th>확인 항목</th><th>입력</th><th>기대값</th></tr></thead>
    <tbody>
      <tr><td>목표 주가</td><td>KO, EPS 10.5, PER 18</td><td class="num">189.00</td></tr>
      <tr><td>2026Q4 매입 목표가</td><td>목표 수익률 10%, 배당 2.92% (g = 7.08%)</td><td class="num">141.51 (배당 2.92063…%면 141.52)</td></tr>
      <tr><td>2030Q4 매입 목표가</td><td>같음</td><td class="num">185.81</td></tr>
      <tr><td>판정 경계</td><td>매입 목표가 100</td><td class="num">80 강력매수 · 80.01/90 필수매수 · 90.01/100 매수 · 103 매입 대기 · 103.01 대기</td></tr>
      <tr><td>매수금액 환산</td><td>₩1,350,000, 환율 1,350</td><td class="num">₩1,350,000 ($1,000.00)</td></tr>
      <tr><td>매수금액 입력</td><td><code>100만</code> / <code>150만원</code> / <code>1.5억</code></td><td class="num">1,000,000 / 1,500,000 / 150,000,000</td></tr>
      <tr><td>목표 주가 직접 입력</td><td>직접 입력 150 vs EPS 6 × PER 25</td><td class="num">같은 분기 일정</td></tr>
      <tr><td>다음 주</td><td>2026-10-08(목) 실행</td><td class="num">2026-10-12 ~ 2026-10-16</td></tr>
      <tr><td>LOC 주문표</td><td>KO 목표 주가 120, 배당 2.92%, 100만/200만/300만, 환율 1,350</td><td class="num">89.84×8 · 80.86×9 · 71.87×10</td></tr>
      <tr><td>분기 수</td><td>2026-10-08 ~ 2030-12-31</td><td class="num">17 (2026Q4 ~ 2030Q4)</td></tr>
      <tr><td>KO 5년 평균 배당수익률</td><td>2026-10-08 조회</td><td class="num">2.92% (2.69/2.97/3.08/3.00/2.87)</td></tr>
      <tr><td>AAPL 5년 평균 배당수익률</td><td>2026-10-08 조회</td><td class="num">0.49%</td></tr>
      <tr><td>NVDA 분할 전 배당</td><td>2024-06-10 10:1 분할 이전</td><td class="num">0.004 (원래 $0.04)</td></tr>
    </tbody>
    <caption>시세·배당 기대값은 조회일에 따라 달라진다. 계산 기대값은 <code>CoreTests.cs</code>에 고정돼 있다.</caption>
  </table></div>
  <pre><code>dotnet test                                   # 단위 테스트 (네트워크 불필요)
set STOCKTARGET_LIVE=1 &amp;&amp; dotnet test         # 실데이터 테스트 포함 (KO 평균, NVDA 분할 환산)</code></pre>

  <h2 id="files">6. 전체 소스 코드</h2>
  <p>각 파일을 펼쳐 <strong>복사</strong>하거나 <strong>파일로 저장</strong>한다. 경로는 솔루션 루트 기준이다.</p>
  <nav class="toc"><ul>
{toc_files}
  </ul></nav>
{blocks}

  <h2 id="pitfalls">7. 주의사항</h2>
  <div class="table-wrap"><table>
    <thead><tr><th>증상</th><th>원인</th><th>대응</th></tr></thead>
    <tbody>
      <tr><td>Yahoo가 HTTP 429</td><td>기본 User-Agent 차단</td><td>브라우저 UA 지정(<code>YahooChartClient</code> 생성자)</td></tr>
      <tr><td>WPF 빌드 오류 CS0103 <code>HttpRequestException</code></td><td>WPF 프로젝트는 <code>System.Net.Http</code>가 암시적 using에 없음</td><td><code>using System.Net.Http;</code></td></tr>
      <tr><td>회사망에서 SSL 오류</td><td>.NET은 Windows 인증서 저장소를 쓰므로 보통 발생하지 않음. 다른 언어/라이브러리(Python curl_cffi 등)는 자체 CA만 믿어 실패</td><td>사내 루트 인증서를 CA 묶음에 추가</td></tr>
      <tr><td>배당수익률이 실제보다 큼</td><td>배당 반영 수정주가(<code>adjclose</code>)를 분모로 사용</td><td><code>indicators.quote[0].close</code> 사용</td></tr>
      <tr><td>한글이 든 .ps1 실행 시 구문 오류</td><td>Windows PowerShell 5.1은 BOM 없는 UTF-8을 CP949로 읽음</td><td>UTF-8 BOM으로 저장(복원 스크립트는 BOM 포함)</td></tr>
      <tr><td>캐시 때문에 값이 안 바뀜</td><td>1시간 캐시</td><td>"시세 새로고침(캐시 무시)" 버튼</td></tr>
    </tbody>
  </table></div>
</main>
<div class="toast" id="toast"></div>
<script id="files-data" type="application/json">{data}</script>
<script>
  var FILES = JSON.parse(document.getElementById('files-data').textContent);

  function toast(msg) {{
    var t = document.getElementById('toast'); t.textContent = msg; t.classList.add('show');
    setTimeout(function () {{ t.classList.remove('show'); }}, 1600);
  }}
  function download(name, text, bom) {{
    var blob = new Blob([(bom ? '\ufeff' : '') + text], {{ type: 'text/plain;charset=utf-8' }});
    var a = document.createElement('a'); a.href = URL.createObjectURL(blob); a.download = name;
    document.body.appendChild(a); a.click(); setTimeout(function () {{ URL.revokeObjectURL(a.href); a.remove(); }}, 0);
  }}
  function copyFile(i) {{
    var text = FILES[i].content;
    if (navigator.clipboard && navigator.clipboard.writeText) {{
      navigator.clipboard.writeText(text).then(function () {{ toast('복사했습니다: ' + FILES[i].path); }},
        function () {{ fallbackCopy(text, i); }});
    }} else {{ fallbackCopy(text, i); }}
  }}
  function fallbackCopy(text, i) {{
    var ta = document.createElement('textarea'); ta.value = text; document.body.appendChild(ta); ta.select();
    try {{ document.execCommand('copy'); toast('복사했습니다: ' + FILES[i].path); }} catch (e) {{ toast('복사 실패 — 직접 선택해 복사하세요'); }}
    ta.remove();
  }}
  function saveFile(i) {{ var p = FILES[i].path; download(p.substring(p.lastIndexOf('/') + 1), FILES[i].content, false); }}

  // 모든 파일을 만들고 빌드·테스트하는 PowerShell 스크립트를 만든다.
  // 파일 내용은 Base64(UTF-8)로 넣어 here-string 이스케이프 문제를 피한다.
  function b64(s) {{ return btoa(unescape(encodeURIComponent(s))); }}
  function saveRestoreScript() {{
    var lines = [
      '# StockTarget 복원 스크립트 (rebuild-guide.html에서 생성)',
      'param([string]$Root = (Join-Path (Get-Location) "StockTarget"), [switch]$SkipBuild)',
      '$ErrorActionPreference = "Stop"',
      '$utf8 = New-Object System.Text.UTF8Encoding $false',
      'New-Item -ItemType Directory -Force -Path $Root | Out-Null',
      '$files = @('
    ];
    FILES.forEach(function (f, idx) {{
      lines.push('  @{{ Path = "' + f.path + '"; Data = "' + b64(f.content) + '" }}' + (idx < FILES.length - 1 ? ',' : ''));
    }});
    lines.push(')');
    lines.push('foreach ($f in $files) {{');
    lines.push('  $dest = Join-Path $Root $f.Path');
    lines.push('  New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null');
    lines.push('  $text = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($f.Data))');
    lines.push('  [System.IO.File]::WriteAllText($dest, $text, $utf8)');
    lines.push('  Write-Host ("created " + $f.Path)');
    lines.push('}}');
    lines.push('if ($SkipBuild) {{ return }}');
    lines.push('Push-Location $Root');
    lines.push('try {{');
    lines.push('  dotnet build; if ($LASTEXITCODE -ne 0) {{ throw "build failed" }}');
    lines.push('  dotnet test --no-build; if ($LASTEXITCODE -ne 0) {{ throw "test failed" }}');
    lines.push('  Write-Host ""; Write-Host "OK. run: dotnet run --project " (Join-Path $Root "src\\StockTarget.App")');
    lines.push('}} finally {{ Pop-Location }}');
    download('restore-stocktarget.ps1', lines.join('\r\n') + '\r\n', true);
    toast('restore-stocktarget.ps1 저장');
  }}

  function toggleTheme() {{
    var root = document.documentElement;
    var dark = root.dataset.theme ? root.dataset.theme === 'dark' : matchMedia('(prefers-color-scheme: dark)').matches;
    root.dataset.theme = dark ? 'light' : 'dark';
    try {{ localStorage.setItem('stocktarget-theme', root.dataset.theme); }} catch (e) {{}}
  }}
  try {{ var t = localStorage.getItem('stocktarget-theme'); if (t) document.documentElement.dataset.theme = t; }} catch (e) {{}}
</script>
</body>
</html>
'''

if __name__ == '__main__':
    main()
