# 휴대폰에 새 빌드를 올린다: 패치 버전 +1 → Release APK 빌드 → adb 덮어쓰기 설치(데이터 유지).
# 빌드나 설치가 실패하면 버전을 되돌린다(올라간 번호가 휴대폰에 실제로 올라간 버전과 늘 같게).
#
#   powershell -ExecutionPolicy Bypass -File scripts\Deploy-Phone.ps1 -Device 192.168.0.102:33095
#
# 버전 규칙은 CLAUDE.md 참고.
param(
    [Parameter(Mandatory = $true)][string]$Device
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $root 'src\StockTarget.Mobile\StockTarget.Mobile.csproj'
$adb = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$utf8 = New-Object System.Text.UTF8Encoding $false # csproj는 BOM 없는 UTF-8

$original = [IO.File]::ReadAllText($csproj, $utf8)
$m = [regex]::Match($original, '<ApplicationDisplayVersion>(\d+)\.(\d+)\.(\d+)</ApplicationDisplayVersion>')
if (-not $m.Success) { throw 'ApplicationDisplayVersion(major.minor.patch)를 찾지 못했습니다' }
$major = [int]$m.Groups[1].Value; $minor = [int]$m.Groups[2].Value; $patch = [int]$m.Groups[3].Value + 1
if ($patch -gt 99 -or $minor -gt 99) { throw '패치 · 마이너는 99까지(빌드 번호 = major*10000 + minor*100 + patch)' }
$version = "$major.$minor.$patch"
$code = $major * 10000 + $minor * 100 + $patch

$updated = $original -replace '<ApplicationDisplayVersion>[^<]*</ApplicationDisplayVersion>', "<ApplicationDisplayVersion>$version</ApplicationDisplayVersion>" `
                     -replace '<ApplicationVersion>[^<]*</ApplicationVersion>', "<ApplicationVersion>$code</ApplicationVersion>"
[IO.File]::WriteAllText($csproj, $updated, $utf8)
Write-Host "버전 $version (빌드 번호 $code)"

try {
    & $adb connect $Device | Out-Host
    dotnet publish $csproj -f net9.0-android -c Release -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "빌드 실패 (exit $LASTEXITCODE)" }

    $apk = Join-Path $root 'src\StockTarget.Mobile\bin\Release\net9.0-android\publish\com.stocktarget.mobile-Signed.apk'
    $out = & $adb -s $Device install -r $apk 2>&1 | Out-String
    Write-Host $out
    if ($out -notmatch 'Success') { throw '설치 실패' }

    $installed = & $adb -s $Device shell dumpsys package com.stocktarget.mobile | Select-String 'versionName=' | Select-Object -First 1
    Write-Host "휴대폰 설치 버전: $($installed.ToString().Trim())"
}
catch {
    [IO.File]::WriteAllText($csproj, $original, $utf8) # 실패하면 버전을 되돌린다
    Write-Host "버전을 $major.$minor.$($patch - 1)(으)로 되돌렸습니다"
    throw
}
