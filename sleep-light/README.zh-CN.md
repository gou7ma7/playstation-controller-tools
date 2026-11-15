# PS4 / PS5 睡眠与空闲灭灯工具

简体中文 · [English](README.md)

> **先检查源码，再自行编译。不要直接运行网上下载的 `.exe`。**

独立的 Windows 托盘工具，支持 **一只 USB DS4 第二代（`054C:09CC`）或 DualSense（`054C:0CE6`）**：默认**三分钟不操作手柄就灭灯，再次操作手柄恢复配置蓝色**，睡眠/休眠前也发送灭灯指令。不需要 Steam、英雄联盟或仓库的其他模块。程序仍名为 `DS4SleepLight.exe`，已有启动/登录快捷方式可继续使用。

## 实际行为

- 启动、退出程序时不改变灯光。
- 收到 `PBT_APMSUSPEND` 后，按识别到的型号发送 RGB 为零的报告。DS4 使用 32 字节 `0x05` 报告，灯色/闪烁标志为 `0x06`；DualSense 使用 Windows 48 字节 `0x02` 报告，只启用 RGB/玩家灯字段。不操作马达、自适应扳机、音频或麦克风静音状态。USB 仍然供电。
- DualSense 白色玩家指示灯默认随 RGB 熄灭，恢复时亮中央一灯。可将 `ControlDualSensePlayerLeds = false` 留给 Steam 管理，或修改恢复掩码。麦克风静音灯保留，避免掩盖静音状态。
- 后台自动唤醒时保持熄灭，包括可能紧接着从 S3 转入 S4 的定时唤醒。
- **默认空闲模式**：180 秒不操作手柄就灭灯。按键按下/松开、方向键、超过容差的摇杆或扳机算操作；持续按住也算使用。鼠标/键盘活动、静置 USB 上报、包计数、传感器、电量变化和触摸板滑动都不算；触摸板按下算按键。
- 空闲且电脑仍运行时，每五秒重发一次灭灯，应对其他软件改色。实际操作手柄后恢复配置 RGB，**默认蓝色 `0,0,255`**。正常使用时不周期性写灯色，Steam 后续仍可改色。
- 恢复电脑或解锁本身不点亮。重新打开输入并建立新基线；睡眠时已灭灯则再次发送灭灯，等待实际手柄操作。锁屏期间可以灭灯，但手柄操作不恢复颜色。
- 将 `EnableIdleLight = false` 后重新编译，可切回原来的仅睡眠模式：用户恢复使用/解锁后延迟 1.5 秒恢复颜色，USB 不可用时短暂重试。`RestoreOnResume` 仅在该模式生效。
- 无法读取并自动还原 Steam 最后设置的颜色/亮度。Windows 完全睡眠时无法继续重发灭灯；空闲模式可提前灭灯，但无法保证其他软件最后一次写入或 USB/固件状态变化不会重新点亮。

暂不支持 USB DS4 第一代、DualSense Edge、蓝牙连接或多只目标手柄。包括同时连接 DS4 与 DualSense 在内，发现多个匹配设备时不发送。测试前断开额外目标及虚拟副本；通过设备属性、游戏手柄用途和输入/输出长度选择协议，不接受蓝牙报告。

## 检查、编译与运行

请阅读 [运行源码](src/Program.cs)、[电源状态机与 USB 报告](src/SleepPolicy.cs)、[输入解析与空闲状态机](src/ControllerActivity.cs)、[共享输入读取](src/ControllerInput.cs)、[原生 USB 输出](src/NativeHid.cs)、[配置](config/SleepLightConfig.cs)、[构建脚本](scripts/build.ps1) 和 [离线测试](tests/SleepLightTests.cs)。

从仓库根目录，在 Windows PowerShell 中执行：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\build.ps1
.\sleep-light\dist\DS4SleepLight.exe
```

需要 Windows x64 和 .NET Framework 4.x C# 编译器；不下载依赖，不要求管理员权限，不安装额外驱动。构建先运行离线检查，再生成 `dist/DS4SleepLight.exe`、`test-results.json`、`build-info.json`；不会启动工具或发送 USB 指令。

整个 `sleep-light/` 文件夹可以单独复制使用，在该文件夹运行 `scripts/build.ps1` 即可。默认 `-Language auto` 跟随 Windows 界面语言；`zh`、`en` 强制中英文。重新编译正在运行的程序前，先退出该工具。

程序和托盘使用内嵌的原创手柄与月亮图标。本模块包含 [可编辑 SVG 和多尺寸 ICO](assets/README.md)；构建时在 `build-info.json` 中记录图标 SHA-256，运行时无需单独图标文件。

## 可选：登录自启动

检查源码并自行编译后：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Enable
```

