$ErrorActionPreference = "Stop"

# repo root = ../scripts
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$VenvDir = Join-Path $RepoRoot ".venv"
$Python  = Join-Path $VenvDir "Scripts\python.exe"
$Icon    = Join-Path $RepoRoot "assets\icon.ico"

Write-Host "== Build SwitchBotController =="

# 0) icon check (exist + real ICO)
if (-not (Test-Path $Icon)) {
  throw "Icon not found: $Icon"
}

# ICO header check: must start with 00 00 01 00
$bytes = [System.IO.File]::ReadAllBytes($Icon)
if ($bytes.Length -lt 4 -or $bytes[0] -ne 0 -or $bytes[1] -ne 0 -or $bytes[2] -ne 1 -or $bytes[3] -ne 0) {
  throw "Invalid ICO file (header mismatch). Make sure assets/icon.ico is a real .ico (not a PNG renamed)."
}

# 1) venv
if (-not (Test-Path $Python)) {
  Write-Host "[1/4] Create venv (.venv)"
  & py -3.12 -m venv .venv
}

# 2) deps
Write-Host "[2/4] Install deps"
& $Python -m pip install --upgrade pip | Out-Host
& $Python -m pip install -r requirements.txt | Out-Host
& $Python -m pip install pyinstaller | Out-Host

# 3) clean
Write-Host "[3/4] Clean dist/build"
Remove-Item -Recurse -Force dist, build -ErrorAction SilentlyContinue
Remove-Item -Force *.spec -ErrorAction SilentlyContinue

# 4) build (onefile)
Write-Host "[4/4] PyInstaller (onefile)"
& $Python -m PyInstaller `
  --onefile `
  --noconsole `
  --name SwitchBotController `
  --icon $Icon `
  src\switchbot_controller.py | Out-Host

Write-Host "OK: dist\SwitchBotController.exe"
