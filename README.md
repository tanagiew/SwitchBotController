# SwitchBot Controller

- SwitchBot Cloud API を使って、登録したデバイスを ON / OFF できる最小構成のWindows向けGUIアプリです。  
- SwitchBotデバイスにのみ対応し、Hub経由の赤外線リモコンには対応していません。
- デバイスの追加・編集は **`config.json` を直接編集**して行います。WinUI 3版では、画面右上の設定ボタンから使用するJSONファイルを選べます。
- WinUI 3版は起動時・再読み込み時に、`config.json`へ登録された全デバイスの状態を取得します。取得できない機器があっても、ほかの機器の表示と操作は継続します。

<img src="./docs/images/screen.jpg" width=350 alt="screen" />

## 使用方法

- Releaseから最新の実行ファイルのzip (SwitchBotController_XX.zip)をダウンロードしてください。
- zipを解凍後、ご自身の環境に合わせたconfig.jsonを作成し、SwitchBotController.exeと同じ階層に配置してください。
- SwitchBotController.exeを実行してください。

### config.json

1. SwitchBotのスマホアプリを開き、ログインします。
2. プロフィール > 設定 > 基本データ の **アプリバージョン** を10回タップします。
3. 開発者向けオプションをタップすると表示される **トークン** をメモしておいてください。

<img src="./docs/images/setting.jpg" width="250" alt="setting" />
<img src="./docs/images/developerOptions.jpg" width="250" alt="developerOptions" />


4. SwitchBotデバイスの 設定 > デバイス情報 を開き **BLE MAC** をメモしておいてください。

<img src="./docs/images/deviceInformation.jpg" width="250" alt="deviceInformation" />


5. config.json.exampleに倣ってconfig.jsonを新規作成してください。
  - api_token : メモした **トークン**
  - name : アプリ上で表示するデバイス名
  - ble_mac : デバイスごとの **BLE MAC** （コロンを除く）

> WinUI 3移行版は、既存環境との互換性のため `api_key` / `device_id` 形式も読み込めます。

WinUI 3版で選択したファイルの場所は、MSIX版ではWindows管理のアプリローカル領域、非MSIX版では `%LOCALAPPDATA%\SwitchBotController\settings.json` に保存されます。APIトークンやデバイス情報はコピーせず、元の `config.json` にだけ保持します。選択したJSONを読み込めない場合は、現在使用中の設定を維持します。

画面右上の設定ボタンから、現在使用中の `config.json` を既定のエディターで直接開くこともできます。編集後は再読み込みボタンで反映してください。

---

## WinUI 3版のビルド方法

### Requirements

- Windows 10 1809以降
- .NET 10 SDK
- Visual Studio 2026のWinUIアプリ開発環境とWindows App SDK

```powershell
dotnet restore .\SwitchBotController.sln
dotnet test .\tests\SwitchBotController.Core.Tests\SwitchBotController.Core.Tests.csproj
dotnet build .\SwitchBotController.sln --configuration Debug --property:Platform=x64
dotnet run --project .\src\SwitchBotController.App\SwitchBotController.App.csproj --property:Platform=x64
```

## Python版のビルド方法

ご自身でビルドする場合は下記情報を参考にしてください。

### Requirements
- Windows
- Python 3.10+（推奨: 3.12）

### Repository Layout (recommended)

```
SwitchBotController/
  src/
    switchbot_controller.py
  scripts/
    build.ps1
  assets/
    icon.ico
  requirements.txt
  config.json            # local only (DO NOT COMMIT)
```

### Setup
```bat
cd /d C:\workspace\SwitchBotController
py -3.12 -m venv .venv
.venv\Scripts\activate
python -m pip install --upgrade pip
pip install -r requirements.txt
python src\switchbot_controller.py
```

### One-command build
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Output:
- `dist\SwitchBotController.exe`
