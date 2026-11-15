# Mapping agent context / 按键映射 Agent 说明

- Scope: this entire `mapping/` subtree; it must work without `rumble/`.
- Read `README.md`/`README.zh-CN.md`, `docs/setup.md`, `docs/usage-notes.md`, then the VDF and reference JSON.
- Source of truth for physical outputs: `steam-input/lol-left-hand-ds4.vdf` and its active preset/group links.
- The native export is DS4-specific. Document manual recreation of core controls for DualSense; never relabel the VDF or claim a verified DualSense export/import.
- Source of truth for final actions: setup docs and `game-settings/wasd-keybinds.reference.json`.
- Core use: left-hand DS4 + right-hand mouse; L2 outputs E for Q, L1 outputs Left Shift for W, mouse right performs E, L3 outputs R for R.
- Required startup for this setup: completely exit Steam, then run Steam as administrator before applying/using the layout in LoL. Keep this prominent in both languages; do not extend elevation to the independent rumble/sleep-light tools. 本方案使用前须完全退出 Steam，再以管理员身份启动；保留醒目双语提示。
- Do not confuse skill slot Q/W/E/R with the keyboard letter Steam emits.
- D-pad down currently emits 3. Sustained-R use needs a per-champion game R binding to 3; it is a note, not automatic switching.
- Passive E/high-frequency Q/W substitutions are usage notes only. Ctrl+left-click remains available; optional Space-to-right-Ctrl runs in an isolated worker of the shared LoL exe, without changing the Steam VDF.
- Prefer the optional rumble foreground integration when automatic LoL-only activation is requested; the original desktop method is global. It selects a local saved layout, not a public hard-coded AppID. Native WASD interaction research is in docs/interaction-research.md; do not assume Space is a supported chord modifier or add a keyboard hook without an explicit implementation request.
- Parse Valve KeyValues with duplicate keys preserved; a plain JSON dictionary parser can lose repeated groups/bindings.
- Keep inactive groups distinguishable from active bindings. Template labels such as Reload are not LoL actions.
- Do not rewrite Steam/global game settings or expose private metadata as part of repo maintenance.
- For Space remapping read `rumble/docs/input-remap.md` and `rumble/src/LoLInputRemap.cs`. Scope strictly to foreground `League of Legends.exe`; preserve physical right Ctrl ownership and release owned Ctrl on leaving scope. Ignore injected input. The keyboard hook belongs only to the isolated input worker, never to the ordinary HID/tray process. No chat detection; do not elevate PowerToys. Build/test never installs hooks or sends input. The reviewed on-demand worker task is separate from the ordinary login shortcut.
- Keep paired Chinese/English docs aligned and update metadata hash after an intentional VDF change.
- Prompts: `prompts/agent.zh-CN.md` and `prompts/agent.en.md`.
