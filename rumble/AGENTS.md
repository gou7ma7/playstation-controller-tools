# Rumble agent context / 振动反馈 Agent 说明

- Scope: this entire `rumble/` subtree; it must build without `mapping/` or Steam.
- Read README, `docs/configuration.md`, `docs/integration.md`, `config/FeedbackConfig.cs`, both runtime source files, then tests.
- Customization entry: `config/FeedbackConfig.cs`. `default` preserves the user's current timings/thresholds/intensity.
- `gentle`, `focus`, `celebration` are independent copies derived from default; no shared mutable configuration.
- Build: `powershell.exe -NoProfile -File ./scripts/build.ps1 -Preset default -Language auto` from this module.
- The build validates all presets and runs simulated behavior tests before compiling the application.
- Pure detection, pattern generation, scheduling, foreground policy and USB report construction must be testable without HTTP/HID operations.
- Supported devices: Windows x64, USB DS4 v2 054C:09CC and DualSense 054C:0CE6. Verify gamepad usage and USB report lengths before choosing a protocol. Require exactly one supported device. No Bluetooth, DS4 v1 or DualSense Edge support. DualSense uses compatible two-channel rumble; no audio haptics or gameplay adaptive-trigger effects. The optional LoL integration may send trigger-only Off reports to DualSense.
- Read only the fixed loopback Live Client API; do not read game memory or send gameplay inputs.
- Net health loss is inferred; no reliable CC/visibility/cooldown/ammo input exists in this implementation.
- Ordinary motor reports must enable only compatible vibration fields; do not enable light/player LED, adaptive-trigger, audio or microphone updates. Keep optional trigger resets in separate trigger-only reports; foreground gating must release Steam selection and stop trigger writes outside LoL, on pause/lock/suspend/exit. Do not fabricate restoration of arbitrary previous effects. Preserve stop/cancel behavior on pause, lock, suspend, disconnect and exit; never replay stale queued pulses.
- The optional runtime `lol-integration.json` is local/private; the checked-in example is disabled with no real AppID/path. An absent/malformed file must leave independent feedback usable. UI has Chinese/English build choices. Pair docs and explain units/ranges when adding config fields.
- Review source and build locally; do not instruct users to trust an arbitrary downloaded executable.
- Prompts: `prompts/agent.zh-CN.md` and `prompts/agent.en.md`.
