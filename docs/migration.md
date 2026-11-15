# Name and version migration

[简体中文](migration.zh-CN.md)

The project is now **PlayStation Controller Tools**, hosted at <https://github.com/gou7ma7/playstation-controller-tools>. The independent `mapping/`, `rumble/` and `sleep-light/` folders keep their paths. General PS4/PS5 lighting has no LoL dependency.

Update an existing checkout's remote:

```powershell
git remote set-url origin https://github.com/gou7ma7/playstation-controller-tools.git
git pull --ff-only
```

GitHub redirects the former `lol-ds4-steam-input` URL. Historical v0.1.0 attachments retain their original names and source, including the older DS4-only sleep utility. Current source and v0.2.0 use the new names.

## ControllerLight

- Main executable/namespace: `ControllerLight.exe` / `ControllerLight`.
- Compile-time configuration: `sleep-light/config/LightConfig.cs`, class `LightConfig`. Transfer old custom values before rebuilding.
- The build also copies the same binary to `DS4SleepLight.exe`. This alias is not a launcher or second service. Both names share the legacy singleton and stop-event identifiers, and can stop pre-rename instances.
- Exit the old watcher before replacing it, using its tray menu or `DS4SleepLight.exe --stop`. Review and build locally, copy both executable names and reports to your chosen folder, then start `ControllerLight.exe`.
- Run `sleep-light/scripts/autostart.ps1 -Action Enable -Executable <path-to-ControllerLight.exe>` to save `ControllerLight.lnk` before removing `DS4SleepLight.lnk`. Disable removes both names. Other manual shortcuts can be updated or keep targeting the compatibility alias.
- Primary logs remain beside the executable at `logs/events.log`. New fallback logs use `%LOCALAPPDATA%\ControllerLight\events.log`; previous `%LOCALAPPDATA%\DS4SleepLight` logs are preserved.
- Build manifests identify both executable names/hashes. CI uses `ControllerLight-<language>-windows-x64` artifacts containing both names. CI still does not install, start or publish the applications.

## Module-specific device support

ControllerLight supports USB DS4 v2 and DualSense. LoL rumble remains USB DS4 v2 only. The native mapping VDF remains a DS4 export; DualSense users can manually recreate the core controls. Renaming adds no Bluetooth, DualSense Edge, native DualSense game-feedback or verified DualSense VDF support.

Review the source and build yourself before running a binary. Windows still cannot run periodic light-off refreshes while asleep.
