# MiniMes 전체 실행 스크립트: API, 예측 서버, 수집기, 작업자 프로그램을 각각 새 창으로 띄운다.
# 사용법 (어느 폴더에서나):  powershell -ExecutionPolicy Bypass -File scripts\start-all.ps1
# 종료: 각 창에서 Ctrl+C
# 주의: 이미 켜져 있는 API(5168), 예측 서버(8000)가 있으면 포트가 겹치므로 먼저 종료한다.

$root = Split-Path -Parent $PSScriptRoot

# 예측 서버용 가상환경이 없으면 한 번만 만든다.
$venvPython = Join-Path $root "MiniMes.Predictor\.venv\Scripts\python.exe"
if (-not (Test-Path $venvPython)) {
    Write-Host "예측 서버 가상환경을 만듭니다 (처음 한 번만)..."
    python -m venv (Join-Path $root "MiniMes.Predictor\.venv")
    & $venvPython -m pip install -r (Join-Path $root "MiniMes.Predictor\requirements.txt")
}

function Start-Window($title, $workDir, $command) {
    Start-Process powershell -WorkingDirectory $workDir -ArgumentList @(
        "-NoExit", "-Command", "`$Host.UI.RawUI.WindowTitle = '$title'; $command")
}

Start-Window "MiniMes API"        (Join-Path $root "MiniMes.Api")        "dotnet run"
Start-Sleep -Seconds 8   # API가 먼저 떠야 다른 프로그램이 연결된다
Start-Window "MiniMes Predictor"  (Join-Path $root "MiniMes.Predictor")  "& '$venvPython' -m uvicorn app.main:app --port 8000"
Start-Window "MiniMes Collector"  (Join-Path $root "MiniMes.Collector")  "dotnet run"
Start-Window "MiniMes OperatorUI" (Join-Path $root "MiniMes.OperatorUI") "dotnet run"

Write-Host "실행했습니다. 웹 화면: http://localhost:5168/"
Write-Host "순서: 화면 1에서 '표준 C/T 재계산' -> '예측 모델 재학습'"