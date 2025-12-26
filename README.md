# SwitchBot Controller

SwitchBot Cloud API を使って、登録したデバイスを **ON / OFF** できる最小構成のWindows向けGUIアプリです。  
デバイスの追加・編集は **`config.json` を直接編集**して行います（アプリ上の設定UIはありません）。

---

## Features
- デバイス一覧を表示し、各デバイスを ON / OFF
- 右側スクロールバーは **必要なときだけ表示**
- 下部に **1行ステータス**（送信中 / 結果コードなど）

---

## Requirements
- Windows
- Python 3.10+（推奨: 3.12）
- SwitchBot API Token（SwitchBotアプリから取得）

---

## Repository Layout (recommended)

```
SwitchBotController/
  src/
    switchbot_controller.py
  scripts/
    build.ps1
  requirements.txt
  config.example.json
  config.json            # local only (DO NOT COMMIT)
```

---

## Setup (Development)

```bat
cd /d C:\workspace\SwitchBotController
py -3.12 -m venv .venv
.venv\Scripts\activate
python -m pip install --upgrade pip
pip install -r requirements.txt
python src\switchbot_controller.py
```

`requirements.txt` は最小で以下です:

```txt
requests
```

---

## Configuration

### IMPORTANT
`config.json` には **APIトークンが平文で入ります**。  
**絶対にGitにコミットしないでください**（`.gitignore` 対象にすること）。

### config.example.json
まず `config.example.json` を用意して、構成のテンプレとしてコミットします。

```json
{
  "api_key": "PUT_YOUR_TOKEN_HERE",
  "devices": [
    { "name": "Curtain", "device_id": "FB711B3DD74A" },
    { "name": "LED", "device_id": "6055F9354302" }
  ]
}
```

### config.json（ローカル用）
テンプレをコピーして `config.json` を作り、`api_key` を本物に置き換えてください。

```bat
copy config.example.json config.json
```

---

## Build (EXE)

本リポジトリは **onefile（exe 1本）** でビルドします。  
実行時は **exe と同じフォルダに `config.json` を置く**運用を想定しています。

### One-command build (PowerShell)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Output:
- `dist\SwitchBotController.exe`

---

## License
Private / Personal use. (Edit as you like)
