# LoL 左手手柄 + 右手鼠标按键映射

简体中文 · [English](README.md)

本模块可独立使用，包含 Steam 原生布局和把手柄输出转换成英雄技能的游戏 WASD 设置，不需要振动程序。

VDF 是忠实保留的 **DS4 原生导出**。DualSense 可按文档，在 Steam Input 中手动重建左摇杆、L1、L2、L3 的相同键盘输出。原生 DualSense 导出或跨型号导入尚未验证，因此保留 DS4 文件名与控制器类型。

> [!IMPORTANT]
> **必做：以管理员身份启动 Steam，再使用这套 LoL 映射。** 先完全退出原有 Steam 进程，再右键 Steam → **以管理员身份运行**，确认 Windows 提示。普通权限的 Steam 可能在桌面上正常应用布局，却没有权限控制英雄联盟。详见 [启动与排查步骤](docs/setup.zh-CN.md)。此要求仅针对 Steam 映射，独立反馈工具不需要管理员权限。

## 核心操作

- 左摇杆：WASD 移动。
- L2：Q 技能，Steam 实际输出 E。
- L1：W 技能，Steam 实际输出左 Shift。
- 鼠标右键：E 技能，在游戏中直接设置。
- L3：R 技能，Steam 实际输出 R。

手柄只使用左半边，右手用真实鼠标瞄准、交互和执行额外操作。详见 [完整设置](docs/setup.zh-CN.md)。

## 手动应用

希望只在 LoL 前台自动启用时，把布局保存在本机非 Steam 条目中，使用[可选前台整合](../../rumble/docs/integration.zh-CN.md)。游戏仍可从原启动器启动；Steam 负责按键转换，可选程序只选中或释放保存的布局。保留条目作为布局身份，配置自己的 AppID，并停止旧的选布局脚本，避免把 LoL 键位应用到整个桌面。

以下桌面布局方式是最初的手动方案。桌面布局全局生效，可能干扰其他游戏；尝试前先保存原布局。

1. 完全退出 Steam，再右键选择 **以管理员身份运行**。
2. 连接手柄，开启 Steam Input 支持。
3. DS4 可预览并应用 `steam://controllerconfig/413080/3827898683`（「英雄联盟 LOL WASD和鼠标」）；DualSense 请在自己的桌面布局中手动重建核心输出。
4. 在游戏 WASD 模式中设置 Q→E、W→左 Shift、E→鼠标右键、R→R。
5. 先在训练模式验证移动和技能，再进入正常游戏。

413080 是 Steam 桌面布局标识，不是英雄联盟 AppID。需要时先导出原桌面布局，方便恢复。分享链接已由本机导出和加载记录确认，其他账号访问尚未验证。链接不可用时可按设置指南重建核心操作；下载或双击 VDF 不等于自动导入。

## 英雄和交互备注

Riot 已把 WASD 的选取、交互、普攻和确认施法拆成独立绑定，应先检查游戏原生选项，再考虑系统层转换。可选的 **[空格作为右 Ctrl](../rumble/docs/input-remap.zh-CN.md)** 使用同一个 LoL exe 的隔离输入工作进程，仅实际对局前台生效，兼容 Ctrl＋左键交互和 Ctrl＋技能键，不修改 Steam 布局。[交互调研](docs/interaction-research.zh-CN.md) 说明原生绑定限制与实际验证状态。

持续施放 R 时，可用方向键下，避免按 L3 时推动摇杆中断大招。原布局这里输出 **3**，需把对应英雄的 R 设到 3。提莫、卡牌等被动 E 英雄，或高频使用 Q/W 的英雄，可以单独调整鼠标右键。冰霜传送门及选择落点等交互，在使用者的这套设置中需要 **Ctrl + 左键**。这些是手动使用备注，见 [详细说明](docs/usage-notes.zh-CN.md)。

## 文件与 AI 入口

- [原生 VDF](steam-input/lol-left-hand-ds4.vdf)、[格式和来源](steam-input/README.zh-CN.md)、[元数据](steam-input/metadata.json)。
- [游戏键位参考 JSON](game-settings/wasd-keybinds.reference.json)：仅供参考，不可直接导入 Riot 配置。
- [AGENTS.md](AGENTS.md)、[中文 Prompt](prompts/agent.zh-CN.md) / [English Prompt](prompts/agent.en.md)。
- [MIT 许可证](LICENSE)。
- [相似映射方案](docs/related-work.zh-CN.md)。

布局保留旧绑定和未启用动作组，便于忠实备份；功能数据与导出文件一致，跨电脑导入尚未测试。
