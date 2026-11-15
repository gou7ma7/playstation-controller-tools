# 配置与验证

[English](configuration.md)

设置来自 `config/LightConfig.cs`，会编入程序。修改后退出正在运行的工具，重新编译并启动。不需要运行时配置文件或其他模块。

- `EnableIdleLight`：默认 `true`，启用手柄空闲控制；恢复电脑/解锁本身不点亮。设为 `false` 切回仅睡眠模式，该模式不启动输入读取线程。
- `IdleTimeoutSeconds`：`10..3600`，默认 `180` 秒。从首个有效输入基线或最后一次手柄操作计时；静置上报、鼠标/键盘活动都不重置。重新连接或界面长时间停止后建立新基线，不使用排队的旧输入。
- `StickDeadzone`：`4..96`，默认 `32`。每个摇杆轴距中心 128 的容差；超出时持续算使用，范围内微动忽略。漂移时可调大，但过大会使轻推不被识别；超出容差的严重漂移可能导致无法空闲灭灯。
- `TriggerThreshold`：`1..255`，默认 `12`。L2/R2 模拟值达到此阈值算使用，单独的扳机数字位忽略，避免微小噪声。其他按键/方向键按下与松开都算，PS 键与触摸板按下也算；传感器和触摸坐标不算。
- `IdleOffRefreshMs`：`0` 或 `1000..60000`，默认 `5000` 毫秒。空闲且电脑仍运行时重发灭灯；`0` 仅发送首次成功的灭灯。越小越快覆盖其他软件改灯，但写入更多；完全休眠时无法执行。正常使用不周期性重发配置颜色。
- `IdleWriteRetryMs`：`500..10000`，默认 `3000` 毫秒。空闲模式输出失败后的重试间隔；必须仍有新鲜、有效输入，断线或目标不明确时不发送。
- `RestoreOnResume`：默认 `true`，**仅 `EnableIdleLight = false` 时生效**。仅睡眠模式恢复/解锁后恢复颜色；设为 `false` 则不恢复。它不关闭空闲模式的手柄操作恢复。
- `Red`、`Green`、`Blue`：整数 `0..255`，默认 `0,0,255`。低亮度蓝可用 `0,0,32`，紫色可用 `96,0,96`。这是原始 RGB 数值，不是 Steam 的亮度滑块。全部为零则恢复后也保持黑色。
- `ControlDualSensePlayerLeds`：默认 `true`，仅 DualSense 生效。RGB 与白色玩家灯一起控制；灭灯时清空五个白灯。设为 `false` 则完全留给其他软件管理玩家灯。麦克风静音状态灯始终不操作。
- `DualSensePlayerLeds`：`0..31`，默认 `4`（中央一灯）。恢复时使用五位掩码，`1`、`2`、`4`、`8`、`16` 分别选择各灯；`0` 只恢复 RGB，不亮白灯，`31` 点亮五灯。仅在玩家灯控制开启且请求 RGB 非零时使用，不能还原 Steam 之前的灯位。
- `RestoreDelayMs`：`0..10000`，默认 `1500` 毫秒。仅睡眠模式恢复/解锁后等待 USB 就绪的延迟。
- `RestoreRetryWindowMs`：`500..30000`，默认 `5000` 毫秒。仅睡眠模式延迟结束后的重试窗口，超时后等待下一次睡眠周期。
- `RestoreRetryIntervalMs`：`100..5000`，默认 `500` 毫秒。仅睡眠模式恢复失败后的重试间隔，成功则立即停止。
- `OffWriteAttempts`：`1..3`，默认 `2` 次。在睡眠回调中短暂重复灭灯，不会产生睡眠期间运行的任务。
- `OffRepeatDelayMs`：`0..100`，默认 `50` 毫秒。重复灭灯报告的间隔。
- `UsbWriteTimeoutMs`：`50..500`，默认 `200` 毫秒。待处理写入超时后请求取消；缓冲区和 OVERLAPPED 内存保留到系统完成或取消 IO。异常驱动可能延迟取消，因此不是绝对的总耗时上限。
- `EnableLocalLog`：默认 `true`，改为 `false` 可停止写轮换本地日志；日志不记录设备/游戏身份数据。

示例：保留默认蓝色但调暗，增加 USB 恢复等待：

```csharp
internal static readonly int Red = 0, Green = 0, Blue = 32;
internal static readonly int RestoreDelayMs = 2500;
```

