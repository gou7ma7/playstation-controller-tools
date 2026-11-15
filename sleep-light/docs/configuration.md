# Configuration and verification

[简体中文](configuration.zh-CN.md)

All settings are compiled from `config/LightConfig.cs`; edit, exit the running watcher, rebuild, then restart it. No runtime configuration file or shared module is needed.

- `EnableIdleLight`: default `true`. Enable controller inactivity control. Resume/unlock alone does not restore color. Set `false` for the original sleep-only policy; the input reader is not started in that mode.
- `IdleTimeoutSeconds`: `10..3600`, default `180`. Count from the first valid input baseline or last controller activity. Stationary reports, mouse and keyboard activity do not reset it. After reconnecting or a long UI gap, use a fresh baseline instead of queued old input.
- `StickDeadzone`: `4..96`, default `32`. Per-axis distance from center 128; an axis outside this range counts as active while held. Inside it, small movement is ignored. Increase this for drift; too large a value delays recognition of gentle movement. Severe drift outside the deadzone can prevent idle off.
- `TriggerThreshold`: `1..255`, default `12`. Analog L2/R2 at or above this counts as use. Their digital bits alone are ignored to avoid tiny trigger noise. Button/d-pad presses and releases count, as do PS and touchpad clicks; sensor data and touch coordinates do not.
- `IdleOffRefreshMs`: `0` or `1000..60000`, default `5000`. Reassert off while idle and awake. `0` sends only the initial successful off. Lower values counter other light writers sooner but increase USB writes; nothing can run this during actual OS sleep. Color is never periodically reasserted during active use.
- `IdleWriteRetryMs`: `500..10000`, default `3000`. Time between failed idle-mode output attempts. Reads must remain valid and fresh; a disconnect or ambiguous target prevents writes.
- `RestoreOnResume`: default `true`, **only used when `EnableIdleLight = false`**. Restore after interactive resume/unlock. If `false`, sleep-only mode never restores color. It does not disable controller-triggered color in idle mode.
- `Red`, `Green`, `Blue`: integers `0..255`; default `0,0,255`. For dim blue use `0,0,32`; for purple use `96,0,96`. These are raw RGB levels, not Steam's brightness slider. All-zero also keeps it dark after resume.
- `ControlDualSensePlayerLeds`: default `true`, DualSense only. Set RGB and white player LEDs together; off clears all five white LEDs. Set `false` to leave player LEDs entirely to other software. The microphone mute indicator is never changed.
- `DualSensePlayerLeds`: `0..31`, default `4` (center LED). Five-bit restore mask, with `1`, `2`, `4`, `8`, `16` selecting individual player LEDs. `0` restores RGB without white LEDs; `31` lights all five. Applied only when player LED control is enabled and requested RGB is nonzero; this cannot recover Steam's previous LED pattern.
- `RestoreDelayMs`: `0..10000`, default `1500`. Sleep-only mode's delay after resume/unlock so USB can become available.
- `RestoreRetryWindowMs`: `500..30000`, default `5000`. Sleep-only retry window after the delay. After expiry, wait for the next sleep cycle.
- `RestoreRetryIntervalMs`: `100..5000`, default `500`. Sleep-only failed restoration retry interval. Success ends retries immediately.
- `OffWriteAttempts`: `1..3`, default `2`. Brief repetitions during the suspend callback. This does not create a task that runs during sleep.
- `OffRepeatDelayMs`: `0..100`, default `50`. Gap between those repeated off reports.
- `UsbWriteTimeoutMs`: `50..500`, default `200`. Timeout before requesting cancellation of a pending write. The buffer and OVERLAPPED memory remain alive until the OS completes/cancels the operation; a faulty driver can delay cancellation, so this is not an absolute wall-clock bound.
- `EnableLocalLog`: default `true`. Set `false` to stop writing the local rotating log. No identifying device/game data is logged.

Example: keep the original default blue but reduce intensity, and wait longer for USB:

```csharp
internal static readonly int Red = 0, Green = 0, Blue = 32;
internal static readonly int RestoreDelayMs = 2500;
```

