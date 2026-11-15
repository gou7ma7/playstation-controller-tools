# LoL USB PS4 / PS5 rumble feedback

[简体中文](README.zh-CN.md) · English

> **Inspect the source and build locally before running. Do not directly run an `.exe` downloaded from the internet.** Start with the build instructions below.

An independently usable Windows x64 tray app reading LoL's local Live Client Data API and providing two-channel vibration for USB DS4 v2 or DualSense. DualSense uses its compatible rumble mode, not audio haptics or gameplay adaptive-trigger effects. It does not require Steam, a particular key layout or WASD mode. No third-party libraries or API Key are used.

## Review and build

Read [runtime](src/LoLDamageRumble.cs), [optional integration](src/LoLIntegration.cs), [config](config/FeedbackConfig.cs), [build script](scripts/build.ps1) and [tests](tests/LoLRumbleTests.cs). From this module's directory:

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1
```

Requires the Windows .NET Framework 4.x x64 C# compiler. The script validates every preset and runs synthetic behavior tests, then generates `dist/LoLDamageRumble.exe`. No downloads or controller writes occur during the build. After review, run your own executable:

```powershell
.\dist\LoLDamageRumble.exe
```

This folder can be copied and built on its own. Generated files in `dist/` are excluded from source commits.

The executable and tray use an embedded original controller/pulse icon. [Editable SVG and multi-size ICO](assets/README.md) are included in this module; the build records the icon SHA-256 in `build-info.json`. No separate icon file is needed at runtime.

## Presets and editing

**[config/FeedbackConfig.cs](config/FeedbackConfig.cs)** is the compile-time configuration, with detailed bilingual comments. Edit parameters and rebuild; nothing needs to be edited in the runtime for normal tuning.

- `default`: current user setup, 65%, all six effects.
- `gentle`: 45%, lighter hit/kill, no assist.
- `focus`: 55%, alerts only; 15% heavy loss, 20% low HP, 15% low resource.
- `celebration`: 65%, kill/assist/multikill only.

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Preset gentle -Language en
```

Choose `auto`, `zh` or `en` for the UI language. The tray shows the compiled preset, thresholds and enabled effects. Tray changes last only for the current run. See [detailed configuration](docs/configuration.md) and [feedback behavior](docs/feedback.md).

## Running and troubleshooting

Supports USB DS4 v2 `054C:09CC` and DualSense `054C:0CE6`. Bluetooth, DS4 v1, DualSense Edge and other controllers are unimplemented. Uses the Windows HID driver and requires exactly one supported gamepad. Vendor/product, gamepad HID usage and USB input/output lengths are verified before selecting the motor protocol. Motor reports leave light/player LED, adaptive-trigger, audio and microphone update flags disabled.

No match: waits quietly. Use the tray for effect toggles, intensity, testing, pause and exit. Tests from the tray really vibrate, even if that effect's automatic trigger is disabled. A different application writing motors can overwrite this app's output.

Pause, lock, suspend, disconnect or exit cancels queued effects and attempts to stop motors. Resume establishes a new baseline. No automatic startup is installed. Tray feedback preferences are temporary; the optional integration uses a separate local JSON file. Stop gracefully with the tray or `--stop`:

```powershell
.\dist\LoLDamageRumble.exe --stop
```

Optional read-only diagnostic command:

```powershell
Start-Process -FilePath .\dist\LoLDamageRumble.exe -ArgumentList '--diagnose', 'diagnostics.json' -Wait -WindowStyle Hidden
Get-Content .\diagnostics.json
```

It checks the local API and ability to open the controller output handle without sending motor commands. `--status-file <path>` writes a version 5 status snapshot with API availability, controller model/connection/error, supported device count, feedback counts and successful motor-report writes. `pulsesWritten` is the legacy pattern-start counter; `motorReportsWritten` counts completed USB writes, including stops. Reports contain no HID path, controller serial or player identity. Optional integration fields record foreground/layout state and trigger reset counts. `--test <path>` really sends a short 150 ms motor pulse and stops it; use the tray for normal pattern testing.

## Optional shared tray for LoL mapping

[Configure foreground integration](docs/integration.md) to select a local saved Steam layout and clear inherited USB DualSense trigger resistance only while the match is foreground. This is disabled unless configured; standalone rumble still works without Steam. Keep Steam elevated for the documented LoL mapping. Switching away restores automatic layout selection and stops trigger writes; other games/tools must reapply their own effects.

## Agent and references

[AGENTS.md](AGENTS.md) · [English prompt](prompts/agent.en.md) · [Chinese prompt](prompts/agent.zh-CN.md) · [MIT license](LICENSE)

[Related feedback work](docs/related-work.md). The DualSense compatible motor layout follows the [Linux PlayStation HID driver](https://kernel.googlesource.com/pub/scm/linux/kernel/git/stable/linux-stable/+/master/drivers/hid/hid-playstation.c): USB report `0x02`, compatible-vibration / haptics-select flags and independent right/left strengths. DS4 retains its existing USB report `0x05`.

[Riot Live Client Data API](https://developer.riotgames.com/docs/lol#game-client-api_live-client-data-api). Net health loss is inferred; no CC, vision, cooldown or ammunition feedback is implemented. Review the limitations before adding new triggers.
