# Configuration and verification

[简体中文](configuration.zh-CN.md)

All settings are compiled from `config/SleepLightConfig.cs`; edit, exit the running watcher, rebuild, then restart it. No runtime configuration file or shared module is needed.

- `RestoreOnResume`: default `true`. Restore the configured color after interactive resume/unlock. `false` leaves the light off until another application writes it.
- `Red`, `Green`, `Blue`: integers `0..255`; default `0,0,255`. For dim blue use `0,0,32`; for purple use `96,0,96`. These are raw RGB levels, not Steam's brightness slider. All-zero also keeps it dark after resume.
- `RestoreDelayMs`: `0..10000`, default `1500`. Delay following interactive resume/unlock so USB can become available. Increasing it helps when the controller enumerates slowly.
- `RestoreRetryWindowMs`: `500..30000`, default `5000`. Retry window after the initial delay. After expiry, wait for the next sleep cycle; no continuous writes.
- `RestoreRetryIntervalMs`: `100..5000`, default `500`. Time between failed restore attempts. Success ends retries immediately.
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

For a real check, keep the watcher running and manually test S3 sleep and S4 hibernate separately. Observe both off and restored color, and open the active log from the tray. `suspend_off ... outcome=written` means Windows completed the reports, not that the visible result was independently measured. Background resumes should record `automatic_resume_no_light_write`; no restore should happen until user resume/unlock.

## Diagnostic log

The primary file is `logs/events.log` beside your executable, with `%LOCALAPPDATA%\DS4SleepLight\events.log` as a fallback when the program folder cannot be written. Use **Open log** in the tray to find the active file. `EnableLocalLog = false` disables these records. Above 1 MiB the log rotates to `events.log.old`; keep both when inspecting a cycle.

Every record has UTC/local time, an ephemeral `run` ID, sequence, process/thread number and elapsed time. These are for event correlation, not device/account identifiers. No device path, serial, account or match data is recorded. Old versions' AppData logs are not automatically deleted or merged.

- `power_received` / `power_handled`: Windows notification and the state before/after processing. ID 4 is suspend, 18 is automatic resume, 7 is interactive resume.
- `session_received` / `session_apply_begin` / `session_applied`: distinguish callback arrival from queued UI handling; report lock/unlock and current policy state.
- `suspend_off_begin` / `suspend_off`: beginning/completion of the light-off operation, result and total duration.
- `usb_operation_*`, `usb_enumeration_*`, `usb_write_attempt_*`: operation, target count, requested RGB, attempt result, Win32 error and duration. `usb_cancel_wait_*` brackets cancellation cleanup, which may be delayed by the driver.
- `interactive_resume_restore_scheduled`, `resume_restore_*`: restore delay, RGB, write result, retry or completion. Background automatic resume still sends no restore color.
- `heartbeat_no_usb_write`: one-minute UI-loop status. `ui_timer_gap` records wall-clock gaps over three seconds; sleep, UI blocking or clock changes can explain a gap. This is not an independent sleep detector and never sends USB commands.
- `device_change_no_usb_write`: generic Windows device-change notification; it does not identify which device changed or poll USB.
- `manual_test_begin` / `manual_test_completed`: actual requested test duration, separate from real suspend notifications. A command-line test has a separate run/process number.

For example, a suspend beginning before sleep but completing only after wake suggests delayed handling or IO. A completed off write followed by a visibly different color does not alone identify who changed it: correlate the timing with your observation and Windows power events. These logs do not read the actual lightbar or trace Steam's writes.

If an application overwrites the light shortly after our command, changing the delay/repetitions may help but is not a guarantee. Do not disable USB power saving, install HID-hiding drivers, or replace a working Steam mapping merely as a default troubleshooting step. Keep any further change tied to observed behavior.