Build from the module folder:

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Language en
```

The build validates ranges and runs synthetic checks for report fields, device capability filtering, duplicate notifications, deferred unlock restoration, pending-restore cancellation, and the S3 → automatic wake → S4 sequence. No native USB call is made by the test runner.

It also checks DS4 and DualSense USB input layouts, format rejection, noise filtering, held controls, the exact 180-second deadline, retries, controller-triggered restore and disconnect/suspend cancellation. The parser and idle policy are in `src/ControllerActivity.cs`; shared, overlapped `ReadFile` with cancellation is in `src/ControllerInput.cs`. See [Microsoft's HID input guidance](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/obtaining-hid-reports) and [Linux's PlayStation report layouts](https://github.com/torvalds/linux/blob/master/drivers/hid/hid-playstation.c). Windows advertises 48 bytes for DualSense USB output on the supported gamepad collection; Linux's padded USB output struct has a different length. Do not substitute DS4's 32-byte layout or Bluetooth's 78-byte layout.

Read-only input probe (no light reports):

```powershell
Start-Process .\dist\ControllerLight.exe -ArgumentList '--input-diagnose 3 dist/input-diagnostic.json' -WindowStyle Hidden -Wait
Get-Content .\dist\input-diagnostic.json
```

This reports aggregate counts, never button values. For an actual idle test, leave the running watcher's controller untouched for three minutes, then use a button/stick. Mouse/keyboard operation should leave it dark. Repeat with Steam running. A successful off write alone does not measure the physical light color.

For a real sleep check, manually test S3 and S4 separately. `suspend_off ... outcome=written` means Windows completed the reports, not that the visible result was measured. Background resumes should record `automatic_resume_no_light_write`. Idle mode waits for controller use to restore; sleep-only mode restores after user resume/unlock.

## Diagnostic log

The primary file is `logs/events.log` beside your executable, with `%LOCALAPPDATA%\ControllerLight\events.log` as a fallback when the program folder cannot be written. Use **Open log** in the tray to find the active file. `EnableLocalLog = false` disables these records. Above 1 MiB the log rotates to `events.log.old`; keep both when inspecting a cycle.

Every record has UTC/local time, an ephemeral `run` ID, sequence, process/thread number and elapsed time. These are for event correlation, not device/account identifiers. No device path, serial, account or match data is recorded. Old versions' AppData logs are not automatically deleted or merged.

- `power_received` / `power_handled`: Windows notification and the state before/after processing. ID 4 is suspend, 18 is automatic resume, 7 is interactive resume.
- `session_received` / `session_apply_begin` / `session_applied`: distinguish callback arrival from queued UI handling; report lock/unlock and current policy state.
- `suspend_off_begin` / `suspend_off`: beginning/completion of the light-off operation, result and total duration.
- `usb_operation_*`, `usb_enumeration_*`, `usb_write_attempt_*`: operation, target count, requested RGB, attempt result, Win32 error and duration. `usb_cancel_wait_*` brackets cancellation cleanup, which may be delayed by the driver.
- `usb_light_target`: selected controller kind and output length; no device identity/path is recorded. Diagnostic JSON and input connection/heartbeat records also distinguish `DualShock4V2` from `DualSense`.
- `interactive_resume_restore_scheduled`, `resume_restore_*`: restore delay, RGB, write result, retry or completion. Background automatic resume still sends no restore color.
- `input_status`, `input_connected_no_light_write`, `idle_input_baseline_no_light_write`, `idle_input_lost_no_light_write`: input access, fresh baseline and unavailable input. A baseline sends no color.
- `idle_timeout_off`: off write result and inactivity duration. It also reasserts off after sleep/reconnect when the policy was already dark; an `idle_ms` below the timeout distinguishes that case.
- `idle_controller_activity_restore_requested`, `idle_activity_color`: controller activity while dark, followed by configured color write. No raw input is recorded.
- `idle_off_refresh`, `idle_input_heartbeat_no_light_write`: repeated off summary and total input reports. A high report count with increasing `idle_ms` is expected for a stationary controller. Heartbeats do not perform USB IO.
- `heartbeat_no_usb_write`: one-minute UI-loop status. `ui_timer_gap` records wall-clock gaps over three seconds; sleep, UI blocking or clock changes can explain a gap. This is not an independent sleep detector and never sends USB commands.
- `device_change_no_usb_write`: generic Windows device-change notification; it does not identify which device changed or poll USB.
- `manual_test_begin` / `manual_test_completed`: actual requested test duration, separate from real suspend notifications. A command-line test has a separate run/process number.

For example, a suspend beginning before sleep but completing only after wake suggests delayed handling or IO. A completed off write followed by a visibly different color does not alone identify who changed it: correlate the timing with your observation and Windows power events. These logs do not read the actual lightbar or trace Steam's writes.

If an application overwrites the light shortly after our command, changing the delay/repetitions may help but is not a guarantee. Do not disable USB power saving, install HID-hiding drivers, or replace a working Steam mapping merely as a default troubleshooting step. Keep any further change tied to observed behavior.
