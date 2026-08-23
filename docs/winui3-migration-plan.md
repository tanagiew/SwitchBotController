# WinUI 3 migration plan

## Goal

Rebuild the desktop UI with C# and WinUI 3 while retaining the existing Python app as the behavior reference until feature parity is confirmed.

## Solution structure

- `src/SwitchBotController.App`: WinUI 3 MVVM desktop application
- `src/SwitchBotController.Core`: configuration and SwitchBot API logic independent of the UI
- `tests/SwitchBotController.Core.Tests`: unit tests that do not call the real SwitchBot API
- `tests/SwitchBotController.App.Tests`: ViewModel tests for cancellation, concurrency, and device-level failure isolation

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
- Verified an x64 build with zero warnings and errors, sixteen passing Core tests, and three passing App ViewModel tests.
- Verified the legacy configuration with live LED ON/OFF commands (`HTTP 200`, SwitchBot status `100 success`) and confirmed that the physical device turned on and off.
- Completed user acceptance from the WinUI screen, including device loading, progress text, HTTP 200 status, success notification, and physical ON/OFF behavior.
- Added a native settings flyout that opens the active `config.json` or selects and validates another file before switching.
- Persisted only the selected absolute file path using an atomic settings-file replacement: ApplicationData for MSIX and LocalAppData for unpackaged runs. Tokens and device details remain in the external configuration.
- Preserved the last working in-memory configuration when a newly selected file is invalid.
- Completed user acceptance for the settings flyout, immediate configuration reload, and restoration of the selected path after restarting the app.
- Added bounded-concurrency status retrieval for every device in the selected configuration, without changing the common ON/OFF command UI by product type.
- Added response-field-based state summaries, per-device failure isolation, and a status refresh for only the operated device after successful commands.
- Simplified the header and device cards, changed the initial window to a narrower vertical layout, added direct config opening, and applied native InfoBar success/error severity colors.
- Replaced the generated placeholder app assets with the user-adjusted SwitchBot icon and generated exact-size Windows assets for the title bar, taskbar, tiles, and splash screen.
- Added scale-specific title-bar images and a multi-resolution ICO to avoid Windows downscaling a single 256px source for small icon surfaces.
- Added bounded post-command status observation: devices reporting position/movement are refreshed until movement stops, while power-only devices perform one delayed refresh.
- Validated both HTTP and SwitchBot body status codes for commands so an API-level rejection is not reported as success.
- Cancelled stale status refresh and post-command polling when the configuration changes, while preserving per-device failure isolation and the four-request concurrency limit.

## Next milestone

Finalize the README, remove the superseded Python implementation, choose the distribution format, and verify the release ZIP on a clean launch path.
