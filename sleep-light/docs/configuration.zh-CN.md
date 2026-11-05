# 配置与验证

[English](configuration.md)

设置来自 `config/SleepLightConfig.cs`，会编入程序。修改后退出正在运行的工具，重新编译并启动。不需要运行时配置文件或其他模块。

- `RestoreOnResume`：默认 `true`，用户恢复使用/解锁后恢复配置灯色。设为 `false` 则保持熄灭，直到其他软件写入。
- `Red`、`Green`、`Blue`：整数 `0..255`，默认 `0,0,255`。低亮度蓝可用 `0,0,32`，紫色可用 `96,0,96`。这是原始 RGB 数值，不是 Steam 的亮度滑块。全部为零则恢复后也保持黑色。
- `RestoreDelayMs`：`0..10000`，默认 `1500` 毫秒。用户恢复使用/解锁后等待 USB 就绪；设备重新枚举慢时可以增加。
- `RestoreRetryWindowMs`：`500..30000`，默认 `5000` 毫秒。从初始延迟结束后计算的重试窗口。超时后等待下一次睡眠周期，不持续写入。
- `RestoreRetryIntervalMs`：`100..5000`，默认 `500` 毫秒。恢复写入失败后的重试间隔，成功则立即停止重试。
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

实际验证时保持工具运行，分别手动测试 S3 睡眠和 S4 休眠。观察灭灯与恢复灯色，从托盘打开当前日志。`suspend_off ... outcome=written` 只表示 Windows 完成了报告写入，不能代替肉眼确认。后台唤醒应记录 `automatic_resume_no_light_write`，直到用户恢复使用/解锁才恢复灯色。

## 日志诊断

优先写入 EXE 旁的 `logs/events.log`；程序目录不可写时回退到 `%LOCALAPPDATA%\DS4SleepLight\events.log`。托盘 **打开日志** 指向当前实际文件。`EnableLocalLog = false` 可关闭记录；超过 1 MiB 轮换为 `events.log.old`，排查时可一起保留。旧版本 AppData 日志不会自动删除或合并。

每行有 UTC/本地时间、临时 `run` 编号、记录顺序、进程/线程编号和运行耗时，用于对照事件，不是设备或账号标识。不会记录设备路径、序列号、账号或比赛数据。

- `power_received` / `power_handled`：Windows 通知与处理前后状态。4 为睡眠通知，18 为后台自动唤醒，7 为用户主动恢复。
- `session_received` / `session_apply_begin` / `session_applied`：区分锁屏/解锁回调收到和排队到界面线程后的处理时机，并记录状态机。
- `suspend_off_begin` / `suspend_off`：灭灯操作开始/完成、结果及总耗时。
- `usb_operation_*`、`usb_enumeration_*`、`usb_write_attempt_*`：操作类别、匹配数量、发送的 RGB、每次写入结果、Win32 错误和耗时。`usb_cancel_wait_*` 标记取消清理的开始/结束，可发现驱动是否延迟完成取消。
- `interactive_resume_restore_scheduled`、`resume_restore_*`：恢复延迟、RGB、写入结果、重试或完成。后台自动唤醒仍不恢复灯色。
- `heartbeat_no_usb_write`：每分钟记录界面循环状态。`ui_timer_gap` 记录超过三秒的实际时间间隔，可能由睡眠、界面阻塞或时钟变化造成；不是独立的睡眠检测器，也不发送 USB 指令。
- `device_change_no_usb_write`：通用 Windows 设备变更通知，不识别具体设备、不轮询 USB。
- `manual_test_begin` / `manual_test_completed`：实际请求的测试秒数，与真实睡眠通知区分。命令行测试拥有独立运行/进程编号。

例如，睡眠前开始的灭灯操作直到唤醒后才完成，提示通知处理或 IO 延迟。灭灯写入已完成、肉眼却看到另一种颜色，并不能单独证明是谁改色：需要结合观察时机和 Windows 电源事件。日志不会读取实际灯条颜色或追踪 Steam 写入。

若其他软件紧接着覆盖灯色，调整延迟/重复次数可能有帮助，但不能保证。不要默认通过关闭 USB 节电、安装 HID 隐藏驱动或替换已正常工作的 Steam 布局来排查；进一步改动应依据观察到的行为。
