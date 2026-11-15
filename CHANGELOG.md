# Changelog / 更新记录

## v0.2.0 — PlayStation Controller Tools

- Rename the repository from `lol-ds4-steam-input` to `playstation-controller-tools`; preserve the three independent modules and source-first build guidance.
- Add USB DualSense light reports and input parsing alongside DS4 v2. Default three-minute idle off and controller-input blue restore are physically confirmed with Steam running. Motion, counters and battery updates do not reset idle time.
- Rename the lighting namespace/executable to `ControllerLight`; move compile-time settings to `config/LightConfig.cs`. Provide an identical `DS4SleepLight.exe` alias and migrate the login shortcut, preserving the singleton and stop mechanism.
- Align bilingual READMEs, Agent guidance, CI artifact names, product metadata and migration notes. Keep the DS4-native VDF and DS4-only rumble implementation accurately labeled.
- Preserve detailed local diagnostics. Full S4/automatic S3-to-S4 light persistence remains unverified; no periodic command can execute while Windows sleeps.

中文：仓库和灭灯工具统一名称，加入已验证的 USB PS5 空闲灭灯与操作恢复；提供旧程序名兼容入口和自启动迁移，同步中英文文档、Agent 说明与 CI。振动仍仅支持 USB DS4 第二代，原生映射仍是 DS4 导出。完整休眠期间的灯光可靠性仍需另行验证。

## v0.1.0 — Initial independent modules / 初始独立模块

Native DS4 Steam Input mapping, configurable LoL DS4 rumble presets and the original DS4 sleep light utility. Historical release attachments retain their original names and code.

原生 DS4 Steam 映射、可配置的 LoL 振动预设及最初的 DS4 睡眠灭灯工具；历史附件保留原名和原代码。