从模块目录编译：

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Language zh
```

构建验证配置范围，并使用模拟检查覆盖报告字段、设备能力筛选、重复通知、解锁延迟恢复、待恢复任务取消，以及 S3 → 自动唤醒 → S4。测试程序不调用原生 USB 接口。

也覆盖 DS4 与 DualSense USB 按键布局、错误格式、噪声过滤、持续按住、准确的 180 秒边界、重试、手柄操作恢复以及断线/睡眠取消。输入解析和空闲状态机在 `src/ControllerActivity.cs`，共享、可取消的重叠 `ReadFile` 在 `src/ControllerInput.cs`。参考 [微软 HID 输入说明](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/obtaining-hid-reports) 与 [Linux PlayStation 报告格式](https://github.com/torvalds/linux/blob/master/drivers/hid/hid-playstation.c)。支持的 DualSense 游戏手柄集合在 Windows 上声明输出为 48 字节，Linux 的 USB 输出结构包含不同长度的填充；不能套用 DS4 的 32 字节或蓝牙的 78 字节布局。

只读输入诊断（不发送灯光报告）：

```powershell
Start-Process .\dist\ControllerLight.exe -ArgumentList '--input-diagnose 3 dist/input-diagnostic.json' -WindowStyle Hidden -Wait
Get-Content .\dist\input-diagnostic.json
```

只报告输入总数，不报告按键值。实际空闲测试：运行工具，三分钟不碰手柄，再按键/推动摇杆；鼠标或键盘操作应不点亮。在 Steam 运行时再确认。写入成功本身不代表肉眼灯光效果。

实际睡眠验证时分别手动测试 S3 和 S4。`suspend_off ... outcome=written` 只表示 Windows 完成了报告写入，不能代替肉眼确认。后台唤醒应记录 `automatic_resume_no_light_write`；空闲模式需操作手柄才恢复，仅睡眠模式则恢复/解锁后点亮。

## 日志诊断

优先写入 EXE 旁的 `logs/events.log`；程序目录不可写时回退到 `%LOCALAPPDATA%\ControllerLight\events.log`。托盘 **打开日志** 指向当前实际文件。`EnableLocalLog = false` 可关闭记录；超过 1 MiB 轮换为 `events.log.old`，排查时可一起保留。旧版本 AppData 日志不会自动删除或合并。

每行有 UTC/本地时间、临时 `run` 编号、记录顺序、进程/线程编号和运行耗时，用于对照事件，不是设备或账号标识。不会记录设备路径、序列号、账号或比赛数据。

- `power_received` / `power_handled`：Windows 通知与处理前后状态。4 为睡眠通知，18 为后台自动唤醒，7 为用户主动恢复。
- `session_received` / `session_apply_begin` / `session_applied`：区分锁屏/解锁回调收到和排队到界面线程后的处理时机，并记录状态机。
- `suspend_off_begin` / `suspend_off`：灭灯操作开始/完成、结果及总耗时。
- `usb_operation_*`、`usb_enumeration_*`、`usb_write_attempt_*`：操作类别、匹配数量、发送的 RGB、每次写入结果、Win32 错误和耗时。`usb_cancel_wait_*` 标记取消清理的开始/结束，可发现驱动是否延迟完成取消。
- `usb_light_target`：选择的手柄类型和输出长度，不记录设备身份或路径。诊断 JSON 与输入连接/运行状态也区分 `DualShock4V2` 和 `DualSense`。
- `interactive_resume_restore_scheduled`、`resume_restore_*`：恢复延迟、RGB、写入结果、重试或完成。后台自动唤醒仍不恢复灯色。
- `input_status`、`input_connected_no_light_write`、`idle_input_baseline_no_light_write`、`idle_input_lost_no_light_write`：输入访问、新基线以及输入不可用；基线不发送点亮颜色。
- `idle_timeout_off`：灭灯结果与空闲时长。睡眠/重新连接后状态本来已黑时也用此记录重发灭灯；`idle_ms` 小于阈值可区分该情况。
- `idle_controller_activity_restore_requested`、`idle_activity_color`：黑色状态下检测到手柄操作，以及之后的配置颜色写入；不记录原始输入。
- `idle_off_refresh`、`idle_input_heartbeat_no_light_write`：重复灭灯汇总和输入报告总数。静置手柄报告总数很高、`idle_ms` 仍增长是正常现象；运行状态记录本身不操作 USB。
- `heartbeat_no_usb_write`：每分钟记录界面循环状态。`ui_timer_gap` 记录超过三秒的实际时间间隔，可能由睡眠、界面阻塞或时钟变化造成；不是独立的睡眠检测器，也不发送 USB 指令。
- `device_change_no_usb_write`：通用 Windows 设备变更通知，不识别具体设备、不轮询 USB。
- `manual_test_begin` / `manual_test_completed`：实际请求的测试秒数，与真实睡眠通知区分。命令行测试拥有独立运行/进程编号。

例如，睡眠前开始的灭灯操作直到唤醒后才完成，提示通知处理或 IO 延迟。灭灯写入已完成、肉眼却看到另一种颜色，并不能单独证明是谁改色：需要结合观察时机和 Windows 电源事件。日志不会读取实际灯条颜色或追踪 Steam 写入。

若其他软件紧接着覆盖灯色，调整延迟/重复次数可能有帮助，但不能保证。不要默认通过关闭 USB 节电、安装 HID 隐藏驱动或替换已正常工作的 Steam 布局来排查；进一步改动应依据观察到的行为。
