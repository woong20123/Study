# StockTarget (매수타점)

## 버전 규칙

- 형식은 `major.minor.patch`, **1.0.0에서 시작**한다.
- **휴대폰에 빌드 패키지를 올릴 때마다 patch를 1 올린다** (1.0.0 → 1.0.1 → 1.0.2 …).
  - `scripts/Deploy-Phone.ps1`로 올린다. 이 스크립트가 patch 올리기, Release APK 빌드, `adb install -r`(데이터 유지)를 한 번에 하고, 실패하면 버전을 되돌린다.
    ```
    powershell -ExecutionPolicy Bypass -File scripts\Deploy-Phone.ps1 -Device <휴대폰 IP:포트>
    ```
  - 손으로 `dotnet build -t:Run`이나 `adb install`로 휴대폰에 올리지 않는다(버전이 안 올라간다). 에뮬레이터 확인은 버전을 올리지 않는다.
  - 올린 뒤 바뀐 버전(csproj)은 그 작업 커밋에 함께 넣는다.
- **major · minor 규칙은 아직 정하지 않았다(사용자가 고민 중).** 사용자가 정하기 전에는 major · minor를 올리지 않는다.
- 버전이 적힌 곳
  - 모바일: `src/StockTarget.Mobile/StockTarget.Mobile.csproj`
    - `ApplicationDisplayVersion` = 표시 버전 (`1.0.1`)
    - `ApplicationVersion` = 안드로이드 내부 빌드 번호 = `major*10000 + minor*100 + patch` (1.0.1 → 10001). 안드로이드는 이 번호가 커져야 업데이트로 설치하므로 늘 표시 버전에서 계산한다. minor · patch는 99까지.
  - 윈도우: `src/StockTarget.App/StockTarget.App.csproj`의 `Version` (지금 1.0.0). 휴대폰 배포와 상관없이 그대로 둔다.
