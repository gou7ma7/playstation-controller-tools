# PlayStation Controller Tools — Windows PS4 / PS5 手柄工具

简体中文 · [English](README.md)

> **强烈建议先检查源码，再自行编译程序。不要直接运行网上下载的 `.exe`。** 请优先使用下面的本地构建方式；预编译文件只能作为参考，不能代替源码检查。

> [!IMPORTANT]
> **使用这套 Steam Input 映射，必须以管理员身份启动 Steam。** 先完全退出 Steam（包括托盘进程），再右键 Steam → **以管理员身份运行**，确认 Windows 提示。否则可能出现布局已加载，但进入英雄联盟后无法移动或施放技能的情况。详见 [必做启动步骤](mapping/docs/setup.zh-CN.md)。独立的振动和灭灯工具不需要管理员权限。

仓库包含三个独立模块：

- **[mapping/ 按键映射](mapping/README.zh-CN.md)**：LoL 左手手柄、右手鼠标方案，包含 DS4 原生 Steam Input VDF、游戏 WASD 设置和英雄备注；DualSense 可按说明手动重建。无需振动程序。
- **[rumble/ LoL 工具](rumble/README.zh-CN.md)**：读取本地比赛接口，为 USB DS4 第二代及 DualSense 提供普通双通道振动。可选的 [前台整合](rumble/docs/integration.zh-CN.md) 和 [空格作为右 Ctrl](rumble/docs/input-remap.zh-CN.md) 共用一个 LoL exe、托盘和登录入口；键盘工作进程与普通 USB 进程隔离。振动本身仍不依赖这套 Steam 布局或 Steam。
- **[sleep-light/ 睡眠与空闲灭灯](sleep-light/README.zh-CN.md)**：USB DS4 第二代或 DualSense 三分钟不操作就灭灯，实际操作手柄恢复配置蓝色；睡眠/休眠前也灭灯，默认空闲模式恢复/解锁本身不点亮。不依赖 Steam 或游戏。

可以任选一个，也可以组合使用，模块不会安装或修改对方。

## 手柄支持范围

- **按键映射**：原生导出来自 DS4。DualSense 可在 Steam Input 中手动重建文档中的左手键位；原生 DualSense 导出/导入尚未验证。
- **LoL 振动反馈**：支持 USB DS4 第二代和 DualSense 的普通双通道振动；未实现音频触觉或游戏触发的自适应扳机反馈，可选整合能够清除遗留阻力。
- **ControllerLight 灭灯**：支持 USB DS4 第二代和 DualSense，两者在 Steam 运行时的三分钟空闲灭灯与操作恢复均已肉眼确认；完整睡眠/休眠可靠性仍需另行观察。

仓库由 `lol-ds4-steam-input` 更名为 `playstation-controller-tools`。GitHub 会重定向旧仓库链接，建议将 Git remote 改为新地址。灭灯工具正式改名 ControllerLight，同时提供内容一致的 `DS4SleepLight.exe` 兼容入口，保留已有快捷方式的可用性。见 [迁移说明](docs/migration.zh-CN.md)。

## 先检查，再编译

建议使用仓库自带脚本，在检查源码后本地编译。GitHub Actions 也会在 Windows 上运行相同的编译和离线检查：振动工具的四种预设各编译中英文，灭灯工具编译中英文，共 10 个构建版本。

成功运行后，可在 **Actions → Windows build and tests → 对应运行记录 → Artifacts** 找到程序、测试报告和构建清单，保留 7 天。这个附件设置不会改变仓库对运行记录、检查结果和日志的保留策略。这些仅供构建参考，仍建议检查源码后自行编译。工作流不会发布 Releases、安装或启动工具，也不会访问手柄。

正式版本下载见 **[Releases](../../releases)**，包含双语版本说明、源码、预编译参考文件、测试报告和 SHA-256 校验值。Release 附件是独立副本，不会随 Actions 临时附件到期而删除。发布版本是在构建验证后单独执行的步骤，普通推送只运行 CI。

