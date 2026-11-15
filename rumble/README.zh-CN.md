# 英雄联盟 USB PS4 / PS5 振动反馈

简体中文 · [English](README.md)

> **先检查源码，再在本地自行编译。不要直接运行网上下载的 `.exe`。** 请优先采用以下构建方式。

可独立使用的 Windows x64 托盘程序，读取英雄联盟本地比赛接口，为 USB DS4 第二代或 DualSense 提供双通道振动。DualSense 使用普通振动兼容模式，不提供音频触觉或游戏触发的自适应扳机效果。不需要 Steam、特定按键布局或 WASD 模式；不使用第三方库或 API Key。

## 检查和编译

阅读 [运行源码](src/LoLDamageRumble.cs)、[可选整合](src/LoLIntegration.cs)、[配置](config/FeedbackConfig.cs)、[构建脚本](scripts/build.ps1) 和 [测试](tests/LoLRumbleTests.cs)。从本模块目录运行：

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1
```

需要 Windows .NET Framework 4.x x64 C# 编译器。脚本验证所有预设并运行模拟测试，再生成 `dist/LoLDamageRumble.exe`；不下载依赖、不向手柄发送指令。检查后运行自己编译的程序：

```powershell
.\dist\LoLDamageRumble.exe
```

本文件夹可以单独复制和构建，`dist/` 生成文件不提交到源码仓库。

程序和托盘使用内嵌的原创手柄与脉冲图标。本模块包含 [可编辑 SVG 和多尺寸 ICO](assets/README.md)；构建时在 `build-info.json` 中记录图标 SHA-256，运行时无需单独图标文件。

## 预设与定制

**[config/FeedbackConfig.cs](config/FeedbackConfig.cs)** 是编译时配置，带详细中英文注释。正常调节只改配置再编译，不需要修改业务逻辑。

- `default`：你的当前方案，65% 强度，六类反馈全开。
- `gentle`：45% 强度，重击/击杀更轻，关闭助攻。
- `focus`：55% 强度，仅关键提醒；大量掉血 15%、低血量 20%、低资源 15%。
- `celebration`：65% 强度，仅击杀、助攻、连杀。

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Preset gentle -Language zh
```

界面语言可选 `auto`、`zh`、`en`。托盘显示编译预设、阈值和启用状态；托盘临时修改只对本次运行有效。见 [详细配置](docs/configuration.zh-CN.md) 和 [反馈行为](docs/feedback.zh-CN.md)。

## 运行与排错

支持 USB DS4 第二代 `054C:09CC` 和 DualSense `054C:0CE6`，未实现蓝牙、DS4 第一代、DualSense Edge 或其他手柄。使用 Windows HID 驱动，需恰好连接一个受支持的游戏手柄。选择振动协议前会验证厂商/产品、游戏手柄 HID 用途及 USB 输入/输出长度。振动报告不启用灯条、玩家灯、自适应扳机、音频或麦克风的更新标志。

没有对局时静默等待。托盘可以开关反馈、调整强度、测试节奏、暂停和退出。托盘手动测试会真的振动，即使该反馈的自动触发已关闭。其他同时写马达的软件可能覆盖输出。

暂停、锁屏、休眠、失联和退出会取消队列并尝试停转，恢复后重新建立基准。不安装开机启动；托盘反馈偏好仅当次有效，可选整合另外使用本机 JSON 配置。通过托盘或 `--stop` 正常结束：

```powershell
.\dist\LoLDamageRumble.exe --stop
```

可选只读诊断：

```powershell
Start-Process -FilePath .\dist\LoLDamageRumble.exe -ArgumentList '--diagnose', 'diagnostics.json' -Wait -WindowStyle Hidden
Get-Content .\diagnostics.json
```

诊断检查接口和手柄输出句柄，不发送振动。`--status-file <路径>` 定期写版本 6 状态快照，含接口可用性、手柄型号/连接/错误、支持设备数量、反馈计数和成功马达报告写入次数。`pulsesWritten` 保留旧版含义，统计开始播放的节奏；`motorReportsWritten` 统计完成的 USB 写入，含停转。报告不含 HID 路径、手柄序列号或玩家身份；可选字段另记录前台/布局、扳机清除计数和输入模块开关/错误；`--test <路径>` 会真的短震 150 毫秒并停转。日常模式测试建议用托盘。

## 与 LoL 映射共用托盘（可选）

按 [前台整合说明](docs/integration.zh-CN.md) 配置，可在实际对局前台时选中本机已保存的 Steam 布局，并清除 USB DualSense 遗留扳机阻力。未配置时默认关闭，振动仍独立于 Steam。使用文档中的 LoL 映射需以管理员身份运行 Steam。切走后恢复自动选布局并停止扳机写入，其他游戏/工具需重新应用自己的效果。整合源码是 [LoLIntegration.cs](src/LoLIntegration.cs)。

## 同一个 exe 的空格修饰键（可选）

[空格作为右 Ctrl](docs/input-remap.zh-CN.md) 仅在实际 LoL 前台让使用者自己的点击与技能键继承 Ctrl 行为。普通 USB/托盘进程没有键盘钩子；隔离工作进程使用同一个 exe，并在离开前台时释放自己按住的 Ctrl。默认关闭，使用独立本机配置和经检查的按需提权任务，不再需要独立点击程序或登录入口。

## Agent 与参考

[AGENTS.md](AGENTS.md) · [中文 Prompt](prompts/agent.zh-CN.md) · [English Prompt](prompts/agent.en.md) · [MIT 许可证](LICENSE)

[相似反馈项目](docs/related-work.zh-CN.md)。DualSense 兼容振动字段参考 [Linux PlayStation HID 驱动](https://kernel.googlesource.com/pub/scm/linux/kernel/git/stable/linux-stable/+/master/drivers/hid/hid-playstation.c)：USB 报告 `0x02`，普通振动兼容 / 触觉模式选择标志以及独立的右/左强度；DS4 保留原有的 USB 报告 `0x05`。

[Riot 本地比赛接口](https://developer.riotgames.com/docs/lol#game-client-api_live-client-data-api)。检测的是净掉血，没有控制、视野、技能冷却或弹药反馈；添加新触发前先检查数据能力。
