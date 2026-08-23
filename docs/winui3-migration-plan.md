# WinUI 3 migration plan

## Goal

Rebuild the desktop UI with C# and WinUI 3 while retaining the existing Python app as the behavior reference until feature parity is confirmed.

## Solution structure

- `src/SwitchBotController.App`: WinUI 3 MVVM desktop application
- `src/SwitchBotController.Core`: configuration and SwitchBot API logic independent of the UI
- `tests/SwitchBotController.Core.Tests`: unit tests that do not call the real SwitchBot API

## Compatibility and safety rules

- Accept both existing configuration variants: `api_token`/`ble_mac` and the legacy `api_key`/`device_id` names.
- Preserve the existing SwitchBot API v1.0 ON/OFF behavior before considering an API upgrade.
- Never commit `config.json` or an API token.
- Do not remove the Python implementation until the C# application reaches feature parity.
- Use fake configuration values and a fake HTTP handler in automated tests.

## Completed milestone

- Replaced the generated counter page with a native WinUI device-card layout.
- Connected configuration loading, reload, ON/OFF commands, progress, and error status to the Core layer.
- Added DPI-aware initial window sizing and limited configuration lookup to explicit locations.
- Verified an x64 build with zero warnings and errors and six passing Core tests.
- Verified the legacy configuration with live LED ON/OFF commands (`HTTP 200`, SwitchBot status `100 success`) and confirmed that the physical device turned on and off.
- Completed user acceptance from the WinUI screen, including device loading, progress text, HTTP 200 status, success notification, and physical ON/OFF behavior.

## Next milestone

Review and commit the completed migration checkpoint, and then add settings/configuration selection before choosing the final packaging format.