只添加一个**当前用户的“启动”文件夹快捷方式**，指向本机编译文件；不会立即启动，不安装服务或驱动。请保留该位置的程序；当前登录会话需要手动运行一次。同一会话只允许一个实例。

撤销：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Disable
.\sleep-light\dist\DS4SleepLight.exe --stop
```

托盘菜单也可退出，并提供不改变灯光的连接检查。若把程序放在其他位置，自启动脚本可用 `-Executable` 指定自己编译的文件。

## 配置与验证

修改 [SleepLightConfig.cs](config/SleepLightConfig.cs)，重新编译即可。详见 [配置与验证指南](docs/configuration.zh-CN.md)。

只读 USB 诊断，从仓库根目录执行：

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--diagnose sleep-light/dist/diagnostic.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\diagnostic.json
```

可选的**实际灯光测试**：熄灭三秒，再发送配置的恢复灯色；不会让电脑睡眠。

先退出正在运行的工具，避免空闲灭灯的重复写入干扰测试；测试后重新启动。

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--test-cycle 3 sleep-light/dist/cycle-result.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\cycle-result.json
```

写入成功不能单独证明肉眼灯光或完整睡眠流程。实际验证需要启动工具，手动选择 Windows“睡眠”或“休眠”，观察灭灯，再恢复/解锁观察灯色。不要用测试脚本自动休眠他人正在使用的电脑。

空闲模式的实际验证：至少三分钟不碰手柄，观察灭灯，再按键或推动摇杆，观察恢复蓝色；Steam 运行时再确认一次。离线检查覆盖噪声过滤、持续按住、180 秒边界、重试、重新连接及睡眠取消，不代表肉眼效果已验证。

USB DS4 第二代的原使用者已在 Steam 运行时确认默认的**三分钟空闲灭灯 → 操作手柄 → 恢复蓝色**，原生日志也记录了两次写入。静置只读测试三秒读到 751 个报告，操作样本为零。这些观察只适用于该 USB 手柄，不代表所有设备。

USB DualSense 已在 Steam 运行时通过静置只读检查：三秒读到 752 个报告，操作样本为零。使用者确认十秒实际灯光测试使 RGB 与白色玩家灯都熄灭，随后恢复配置蓝色与中央白灯。DualSense 完整睡眠/休眠转换仍需另行肉眼测试。

使用者也已确认 DualSense 默认的**三分钟空闲灭灯 → 操作手柄 → 恢复灯色**。日志记录约 180 秒时灭灯写入成功，以及后续实际操作触发蓝色恢复并写入成功；这验证了电脑运行期间的空闲流程，不代表完整睡眠/休眠转换已验证。

原来的仅睡眠版本曾通过用户观察的 S3 测试，但之后又报告睡眠时常亮，尽管睡前写入成功；空闲测试不能证明 S4 或自动 S3 转 S4 可靠，完整睡眠流程仍需单独观察。

## 共存与限制

DS4 上，仓库振动模块仅启用马达（`0x01`），此模块仅启用灯色/闪烁（`0x06`）。DualSense 上仅启用 RGB 和可选的玩家灯，马达、触觉、扳机、音频、静音和灯光动画标志均保持关闭。共享句柄便于共存，但其他灯光写入者仍可能覆盖颜色。振动模块目前仍只支持 DS4，加入 DualSense 灯光支持不会同时增加 PS5 游戏反馈。

其他软件也写灯色、独占 HID、设备在转换时掉线、Windows 电源通知缺失/延迟，都可能影响实际效果。程序不阻止电脑睡眠，不断开 USB，不修改 BIOS/电源设置，不映射按键，不读取游戏数据或游戏内存。实际手柄行为需要自行观察，不能只凭写入成功保证有效。

可选轮换日志优先写入 EXE 旁的 `logs/events.log`；程序目录不可写时，回退到 `%LOCALAPPDATA%\DS4SleepLight\events.log`。托盘的 **打开日志** 可打开当前文件。记录 UTC/本地时间、运行编号与顺序、电源/锁屏状态、RGB 指令与 USB 耗时，以及输入连接、空闲灭灯/操作恢复、输入报告总数、重发总数和较长的界面定时器间隔。成功的重复空闲灭灯最多每分钟汇总一次，失败单独记录；运行状态和设备变更记录本身不操作 USB。日志超过 1 MiB 时轮换到 `events.log.old`。

这些记录用于区分通知未到达/延迟与 USB 写入失败/延迟，不能读取实际灯色、识别其他软件的灯光指令，也不能证明写入成功后灯色没有被覆盖。日志、诊断和测试文件不包含设备路径、序列号、账号或游戏数据。详见 [日志解读](docs/configuration.zh-CN.md#日志诊断)。

[Agent 说明](AGENTS.md) · [Agent Prompt](prompts/agent.zh-CN.md) · [MIT 许可证](LICENSE)
