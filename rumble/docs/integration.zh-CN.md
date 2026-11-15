# 可选 LoL 前台整合

[English](integration.md)

振动程序可以同时在实际 LoL 对局窗口位于前台时选中已保存的 Steam Input 布局，并清除 USB DualSense 遗留的扳机阻力。此功能可选：程序旁没有 `lol-integration.json` 时，振动仍独立于 Steam 和按键映射模块。反馈阈值和节奏仍使用编译时的 `FeedbackConfig.cs`。

## 编译后配置

检查 [LoLIntegration.cs](../src/LoLIntegration.cs)，把 [默认关闭的示例](../config/lol-integration.example.json) 复制到自行编译的程序旁，命名为 `lol-integration.json`。修改这份本机私有文件后重启程序。不要照搬其他电脑的非 Steam AppID。

- `enabled`：布尔值，默认 `false`；填好实际 Steam 路径及本机 AppID 后才开启。
- `steamExecutable`：以 `steam.exe` 结尾的绝对路径。Steam 需已在运行，本功能不会登录后主动启动它。**这套 LoL 映射要求以管理员身份运行 Steam**，振动程序本身不需提权。
- `steamInputAppId`：本机非 Steam 条目的 32 位无符号 AppID，须大于零。保留对应条目和已保存的布局。游戏可以从 WeGame 启动，Steam 条目仍作为布局身份。
- `gameProcessName`：默认 `League of Legends`，不带 `.exe` 或路径。匹配实际对局窗口，而非启动器、客户端或仅在后台运行的对局。
- `pollMilliseconds`：默认 `200`，范围 `100..5000`。切换前台或 Steam 重启后才请求选布局，不在每轮检测时重复调用；失败重试至少间隔两秒。
- `clearTriggerResistance`：布尔值，默认 `false`。开启后，对局前台时明确将 USB DualSense 左右扳机设为 Off。DS4 没有自适应扳机，不向它发送扳机报告。
- `triggerRefreshMilliseconds`：默认 `0`，只在进入 LoL 前台时清除一次，不持续刷新。可选 `500..60000` 才启用周期补救；优先使用 DSX 游戏配置，并保持这里为零。失败写入的重试至少间隔两秒。切走、暂停此整合、锁屏、睡眠或退出后停止扳机写入。

托盘 **LoL 前台映射 / 无阻力扳机** 开关控制本次运行中的整合功能；独立的 **暂停全部反馈** 只控制比赛振动。配置缺失或关闭时既不选布局，也不清除扳机。配置格式错误会关闭可选整合并记录错误，不影响独立振动。

Steam 退出时，整合检测继续等待；Steam 再启动后重新选择所需布局。采用整合功能后，只需为振动程序设置一份登录自启动；它本身不会安装自启动。开启整合前停止并移除旧版独立前台检测脚本的启动入口。

## 控制权与限制

`steam://forceinputappid/<AppID>` 是 [Steam 文档中的接口](https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs)，`0` 恢复自动选择。程序不改写 VDF、桌面布局或 LoL 键位；手柄按键转换仍由 Steam Input 完成，游戏里的 WASD 绑定需事先配置。

DualSense 自适应效果属于手柄的当前状态，关闭发送效果的软件后可能仍保留。清除只改变当前硬件状态，不修改 DSX 已保存的配置。离开 LoL 后停止发送，不会重建任意旧 DSX 效果，其他游戏或工具需重新应用自己的配置。[DSX 官方更新说明](https://steamcommunity.com/app/1812620/allnews/) 提供了游戏配置关联及恢复先前配置等功能；具体选项取决于安装版本。

清除使用 USB 报告 `0x02`，只启用扳机标志 `0x0c`，在右/左效果位置写 Off `0x05`。参考 [DualSense Explorer](https://github.com/nondebug/dualsense/blob/main/dualsense-explorer.html) 和 [扳机协议资料](https://github.com/iamdevmarcos/dualsense-controller/blob/main/controller/TRIGGER-NOTICES.md)。不启用普通振动、灯条、玩家灯、音频或麦克风更新；兼容振动仍用独立报告。此功能不保存/恢复硬件效果、不新增 LoL 自适应扳机反馈、不支持蓝牙/Edge，也不校准手柄。其他活跃扳机工具可能覆盖效果，建议每个游戏明确由一个工具控制。

## 优先用 DSX 的游戏配置管理扳机

DSX 3.1 支持在 Installed Games 中为游戏关联配置，官方更新也说明了游戏退出后恢复上一配置的机制。先应用双扳机 OFF 的中性配置作为基线，再给实际游戏 EXE 关联各自的效果，并启用 **Reset profile to previous**。这里恢复的是启动游戏前的配置，不保证固定回到默认，所以需要先建立基线。

它按进程启动、退出检测，尚不能当作已验证的前台切换；不要用长期运行的启动器或客户端代替实际游戏进程。LoL 工具的 Steam 前台选布局仍单独保留。LoL 的 DSX 配置需要避免额外键位映射、马达反馈或灯光写入与 Steam、自制工具重复。配置自动切换需要 DSX 正在运行；关闭 DSX 不保证清除已经发送到手柄的效果。

实际验证 DSX 配置切换正常后，将 `clearTriggerResistance` 设为 `false`，由 DSX 管理扳机。验证前可用 `triggerRefreshMilliseconds: 0`，只在进入 LoL 时清除一次作为备用；持续清理是可选补救，不再作为推荐配置。

依据：[DSX 官方更新](https://steamcommunity.com/app/1812620/allnews/)、[恢复上一配置讨论](https://steamcommunity.com/app/1812620/discussions/0/591763775031435045/)、[网友报告的前台切换限制](https://steamcommunity.com/app/1812620/discussions/0/836123263126001131/)。

## 诊断

`--status-file <路径>` 写 UTF-8 版本 6 状态，含 `steamInputEnabled`、`steamInputSelectedAppId`（`0` 表示自动）、`lolWindowForeground`、`triggerResetEnabled`、`triggerResetsWritten`（完成的清除报告）及 `integrationError`。输入模块开关/错误另记录，见 [空格修饰键](input-remap.zh-CN.md)。`motorReportsWritten` 只统计普通振动报告，含停转。`integration.log` 记录切换与错误，达到 512 KiB 轮换并保留一份旧日志，不记录玩家身份或 HID 序列号/路径。

明确需要单次清除时可用 `--reset-triggers <report.json>`，立即清除 USB DualSense 双扳机而不启动托盘。此命令直接改变硬件，不要求游戏前台；构建及离线测试不会发送它。

在训练模式确认映射与振动，再切到其他窗口至少一个状态写入周期。`lolWindowForeground` 应变为 false，布局变为 `0`，清除报告计数停止增长。只有设备/接口检查通过，不能证明 Steam 布局已正确配置。
