# Sleep and idle light agent context / 睡眠与空闲灭灯 Agent 说明

- Scope: all of `sleep-light/`; build and run independently of mapping, rumble, Steam and LoL.
- Read README, `docs/configuration.md`, configuration, all `src/` files and offline tests.
- Windows x64, one USB DS4 v2 054C:09CC, gamepad usage, 64-byte USB input and 32-byte output only. Refuse ambiguous multiple targets.
- Preserve report 0x05 / flags 0x06: RGB and blink only; do not enable motor, audio or unrelated fields.
- Starting/exiting must not write lights. Shared HID input may be observed only for controller inactivity; never inject/remap gameplay input or access game APIs/memory, power settings or BIOS.
- Background PBT_APMRESUMEAUTOMATIC must not restore RGB; S3 can automatically wake only to enter S4.
- Default idle mode: three minutes without buttons/sticks/triggers turns lights off; actual controller activity restores configured RGB. Neutral reports, packet counters, gyro/accelerometer, battery and touch motion are not activity. Held controls count; stick deadzone and trigger threshold filter noise.
- Idle mode never restores RGB merely on resume/unlock. Discard stale queued input across suspend/reconnect/long UI gaps. Reassert off periodically only while idle and the OS is running. Locked sessions may turn off but may not restore color.
- With EnableIdleLight=false, retain the original interactive resume/unlock restoration and pending-restore cancellation on suspend/lock.
- Config: `config/SleepLightConfig.cs`; compile-time RGB default blue. Do not claim restoration of Steam's last color.
- Build `scripts/build.ps1`; offline checks must not invoke HID or suspend the PC.
- Native write completion is not proof of visible light or actual sleep behavior; describe verification accurately.
- Diagnostic logs are best-effort and non-identifying. Heartbeat/device-change records must not perform USB IO or write lights. Log idle transitions and aggregate input/refresh counts, never raw buttons, coordinates or report data. 日志仅记录状态与汇总，不记录原始按键或数据包。
- Keep pinned data and unmanaged OVERLAPPED alive until pending/canceled IO completes.
- Autostart is an optional current-user shortcut. Never silently install drivers or services for this module.
- No device paths/serials, accounts, personal match data or local absolute paths in public files.
- Keep Chinese and English docs/prompts aligned. Source-first build guidance must stay prominent.
- Copyable prompts: `prompts/agent.en.md`, `prompts/agent.zh-CN.md`.
