namespace ControllerLight {
    // Edit this file and rebuild. 修改此文件后重新编译。
    internal static class LightConfig {
        // Default: controller inactivity, not mouse/keyboard or USB report arrival.
        // 默认按手柄操作判断空闲；鼠标/键盘活动和 USB 数据上报不影响倒计时。
        internal static readonly bool EnableIdleLight = true;
        internal static readonly int IdleTimeoutSeconds = 180; // 10..3600; default three minutes / 默认三分钟
        // Per-axis distance from center 128; ignore small drift/jitter (4..96).
        // 各摇杆轴距中心 128 的容差，过滤漂移/抖动（4..96）。超出范围持续偏移会算操作。
        internal static readonly int StickDeadzone = 32;
        internal static readonly int TriggerThreshold = 12; // 1..255; ignore tiny trigger noise / 忽略扳机微小噪声
        // While idle and awake, reassert off; never resend color during normal use.
        // 空闲且电脑仍运行时低频重发灭灯；正常使用不反复写灯色。0 = no refresh / 不重发
        internal static readonly int IdleOffRefreshMs = 5000; // 0 or 1000..60000 / 或
        internal static readonly int IdleWriteRetryMs = 3000; // 500..10000; unavailable output / 写入失败重试间隔
        // Used only when EnableIdleLight=false. Idle mode waits for controller input.
        // 仅 EnableIdleLight=false 时生效；空闲模式解锁不点亮，操作手柄才恢复。
        internal static readonly bool RestoreOnResume = true;
        // RGB channels, 0..255. Default blue. RGB 通道 0..255，默认蓝色。
        internal static readonly int Red = 0, Green = 0, Blue = 255;
        // DualSense only: turn white player LEDs off with RGB; restore this five-bit mask.
        // 仅 PS5：白色玩家灯随 RGB 灭灯；恢复时使用下列五位掩码。静音状态灯始终不操作。
        internal static readonly bool ControlDualSensePlayerLeds = true;
        internal static readonly int DualSensePlayerLeds = 4; // 0..31; 4 = center LED / 中央一灯；0 = none / 不亮
        // Wait for the USB stack after user resume, milliseconds (0..10000).
        // 用户恢复后等待 USB 就绪的毫秒数（0..10000）。
        internal static readonly int RestoreDelayMs = 1500;
        // Retry missing/busy devices for this long after the delay (500..30000 ms).
        // 延迟结束后，设备尚未就绪时重试的总时间（500..30000 毫秒）。
        internal static readonly int RestoreRetryWindowMs = 5000;
        internal static readonly int RestoreRetryIntervalMs = 500; // 100..5000 ms / 毫秒
        // Short repeated off writes during the suspend notification, not while asleep.
        // 睡眠通知中短暂重复灭灯；不会在睡眠中持续运行。1..3 次。
        internal static readonly int OffWriteAttempts = 2;
        internal static readonly int OffRepeatDelayMs = 50; // 0..100 ms / 毫秒
        // Cancel a pending USB write after this timeout (50..500 ms).
        // USB 写入超过此毫秒数时请求取消（50..500）。
        internal static readonly int UsbWriteTimeoutMs = 200;
        // Local diagnostic log contains no device path/serial, account or game data.
        // 本地日志不包含设备路径/序列号、账号或游戏数据。
        internal static readonly bool EnableLocalLog = true;

        internal static void Validate() {
            Range("Red", Red, 0, 255); Range("Green", Green, 0, 255); Range("Blue", Blue, 0, 255);
            Range("DualSensePlayerLeds", DualSensePlayerLeds, 0, 31);
            Range("IdleTimeoutSeconds", IdleTimeoutSeconds, 10, 3600);
            Range("StickDeadzone", StickDeadzone, 4, 96); Range("TriggerThreshold", TriggerThreshold, 1, 255);
            if (IdleOffRefreshMs != 0) Range("IdleOffRefreshMs", IdleOffRefreshMs, 1000, 60000);
            Range("IdleWriteRetryMs", IdleWriteRetryMs, 500, 10000);
            Range("RestoreDelayMs", RestoreDelayMs, 0, 10000);
            Range("RestoreRetryWindowMs", RestoreRetryWindowMs, 500, 30000);
            Range("RestoreRetryIntervalMs", RestoreRetryIntervalMs, 100, 5000);
            Range("OffWriteAttempts", OffWriteAttempts, 1, 3);
            Range("OffRepeatDelayMs", OffRepeatDelayMs, 0, 100);
            Range("UsbWriteTimeoutMs", UsbWriteTimeoutMs, 50, 500);
        }
        private static void Range(string name, int value, int min, int max) {
            if (value < min || value > max) throw new System.ArgumentOutOfRangeException(name, name + " must be " + min + ".." + max + ".");
        }
    }
}
