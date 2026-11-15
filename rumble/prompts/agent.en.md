# Copyable rumble prompt

Give the following to an AI with repository access and replace the final line.

```text
Help me use or customize rumble/. Read root/module AGENTS.md, rumble/README.md, docs/configuration.md, config/FeedbackConfig.cs, src/LoLDamageRumble.cs, src/LoLIntegration.cs, src/LoLInputRemap.cs, docs/integration.md, docs/input-remap.md and tests/LoLRumbleTests.cs first.

This is a Windows x64 USB DS4 v2 / DualSense compatible two-channel LoL feedback app using the local Live Client API, independent of Steam layouts. default preserves the original user's setup. Prefer editing documented thresholds, switches, intensity, waveforms and priorities in the config, then rebuild. Keep native Steam layouts in mapping/. Optional foreground integration shares the tray without becoming a feedback dependency.

Explain the relevant fields, units, ranges, trigger/rearm behavior and preemption before editing. Preserve deduplication, reconnect baselines, stale-effect expiry and motor cancellation on pause/exit. Test pure behavior with synthetic data and simulated output. Verify USB gamepad descriptors and model-specific start/stop reports; ordinary motor reports must never enable LED, trigger, audio or microphone update flags. Optional foreground integration may send separate USB DualSense trigger-only Off reports; preserve release outside LoL/lock/suspend/exit, and do not pretend to restore arbitrary previous effects. Do not claim Bluetooth, DualSense Edge, audio-haptic or gameplay adaptive-trigger feedback support.

Optional Space-to-right-Ctrl uses an isolated input worker of the same exe, with no keyboard hook in the ordinary USB/tray process. Preserve user-operated inputs, physical Ctrl ownership and release on leaving scope. No chat detection. Do not elevate PowerToys or merge input hooks into HID handling. Worker task registration is explicit and separate from offline builds/tests.

After reviewing the source/scripts, run scripts/build.ps1 locally with the chosen -Preset and -Language. Report test results and output paths. Keep English/Chinese documentation aligned. Do not recommend running an unknown downloaded executable or claim unsupported CC/visibility/ammo inputs exist.

My request: <describe the desired feedback>
```
