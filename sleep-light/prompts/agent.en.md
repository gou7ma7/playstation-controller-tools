# Copyable sleep light prompt

Give the following to an AI with access to this repository; replace the final line.

```text
Help me use or customize sleep-light/. First read root/module AGENTS.md, sleep-light/README.md, docs/configuration.md, config/SleepLightConfig.cs, all src/ files and tests/SleepLightTests.cs.

This is an independent Windows x64 USB DS4 v2 sleep/idle light utility. Default: shared HID input observes buttons, d-pad, sticks and triggers; three minutes of inactivity turns off, and actual controller input restores configured blue. Ignore neutral reports, counters, sensor/battery changes and touch motion; filter drift with configurable thresholds. Idle mode reasserts off while awake, never periodically reasserts color during active use, and never restores solely on resume/unlock. EnableIdleLight=false retains sleep-only restore on user resume/unlock. Windows cannot run refreshes during sleep, and Steam's last color cannot be read reliably. Preserve module independence; never enable motor fields or inject/remap gameplay input. Keep pending IO buffers alive through cancellation.

Prefer annotated compile-time configuration, validate ranges, build from source using scripts/build.ps1, and keep bilingual docs aligned. Offline tests must not send USB commands or suspend my computer. Clearly distinguish successful write completion from physically verified behavior. Explain optional current-user autostart and its removal. Do not run internet-downloaded binaries or install a driver just for lighting.

My specific request: <desired behavior or configuration>
```
