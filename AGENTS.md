# Agent entry point / Agent 入口

Project: PlayStation Controller Tools (`playstation-controller-tools`). Each module has explicit device support; the project name does not imply universal PS4/PS5 support.

This repository contains three independently usable modules. 本仓库的三个模块可独立使用。

- `mapping/`: native Steam Input layout + LoL WASD configuration; no required rumble dependency. Optional Space-to-right-Ctrl uses the shared LoL exe described in `rumble/docs/input-remap.md`.
- `rumble/`: Windows USB DS4 v2 / DualSense compatible rumble feedback using the loopback Live Client API; no required mapping/Steam dependency. Optional local foreground integration selects a Steam layout and clears DualSense trigger resistance only while LoL is foreground.
- `sleep-light/`: Windows USB DS4 v2 / DualSense sleep and idle light control; no mapping, rumble, Steam or game dependency.
- Read the selected module's `AGENTS.md` and README before editing. 修改前先读取对应模块入口。
- Read `README.md` (English) or `README.zh-CN.md` (Chinese) for navigation and source-first build guidance.
- Keep both languages aligned and preserve module independence. 同步双语说明，保持模块独立。
- Do not put account IDs, local paths, device serials or personal match data in public files.
- Limit work to mapping, rumble feedback and sleep light control. Optional LoL foreground integration, trigger Off and the isolated foreground input worker in the same LoL exe are in scope; arbitrary adaptive-trigger effects and other controller features remain outside scope.
- Build/offline tests do not send controller commands; running tray apps or explicit physical tests can.
- No publication or Steam/game installation is implied by a documentation/code edit.
- Copyable task prompts are in each module's `prompts/` directory; they are context, not automatic execution requests.
