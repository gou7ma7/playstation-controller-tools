# PS4 / PS5 sleep and idle light

[简体中文](README.zh-CN.md) · English

> **Review the source and build locally. Do not directly run an `.exe` downloaded from the internet.**

An independent Windows tray utility for **one USB DualShock 4 v2 (`054C:09CC`) or DualSense (`054C:0CE6`)**. By default it turns the lights off after **three minutes without controller input** and restores configured blue when the controller is used again. It also sends off before sleep/hibernate. It needs neither Steam, League of Legends, nor the other repository modules. The executable remains `DS4SleepLight.exe` to preserve existing launch and login shortcuts.

## Behavior

- Starting or exiting the watcher does not change the light.
- On `PBT_APMSUSPEND`, send RGB zero using the detected controller's protocol. DS4 uses a 32-byte report `0x05` with lightbar/blink flags `0x06`; DualSense uses the Windows 48-byte report `0x02` with RGB/player-LED flags. Motors, adaptive triggers, audio and microphone mute remain untouched. USB power stays connected.
- DualSense's white player indicators turn off with RGB by default and restore as one center LED. Set `ControlDualSensePlayerLeds = false` to leave them to Steam, or change the restore mask. The microphone mute light is preserved because it indicates microphone state.
- On a background automatic resume, keep the light off. This includes a timer wake that may immediately transition from S3 to S4.
- **Default idle mode:** after 180 seconds without controller use, send off. A button press/release, d-pad, stick past the deadzone or trigger past its threshold counts as use; a held control also counts. Mouse/keyboard activity, neutral USB packets, counters, motion sensors, battery updates and touchpad movement do not. A touchpad click counts as a button.
- While idle and the OS is awake, reassert off every five seconds to counter other software changing the light. On controller activity, restore the configured RGB, **blue (`0,0,255`) by default**. While actively used, no periodic color commands are sent, so Steam may change it afterward.
- Resume/unlock alone does not restore color in idle mode. Input is reopened with a fresh baseline; if the light was turned off for sleep, off is reasserted before waiting for actual controller input. Locked sessions can turn off but cannot restore color from controller input.
- Set `EnableIdleLight = false` and rebuild for the original sleep-only mode: restore after interactive resume/unlock, with a 1.5-second delay and a brief retry window. `RestoreOnResume` applies only to that mode.
- The program cannot read or automatically restore Steam's last RGB/brightness setting, and cannot execute off refreshes while Windows is asleep. Idle mode helps before sleep but does not guarantee that another application's final write or a USB/firmware transition cannot relight it.

USB DS4 v1, DualSense Edge, Bluetooth controllers and multiple connected target controllers are unsupported. Multiple matching controllers, including one DS4 plus one DualSense, result in no writes. Disconnect extra targets and virtual copies before testing. Attribute, gamepad usage and input/output length checks select the report format; Bluetooth reports are not accepted.

## Review, build and run

Read [source](src/Program.cs), [power policy and USB report](src/SleepPolicy.cs), [input parsing and idle policy](src/ControllerActivity.cs), [shared input reader](src/ControllerInput.cs), [native USB output](src/NativeHid.cs), [configuration](config/SleepLightConfig.cs), [build script](scripts/build.ps1) and [offline tests](tests/SleepLightTests.cs).

From the repository root, in Windows PowerShell:

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\build.ps1
.\sleep-light\dist\DS4SleepLight.exe
```

Requires Windows x64 and the .NET Framework 4.x C# compiler. No downloaded packages, administrator rights or added device driver. The build runs offline checks and produces `dist/DS4SleepLight.exe`, `test-results.json` and `build-info.json`; it does not start the watcher or send USB commands.

The folder is standalone: after copying `sleep-light/` elsewhere, run `scripts/build.ps1` there. `-Language auto` uses the Windows UI language; `zh` and `en` force Chinese or English. Stop a running watcher before rebuilding its executable.

The executable and tray use an embedded original controller/moon icon. [Editable SVG and multi-size ICO](assets/README.md) are included in this module; the build records the icon SHA-256 in `build-info.json`. No separate icon file is needed at runtime.

## Optional login autostart

After reviewing and building your own executable:

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Enable
```

