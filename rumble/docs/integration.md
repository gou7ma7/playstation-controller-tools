# Optional LoL foreground integration

[简体中文](integration.zh-CN.md)

The feedback app can also select a saved Steam Input layout while the actual LoL match window is foreground, and clear inherited USB DualSense trigger resistance. This is optional: without `lol-integration.json` beside the executable, rumble remains independent of Steam and the mapping module. Feedback thresholds and waveforms still come from compile-time `FeedbackConfig.cs`.

## Configure after building

Review [LoLIntegration.cs](../src/LoLIntegration.cs), then copy [the disabled example](../config/lol-integration.example.json) beside your locally built executable as `lol-integration.json`. Edit that private file and restart the app. Do not copy another computer's non-Steam AppID.

- `enabled`: boolean, default `false`. Enable only after filling in a real Steam path and local AppID.
- `steamExecutable`: absolute path ending in `steam.exe`. Steam must already be running; this feature does not launch it at login. **Run Steam as administrator for this LoL mapping setup.** The rumble app itself does not need elevation.
- `steamInputAppId`: local non-Steam shortcut's unsigned 32-bit AppID, greater than zero. Keep the Steam shortcut and its saved layout. A game can be launched through WeGame; the shortcut is still the layout identity.
- `gameProcessName`: default `League of Legends`, without `.exe` or a path. Match the actual game window, rather than the launcher, client or a merely running background match.
- `pollMilliseconds`: default `200`, valid `100..5000`. Steam receives a selection request on a transition or after restart, rather than every poll. Failed requests retry no faster than every two seconds.
- `clearTriggerResistance`: boolean, default `false`. If enabled, explicitly set both USB DualSense triggers to Off while the match is foreground. DS4 has no adaptive trigger output; no trigger report is sent to it.
- `triggerRefreshMilliseconds`: default `0`, meaning clear once on entering LoL and do not refresh. Optional `500..60000` values enable periodic fallback; prefer DSX game profiles and keep this zero. Failed writes retry no faster than every two seconds. Switching away, pausing this integration, locking, suspending or exiting stops trigger writes.

The tray's **LoL foreground mapping / free triggers** toggle controls this integration for the current run. The separate **Pause all feedback** toggle controls match vibration. A missing/disabled configuration performs neither layout selection nor trigger resets. Malformed configuration disables the optional integration and reports an error without preventing standalone feedback.

The integrated watcher remains alive when Steam closes, waits for it to restart, and reselects the needed layout. Only the rumble app needs a login startup entry if this integration is used. It does not install autostart by itself; remove/stop any older standalone foreground watcher before enabling it.

## Ownership and limits

`steam://forceinputappid/<AppID>` is the [documented Steam interface](https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs); `0` restores normal automatic selection. The app does not rewrite VDF files, desktop bindings or LoL key settings. Controller key conversion remains Steam Input's work; the documented LoL WASD bindings must already be configured.

DualSense adaptive effects are controller state: closing the application that wrote an effect may leave it active. Clearing it changes the current device state, not DSX's saved profiles. Leaving LoL stops writing; it does not reconstruct an arbitrary previous DSX effect. A different game/tool must apply its own profile again. [DSX's official news](https://steamcommunity.com/app/1812620/allnews/) describes game profile assignment and previous-profile restore features; availability/settings depend on the installed version.

This reset sends USB report `0x02`, trigger-only valid flags `0x0c`, and Off `0x05` at the right/left effect offsets. See [DualSense Explorer](https://github.com/nondebug/dualsense/blob/main/dualsense-explorer.html) and [trigger protocol references](https://github.com/iamdevmarcos/dualsense-controller/blob/main/controller/TRIGGER-NOTICES.md). It does not enable ordinary motor, light, player LED, audio or microphone updates. The compatible rumble reports remain separate. It does not save or restore a hardware effect, add LoL adaptive-trigger feedback, support Bluetooth/Edge, or calibrate the controller. Another active trigger writer can overwrite the reset; configure one owner for each game.

## Prefer DSX game profiles for adaptive triggers

DSX 3.1 supports assigning a profile to a game in Installed Games; its release notes also describe restoring the previous profile when a game exits. Apply a neutral profile with both triggers OFF as the baseline, associate custom effects with the actual game executable, and enable **Reset profile to previous** for those games. Previous means the profile active before launching the game, so establish the baseline first.

This is process-lifetime detection, not a verified foreground switch. Launchers and long-lived clients should not stand in for the actual game executable. Keep the LoL tool's Steam foreground selection separate. Ensure a LoL DSX profile does not introduce duplicate key mapping, motor feedback or LED writes that compete with Steam and the independent tools. Profile switching needs DSX running; closing DSX does not reliably neutralize effects already sent to the controller.

Once DSX profile switching is physically verified, set `clearTriggerResistance` to `false` to let DSX own trigger state. Until then, `triggerRefreshMilliseconds: 0` provides a single-entry neutralization fallback; periodic clearing is optional and is not the recommended configuration.

Sources: [DSX developer release notes](https://steamcommunity.com/app/1812620/allnews/), [previous-profile setting discussion](https://steamcommunity.com/app/1812620/discussions/0/591763775031435045/), [foreground limitation reported by a user](https://steamcommunity.com/app/1812620/discussions/0/836123263126001131/).

## Diagnostics

Use `--status-file <path>` for UTF-8 version 6 status. Fields include `steamInputEnabled`, `steamInputSelectedAppId` (`0` means automatic), `lolWindowForeground`, `triggerResetEnabled`, `triggerResetsWritten` (completed clear reports), and `integrationError`. Optional input enable/error fields are separate; see [Space remapping](input-remap.md). `motorReportsWritten` counts only ordinary vibration reports including stops. `integration.log` records transitions/errors; it rotates at 512 KiB to one older file. No player identity or HID serial/path is recorded.

For an explicitly requested one-time reset, `--reset-triggers <report.json>` clears both USB DualSense triggers without starting the tray app. It changes the device immediately; it does not need the game to be foreground. Build/offline tests do not send this command.

Verify mappings and rumble in a practice match, then switch to another window for at least one status interval. `lolWindowForeground` should become false, selection should be `0`, and the clear-report counter should stop increasing. Device/API checks alone do not prove a Steam layout is correctly configured.
