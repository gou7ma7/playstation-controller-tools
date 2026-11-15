using System;

namespace ControllerLight {
    // USB report 0x01 only. Never interpret packet counters or motion as activity.
    // 只解释 USB 0x01；包计数、传感器、电量和触摸坐标都不算操作。
    internal sealed class ControllerActivity {
        private readonly int deadzone, triggerThreshold;
        private readonly ControllerKind kind;
        private bool baseline;
        private int previousButtons;
        private bool previousAnalog;
        internal ControllerActivity(int stickDeadzone, int minimumTrigger) : this(ControllerKind.DualShock4V2, stickDeadzone, minimumTrigger) { }
        internal ControllerActivity(ControllerKind controllerKind, int stickDeadzone, int minimumTrigger) { kind = controllerKind; deadzone = stickDeadzone; triggerThreshold = minimumTrigger; }
        internal bool Observe(byte[] report, int length, out bool activity) {
            activity = false;
            if (report == null || length != 64 || report.Length < length || report[0] != 1) return false;
            int buttonOffset = kind == ControllerKind.DualSense ? 8 : 5;
            int triggerOffset = kind == ControllerKind.DualSense ? 5 : 8;
            int systemMask = kind == ControllerKind.DualSense ? 7 : 3; // DualSense mute button counts; status does not.
            int dpad = report[buttonOffset] & 15;
            if (dpad > 8) return false;
            // L2/R2 digital bits are ignored: use analog threshold to avoid hair-trigger noise.
            int buttons = (report[buttonOffset] & 0xf0) | ((report[buttonOffset + 1] & 0xf3) << 8) | ((report[buttonOffset + 2] & systemMask) << 16) | (dpad << 20);
            bool heldButtons = (buttons & 0xfffff) != 0 || dpad != 8;
            bool analog = Math.Abs((int)report[1] - 128) > deadzone || Math.Abs((int)report[2] - 128) > deadzone
                || Math.Abs((int)report[3] - 128) > deadzone || Math.Abs((int)report[4] - 128) > deadzone
                || report[triggerOffset] >= triggerThreshold || report[triggerOffset + 1] >= triggerThreshold;
            if (baseline) activity = heldButtons || analog || buttons != previousButtons || analog != previousAnalog;
            baseline = true; previousButtons = buttons; previousAnalog = analog;
            return true;
        }
    }

    internal enum IdleLightAction { None, Off, Color }
    // Pure policy. A held control counts as use; neutral reports do not reset the timer.
    // 纯状态机：持续按住算使用；静置时持续上报不重置倒计时。
    internal sealed class IdleLightPolicy {
        private readonly int timeoutMs, refreshMs, retryMs;
        private bool connected, pendingColor;
        private double lastActivity, nextWrite;
        internal bool Dark { get; private set; }
        internal IdleLightPolicy(int timeout, int refresh, int retry) { timeoutMs = timeout; refreshMs = refresh; retryMs = retry; }
        internal double IdleMilliseconds(double now) { return Math.Max(0, now - lastActivity); }
        internal void Connected(double now) { connected = true; lastActivity = now; nextWrite = now; }
        internal void Disconnected() { connected = false; pendingColor = false; }
        internal void Suspend() { Dark = true; Disconnected(); nextWrite = 0; }
        internal void Activity(double now) {
            lastActivity = now;
            if (Dark) { Dark = false; pendingColor = true; nextWrite = now; }
        }
        internal IdleLightAction Next(double now) {
            if (!connected) return IdleLightAction.None;
            if (!Dark && now - lastActivity >= timeoutMs) { Dark = true; pendingColor = false; nextWrite = now; }
            if (now < nextWrite) return IdleLightAction.None;
            if (Dark) return IdleLightAction.Off;
            return pendingColor ? IdleLightAction.Color : IdleLightAction.None;
        }
        internal void Completed(IdleLightAction action, bool success, double now) {
            if (action == IdleLightAction.Color && success) pendingColor = false;
            nextWrite = now + (success ? (refreshMs == 0 ? double.PositiveInfinity : refreshMs) : retryMs);
        }
    }
}
