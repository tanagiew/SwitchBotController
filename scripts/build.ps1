Param(
  [ValidateSet("Release","Debug")]
  [string]$Mode = "Release",

  # Python launcher version selector (uses installed list like: py -3.12)
  [string]$Py = "3.12"
)

$ErrorActionPreference = "Stop"

# Repo root = one level above /scripts
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$VenvDir = Join-Path $RepoRoot ".venv"
$VenvPython = Join-Path $VenvDir "Scripts\python.exe"
$DistDir = Join-Path $RepoRoot "dist"
$BuildDir = Join-Path $RepoRoot "build"

Write-Host "== SwitchBotController build =="
Write-Host "Repo: $RepoRoot"
Write-Host "Mode: $Mode"
Write-Host "Python: py -$Py"

# 1) Create venv if missing
if (-not (Test-Path $VenvPython)) {
  Write-Host "`n[1/6] Creating venv (.venv)..."
  & py "-$Py" -m venv .venv
}

# 2) Upgrade pip + install deps
Write-Host "`n[2/6] Installing dependencies..."
& $VenvPython -m pip install --upgrade pip | Out-Host
& $VenvPython -m pip install -r requirements.txt | Out-Host

# 3) Ensure pyinstaller is installed (kept out of requirements.txt by default)
Write-Host "`n[3/6] Installing pyinstaller..."
& $VenvPython -m pip install pyinstaller | Out-Host

# 4) Clean old build artifacts
Write-Host "`n[4/6] Cleaning dist/build..."
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
if (Test-Path $BuildDir) { Remove-Item $BuildDir -Recurse -Force }
Get-ChildItem -Filter "*.spec" -Path $RepoRoot -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

# 5) Build
Write-Host "`n[5/6] Building EXE (onefile)..."
$CommonArgs = @(
  "--onefile",
  "--name", "SwitchBotController",
  "src\switchbot_controller.py"
)

if ($Mode -eq "Release") {
  # console無し（GUIアプリ想定）
  $CommonArgs = @("--noconsole") + $CommonArgs
}

& $VenvPython -m PyInstaller @CommonArgs | Out-Host

# 6) Post steps: copy example config next to exe (optional but handy)
Write-Host "`n[6/6] Post steps..."
$OutDir = Join-Path $DistDir "."
$ExePath = Join-Path $OutDir "SwitchBotController.exe"

if (-not (Test-Path $ExePath)) {
  throw "Build failed: EXE not found at $ExePath"
}

# Copy example config so the folder is self-explanatory.
if (Test-Path (Join-Path $RepoRoot "config.example.json")) {
  Copy-Item (Join-Path $RepoRoot "config.example.json") -Destination $OutDir -Force
}

Write-Host "`nOK: $ExePath"
Write-Host "Tip: Place your config.json next to the EXE when you run it."