请阅读 [运行源码](rumble/src/LoLDamageRumble.cs)、[详细配置](rumble/config/FeedbackConfig.cs)、[构建脚本](rumble/scripts/build.ps1) 和 [测试](rumble/tests/LoLRumbleTests.cs)。确认后，在 Windows PowerShell 中从仓库根目录运行：

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\build.ps1
```

脚本先验证所有预设并运行模拟行为测试，再生成 `rumble/dist/LoLDamageRumble.exe`。构建过程不会启动用户程序，也不会给手柄发送指令。检查和编译完成后，运行自己编译的文件：

```powershell
.\rumble\dist\LoLDamageRumble.exe
```

振动程序的编译和运行需要 Windows x64 和 .NET Framework 4.x 的 C# 编译器；这个模块不下载依赖，不要求 API Key 或管理员权限。振动实现支持 USB DS4 第二代（`054C:09CC`）和 DualSense（`054C:0CE6`）。

两个程序均包含原创应用和托盘图标：振动工具为紫色手柄与脉冲，灭灯工具为蓝色手柄与月亮。各模块的 `assets/` 保存可编辑 SVG 和多尺寸 ICO；构建时嵌入 ICO，并在 `build-info.json` 中记录图标的 SHA-256。运行 EXE 无需额外图标文件。

## 独立编译睡眠灭灯工具

先阅读 [灭灯源码与配置说明](sleep-light/README.zh-CN.md)，再自行编译并运行：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\build.ps1
.\sleep-light\dist\ControllerLight.exe
```

构建运行离线检查，不发送 USB 指令。[当前用户登录自启动](sleep-light/README.zh-CN.md#可选登录自启动) 是独立、可撤销的步骤。工具仅更新灯光字段，包括可选的 DualSense 白色玩家灯；不改变马达、自适应扳机、麦克风静音状态或按键映射。[详细配置与验证](sleep-light/docs/configuration.zh-CN.md)。

## 定制振动

编辑 **[rumble/config/FeedbackConfig.cs](rumble/config/FeedbackConfig.cs)**，重新编译即可。每个参数都有中英文注释，配置会编入程序，不需要另带运行时配置文件。

- `default`：你的当前方案，65% 强度，大量掉血、低血量、低蓝量/能量、击杀、助攻、连杀全部启用。
- `gentle`：轻柔模式，45% 强度，重击和击杀更轻，关闭助攻提醒。
- `focus`：关键提醒，关闭击杀庆祝，提高大量掉血门槛，降低低血量和低资源提醒门槛。
- `celebration`：庆祝模式，仅击杀、助攻和连杀。

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\build.ps1 -Preset focus -Language zh
```

默认 `-Language auto` 按 Windows 界面语言选择中文或英文；`zh`、`en` 可强制选择。预设在编译时选定，更多说明见 [详细配置指南](rumble/docs/configuration.zh-CN.md)。

## 目录与 Agent 入口

`mapping/` 自带布局、游戏参考设置、双语文档、`AGENTS.md` 和可复制 Prompt；`rumble/` 自带配置、源码、编译脚本、测试、双语文档和自己的 Agent 入口。`sleep-light/` 自带独立灭灯程序、配置、构建/自启动脚本、离线测试和双语 Agent 入口。三个文件夹均可单独使用。

AI 可先读取根目录 [AGENTS.md](AGENTS.md)，再进入对应模块。使用者也可以直接复制模块 `prompts/` 中的中英文任务模板。

[贡献说明](CONTRIBUTING.zh-CN.md) · [MIT 许可证](LICENSE)

## 验证与限制

灯光离线检查覆盖噪声过滤、持续按住、180 秒空闲边界、重试、断线、后台唤醒、锁屏/解锁与取消。Steam 运行时，原使用者已确认三分钟灭灯和操作手柄恢复蓝色，原生日志吻合。仅睡眠版本曾通过 S3 测试，但后来又出现写入成功却常亮的反馈，S4 和自动 S3 转 S4 的可靠性仍未确认。

测试覆盖当前默认方案的阈值和节奏、预设差异、配置范围、事件去重和调度。使用模拟比赛数据与模拟马达输出，不直接振动。真实对局的受击反馈已由原使用者确认；新预设的手感需要自行测试。布局功能数据与导出文件一致，跨电脑导入及其他 Steam 账号访问尚未验证。

振动模块使用的是净掉血差分，没有被控制、小地图敌人出现、冷却或男枪弹药检测。只读取固定本地接口和发送马达指令，不读取游戏进程内存、不发送游戏操作。具体限制见模块文档。

本项目未经 Riot Games 背书，不代表 Riot Games 的观点；英雄联盟及相关商标属于其权利人。