This adds only a **current-user Startup shortcut** pointing to your local executable. It neither launches it immediately nor installs a service/driver. Keep the executable at that location; run it once for the current session. The watcher allows one instance per session.

To undo:

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Disable
.\sleep-light\dist\DS4SleepLight.exe --stop
```

The tray menu also provides Exit and a connection check that does not change lights. Use `-Executable` with the autostart script when you keep your compiled binary elsewhere.

## Configure and verify

Edit [SleepLightConfig.cs](config/SleepLightConfig.cs) and rebuild. See the [configuration and verification guide](docs/configuration.md).

Read-only USB diagnosis, from the repository root:

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--diagnose sleep-light/dist/diagnostic.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\diagnostic.json
```

Optional **physical light test**: this changes the light for three seconds and then sends the configured restore color; it does not suspend the computer.

Stop the watcher first so idle off refreshes do not interfere with this test, then restart it afterward.

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--test-cycle 3 sleep-light/dist/cycle-result.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\cycle-result.json
```

Successful HID write completion does not independently confirm the visible light or full sleep-cycle behavior. For the real test, start the watcher, manually choose Windows Sleep or Hibernate, observe the light, then resume/unlock and observe its return. Do not automate suspending another person's active PC as a test.

For idle mode, leave the controller untouched for at least three minutes, observe off, then press a button or move a stick and observe configured blue. Verify again with Steam running. Offline checks cover noise filtering, held controls, the 180-second boundary, retries, reconnection and suspend cancellation; they do not confirm visible behavior.

For USB DS4 v2, the original user confirmed **three-minute idle off → controller input → blue restore** with Steam running; native logs also recorded both writes. A stationary probe read 751 reports in three seconds with zero activity samples. These observations apply to that USB controller, not every device.

USB DualSense support was checked with Steam running: a stationary read-only probe received 752 reports in three seconds with zero activity samples. The user confirmed that a ten-second physical test turned off both RGB and white player LEDs, then restored configured blue and the center white LED. A full DualSense sleep/hibernate transition remains a separate physical test.

The original sleep-only version previously passed a user-observed S3 cycle. Later sleep failures were reported despite successful pre-suspend writes; the idle test does not establish reliable S4 or automatic S3-to-S4 behavior. Full sleep behavior still requires separate observation.

## Coexistence and limits

For DS4, the repository's rumble reports enable only motors (`0x01`), while this module enables only lightbar/blink (`0x06`). DualSense lighting enables only RGB and optional player indicators; motor, haptic, trigger, audio, mute and animation flags stay clear. Shared handles allow coexistence, but other lighting writers can still overwrite color. The rumble module remains DS4-only; adding DualSense lighting does not add DualSense game feedback.

Other software that also writes the lightbar, exclusive HID access, device removal during transition, or missing/delayed Windows power notifications may prevent a visible response. The program does not keep the computer awake, disconnect USB, change BIOS/power settings, remap buttons, read game data or read game memory. Physical behavior must be checked on your device; no guarantee is made solely from a successful write.

The optional rotating log is `logs/events.log` beside the executable; if that folder is not writable, it falls back to `%LOCALAPPDATA%\DS4SleepLight\events.log`. The tray's **Open log** command opens the active file. It records UTC/local timestamps, a per-run ID and sequence, power/session states, RGB commands and USB timing, plus input connection, idle-off/activity-restore transitions, aggregate report counts, refresh counts and long UI timer gaps. Successful repeated idle-off writes are summarized at most once per minute; failures are recorded. Heartbeat/device-change records themselves do not perform USB IO. A log above 1 MiB rotates to `events.log.old`.

These records help distinguish a missing/delayed notification from a failed or delayed USB write. They cannot read the actual light color, identify another application's light commands, or prove that a successful write stayed visible. Logs and diagnosis/test files omit device paths, serials, accounts and game data. See [log interpretation](docs/configuration.md#diagnostic-log).

[Agent context](AGENTS.md) · [Agent prompts](prompts/agent.en.md) · [MIT license](LICENSE)
