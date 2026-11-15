# Optional LoL Space as right Ctrl

[简体中文](input-remap.zh-CN.md)

Uses the same **`LoLDamageRumble.exe`**, LoL tray and existing login entry. Rumble, foreground Steam layout selection and Space remapping remain separate code modules. No separate click program is required. Exit the older independent mouse-only Space+click helper before enabling this replacement.

## Behavior

- Only in the actual foreground **`League of Legends.exe`**, physical Space down/up becomes right Ctrl down/up. Original Space is suppressed.
- Mouse and skill keys remain user-operated and pass normally, enabling Ctrl+left-click interaction and Ctrl+skill-key leveling.
- Space repeats do not repeat Ctrl down. Space release, leaving game foreground, lock/suspend or worker exit releases owned right Ctrl.
- User-held physical right Ctrl is preserved. Left Ctrl, left Alt and PowerToys swaps are outside this mapping.
- Space held outside the game does not become Ctrl on switching into it; release and press again.
- There is no chat/menu detection. Space cannot also type a space or recenter the camera within that foreground process. This matches the current user's choice not to type during play.

No repeated clicks, skill automation, game-memory reads or game injection. Runtime state stores current modifier ownership and aggregate counts, with no typed text or key history.

## Review and enable

Review [LoLInputRemap.cs](../src/LoLInputRemap.cs), [tests](../tests/LoLRumbleTests.cs), and build yourself with the [normal build script](../scripts/build.ps1). Build/tests install no hooks and simulate no input. Remapping defaults off; rumble does not require a keyboard hook.

Copy the [example](../config/input-remap.example.json) beside your own executable as `input-remap.json` and set `enabled` to `true` if desired. This one boolean is separate from `lol-integration.json`. The tray switch temporarily changes runtime state, without rewriting configuration. Edit the file and restart to change the startup default.

## One exe, isolated worker

The ordinary process owns the tray, match data, USB rumble and Steam selection, with **no keyboard hook**. The same exe starts a worker in `--input-worker` mode, installing only a keyboard hook for Space/right Ctrl. It does not access USB, match data or Steam. Input handling must not share a potentially blocked USB message loop.

Windows `SendInput` cannot control a higher-integrity game, so the input worker is elevated when required. **PowerToys, the rumble host and lighting app remain non-elevated.** Without a registered worker task, enabling remapping prompts for that exe's UAC approval.

To let the existing ordinary login entry launch this feature, review the [task script](../scripts/input-worker-task.ps1), then register its on-demand task once from an administrator PowerShell:

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\input-worker-task.ps1 -Action Enable -Executable "C:\YourTools\LoLDamageRumble.exe"
```

`PlayStationControllerTools-LoLInput` runs the current user's same exe with only `--input-worker`, without a password or separate login trigger. The enabled host requests the task; the ordinary LoL login entry stays unchanged. Do not force-stop a running modifier owner in Task Scheduler.

Turning remapping off or exiting the host stops its heartbeat; the worker releases owned Ctrl and exits. Host crash/stall also expires the heartbeat. `input-host.json` is the host heartbeat and `input-status.json` is worker state; do not publish personal runtime data. The executable directory must be writable by the current user.

To remove the task, switch remapping off and wait for the worker to exit, then run the script for the same path with `-Action Disable`. Do not leave a stale task referencing a deleted or renamed executable.

## Verification

158 offline checks cover existing rumble/foreground behavior plus modifier ownership, both release orders, focus exit, repeats and right-Ctrl input format. **The user confirmed Space+click, Space+skill-key leveling and normal input after release in practice mode.** Runtime counts confirmed paired right-Ctrl down/up with no injection errors. Other PCs and game versions still need verification.

Sources: [SendInput integrity limits](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput), [LowLevelKeyboardProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc). [Native LoL binding research](../../mapping/docs/interaction-research.md).
