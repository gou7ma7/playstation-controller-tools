using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ControllerLight {
    internal static class Ui {
        internal static string Text(string zh, string en) {
#if UI_ZH
            return zh;
#elif UI_EN
            return en;
#else
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? zh : en;
#endif
        }
    }
    internal static class Log {
        internal const int MaximumBytes = 1024 * 1024;
        private static readonly object gate = new object();
        private static readonly string run = Guid.NewGuid().ToString("N").Substring(0, 8);
        private static readonly Stopwatch clock = Stopwatch.StartNew();
        private static readonly int processId = Process.GetCurrentProcess().Id;
        private static long sequence;
        private static string activeFolder;
        private static string PrimaryFolder { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"); } }
        internal static string FilePath { get { lock (gate) { return Path.Combine(activeFolder ?? PrimaryFolder, "events.log"); } } }
        internal static void Write(string action, WriteResult result) {
            Write(action, result, "");
        }
        internal static void Event(string action, string detail) { Write(action, null, detail); }
        internal static void Write(string action, WriteResult result, string detail) {
            if (!LightConfig.EnableLocalLog) return;
            lock (gate) {
                DateTime utc = DateTime.UtcNow;
                string fields = result == null ? "" : "outcome=" + result.Outcome + " matches=" + result.Matches + " writes=" + result.Writes + " win32=" + result.Win32Error + " controller=" + result.Controller;
                string line = utc.ToString("o", CultureInfo.InvariantCulture) + " local=" + utc.ToLocalTime().ToString("o", CultureInfo.InvariantCulture)
                    + " run=" + run + " seq=" + (++sequence).ToString(CultureInfo.InvariantCulture) + " pid=" + processId
                    + " tid=" + Thread.CurrentThread.ManagedThreadId + " elapsed_ms=" + clock.ElapsedMilliseconds
                    + " " + action + " " + fields + " " + detail;
                line = line.Replace('\r', ' ').Replace('\n', ' ').TrimEnd() + Environment.NewLine;
                if (activeFolder != null && TryAppend(activeFolder, line)) return;
                if (TryAppend(PrimaryFolder, line)) { activeFolder = PrimaryFolder; return; }
                string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ControllerLight");
                if (TryAppend(fallback, line)) activeFolder = fallback;
            }
        }
        // Best-effort logging must never turn a power callback into a fatal error.
        // 日志无法写入时不影响电源处理；程序目录不可写则回退至用户目录。
        internal static bool TryAppend(string folder, string line) {
            try {
                Directory.CreateDirectory(folder);
                string file = Path.Combine(folder, "events.log");
                if (File.Exists(file) && new FileInfo(file).Length > MaximumBytes) { string old = file + ".old"; if (File.Exists(old)) File.Delete(old); File.Move(file, old); }
                File.AppendAllText(file, line); return true;
            } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; } catch (System.Security.SecurityException) { return false; }
        }
        internal static void SaveDiagnostic(string path, WriteResult result) {
            File.WriteAllText(Path.GetFullPath(path), new JavaScriptSerializer().Serialize(new {
                version = 1, supportedDevice = "USB DS4 v2 054C:09CC or DualSense 054C:0CE6", controller = result.Controller, outcome = result.Outcome,
                matchingControllers = result.Matches, reportsWritten = result.Writes, win32Error = result.Win32Error
            }));
        }
    }
    internal sealed class LightApp : Form {
        private readonly SleepPolicy policy = new SleepPolicy(!LightConfig.EnableIdleLight && LightConfig.RestoreOnResume);
        private readonly IdleLightPolicy idle = new IdleLightPolicy(LightConfig.IdleTimeoutSeconds * 1000, LightConfig.IdleOffRefreshMs, LightConfig.IdleWriteRetryMs);
        private readonly ControllerInput input;
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly ToolStripMenuItem status;
        private double restoreAt, restoreDeadline, nextAttempt;
        private double nextHeartbeat;
        private DateTime lastTickUtc = DateTime.UtcNow;
        private bool ending;
        private bool inputWasConnected, idleOffReported;
        private long inputGeneration = -1, inputActivity, idleRefreshes;
        private double nextIdleRefreshLog;

        internal LightApp() {
            Text = "ControllerLight";
            // Create a top-level HWND so WM_POWERBROADCAST arrives even without a visible window.
            // 创建顶层窗口句柄；不显示窗口，也能接收电源广播。
            IntPtr window = Handle;
            ContextMenuStrip menu = new ContextMenuStrip();
            status = new ToolStripMenuItem(LightConfig.EnableIdleLight ? Ui.Text("等待手柄，空闲三分钟灭灯", "Waiting for controller; idle light enabled") : Ui.Text("等待睡眠/休眠", "Waiting for sleep/hibernate")); status.Enabled = false; menu.Items.Add(status);
            ToolStripMenuItem diagnostic = new ToolStripMenuItem(Ui.Text("检查连接（不改变灯光）", "Check connection (no light changes)"));
            diagnostic.Click += delegate { WriteResult result = NativeHid.Diagnose(); Log.Write("diagnose", result); SetStatus(result.Outcome == "output_accessible_no_write" ? Ui.Text("支持的手柄可写入", "Supported controller accessible") : FailureText(result)); };
            menu.Items.Add(diagnostic);
            ToolStripMenuItem openLog = new ToolStripMenuItem(Ui.Text("打开日志", "Open log"));
            openLog.Click += delegate {
                if (!File.Exists(Log.FilePath)) { SetStatus(Ui.Text("尚无日志或日志已关闭", "No log yet or logging disabled")); return; }
                try { Process.Start("notepad.exe", "\"" + Log.FilePath + "\""); }
                catch (System.ComponentModel.Win32Exception) { SetStatus(Ui.Text("无法打开日志文件", "Cannot open log file")); }
            }; menu.Items.Add(openLog);
            ToolStripMenuItem exit = new ToolStripMenuItem(Ui.Text("退出", "Exit")); exit.Click += delegate { Close(); }; menu.Items.Add(exit);
            tray.Icon = appIcon; tray.ContextMenuStrip = menu; tray.Visible = true;
            SetStatus(status.Text);
            if (LightConfig.EnableIdleLight) { input = new ControllerInput(); input.Enable(true); }
            timer.Interval = 100; timer.Tick += delegate { TraceTick(); RestoreTick(); IdleTick(); }; timer.Start();
            SystemEvents.SessionSwitch += SessionChanged;
            Trace("watcher_started_no_light_write", "log_schema=4 rgb=" + LightConfig.Red + "," + LightConfig.Green + "," + LightConfig.Blue
                + " idle_enabled=" + LightConfig.EnableIdleLight + " idle_seconds=" + LightConfig.IdleTimeoutSeconds
                + " stick_deadzone=" + LightConfig.StickDeadzone + " trigger_threshold=" + LightConfig.TriggerThreshold + " idle_off_refresh_ms=" + LightConfig.IdleOffRefreshMs
                + " restore=" + LightConfig.RestoreOnResume + " restore_delay_ms=" + LightConfig.RestoreDelayMs
                + " retry_window_ms=" + LightConfig.RestoreRetryWindowMs + " off_attempts=" + LightConfig.OffWriteAttempts
                + " off_gap_ms=" + LightConfig.OffRepeatDelayMs + " usb_timeout_ms=" + LightConfig.UsbWriteTimeoutMs);
        }
        private void Trace(string action, string detail) { Log.Event(action, "phase=" + policy.Phase + " locked=" + policy.Locked + " idle_dark=" + idle.Dark + " " + detail); }
        private void TraceTick() {
            DateTime utc = DateTime.UtcNow;
            double gap = (utc - lastTickUtc).TotalMilliseconds;
            lastTickUtc = utc;
            if (gap > 3000) {
                Trace("ui_timer_gap", "wall_gap_ms=" + gap.ToString("0", CultureInfo.InvariantCulture));
                // Discard input buffered while the UI/OS was not running.
                if (input != null) { input.Enable(false); inputWasConnected = false; idle.Disconnected(); RefreshInputAccess(); }
            }
            if (clock.Elapsed.TotalMilliseconds >= nextHeartbeat) {
                nextHeartbeat = clock.Elapsed.TotalMilliseconds + 60000;
                Trace("heartbeat_no_usb_write", "restore_due_in_ms=" + (policy.Phase == LightPhase.RestorePending ? Math.Max(0, nextAttempt - clock.Elapsed.TotalMilliseconds).ToString("0", CultureInfo.InvariantCulture) : "none"));
                if (input != null) { InputSnapshot snapshot = input.Snapshot(); Trace("idle_input_heartbeat_no_light_write", "input_status=" + snapshot.Status + " controller=" + snapshot.Controller + " reports=" + snapshot.Reports + " idle_ms=" + idle.IdleMilliseconds(clock.Elapsed.TotalMilliseconds).ToString("0", CultureInfo.InvariantCulture) + " refreshes=" + idleRefreshes); }
            }
        }
        private static string PowerName(int id) {
            if (id == 4) return "PBT_APMSUSPEND";
            if (id == 7) return "PBT_APMRESUMESUSPEND";
            if (id == 18) return "PBT_APMRESUMEAUTOMATIC";
            if (id == 10) return "PBT_APMPOWERSTATUSCHANGE";
            return "other";
        }
        protected override void SetVisibleCore(bool value) { base.SetVisibleCore(false); }
        protected override void WndProc(ref Message message) {
            if (message.Msg == 0x0218) {
                int notification = message.WParam.ToInt32();
                Trace("power_received", "id=" + notification + " name=" + PowerName(notification));
                if (notification == 4) Suspend(); // PBT_APMSUSPEND
                else if (notification == 18) { policy.AutomaticResume(); Log.Write("automatic_resume_no_light_write", null); }
                else if (notification == 7) { if (policy.UserResume()) ScheduleRestore(); RefreshInputAccess(); } // PBT_APMRESUMESUSPEND
                Trace("power_handled", "id=" + notification + " name=" + PowerName(notification));
            }
            else if (message.Msg == 0x0219) Trace("device_change_no_usb_write", "id=" + message.WParam.ToInt64());
            base.WndProc(ref message);
        }
        private void Suspend() {
            if (!policy.Suspend()) { Trace("duplicate_suspend_ignored", ""); return; }
            if (input != null) { input.Enable(false); inputWasConnected = false; idle.Suspend(); idleOffReported = false; }
            Trace("suspend_off_begin", "rgb=0,0,0");
            Stopwatch duration = Stopwatch.StartNew();
            WriteResult result = NativeHid.Send(0, 0, 0, LightConfig.OffWriteAttempts, LightConfig.OffRepeatDelayMs);
            Log.Write("suspend_off", result, "duration_ms=" + duration.ElapsedMilliseconds + " phase=" + policy.Phase);
            SetStatus(result.Success ? Ui.Text("已发送灭灯指令", "Light-off command sent") : FailureText(result));
        }
        private void SessionChanged(object sender, SessionSwitchEventArgs e) {
            Log.Event("session_received", "reason=" + e.Reason);
            if (ending) { Log.Event("session_ignored_ending", "reason=" + e.Reason); return; }
            try {
                BeginInvoke(new Action(delegate {
                    Trace("session_apply_begin", "reason=" + e.Reason);
                    if (e.Reason == SessionSwitchReason.SessionLock) policy.Lock();
                    else if (e.Reason == SessionSwitchReason.SessionUnlock && policy.Unlock()) ScheduleRestore();
                    RefreshInputAccess();
                    Trace("session_applied", "reason=" + e.Reason);
                }));
            } catch (InvalidOperationException) { Log.Event("session_queue_unavailable", "reason=" + e.Reason); }
        }
        private void RefreshInputAccess() {
            if (input != null) input.Enable(!ending && policy.Phase == LightPhase.Ready);
        }
        private void IdleTick() {
            if (input == null || policy.Phase != LightPhase.Ready) return;
            InputSnapshot snapshot = input.Snapshot();
            if (!snapshot.Connected || ControllerInput.Now - snapshot.LastReportMs > 1000) {
                if (inputWasConnected) { idle.Disconnected(); inputWasConnected = false; Trace("idle_input_lost_no_light_write", "status=" + snapshot.Status); SetStatus(Ui.Text("输入不可用，暂停空闲控制", "Input unavailable; idle control paused")); }
                return;
            }
            double now = clock.Elapsed.TotalMilliseconds;
            if (!inputWasConnected || inputGeneration != snapshot.Generation) {
                inputWasConnected = true; inputGeneration = snapshot.Generation; inputActivity = snapshot.Activity;
                idle.Connected(now); Trace("idle_input_baseline_no_light_write", "timeout_seconds=" + LightConfig.IdleTimeoutSeconds);
                SetStatus(Ui.Text("监听手柄空闲", "Watching controller inactivity"));
            } else if (snapshot.Activity != inputActivity) {
                inputActivity = snapshot.Activity;
                if (!policy.Locked) {
                    bool wasDark = idle.Dark; double idleMs = idle.IdleMilliseconds(now); idle.Activity(now);
                    if (wasDark) { idleOffReported = false; Trace("idle_controller_activity_restore_requested", "idle_ms=" + idleMs.ToString("0", CultureInfo.InvariantCulture)); }
                }
            }
            IdleLightAction action = idle.Next(now);
            if (action == IdleLightAction.None || (action == IdleLightAction.Color && policy.Locked)) return;
            bool refresh = action == IdleLightAction.Off && idleOffReported;
            WriteResult result = refresh ? NativeHid.SendQuiet(0, 0, 0)
                : NativeHid.Send(action == IdleLightAction.Off ? 0 : LightConfig.Red, action == IdleLightAction.Off ? 0 : LightConfig.Green, action == IdleLightAction.Off ? 0 : LightConfig.Blue, 1, 0);
            idle.Completed(action, result.Success, clock.Elapsed.TotalMilliseconds);
            if (refresh) {
                idleRefreshes++;
                if (!result.Success || now >= nextIdleRefreshLog) { nextIdleRefreshLog = now + 60000; Log.Write("idle_off_refresh", result, "rgb=0,0,0 refreshes=" + idleRefreshes); }
            } else {
                Log.Write(action == IdleLightAction.Off ? "idle_timeout_off" : "idle_activity_color", result, "idle_ms=" + idle.IdleMilliseconds(now).ToString("0", CultureInfo.InvariantCulture));
            }
            if (result.Success) {
                if (action == IdleLightAction.Off) { idleOffReported = true; SetStatus(Ui.Text("空闲已灭灯，操作手柄恢复", "Idle: light off; use controller to restore")); }
                else SetStatus(Ui.Text("已恢复配置灯色，监听空闲", "Configured color restored; watching idle"));
            } else SetStatus(FailureText(result));
        }
        private void ScheduleRestore() {
            restoreAt = clock.Elapsed.TotalMilliseconds + LightConfig.RestoreDelayMs;
            nextAttempt = restoreAt;
            restoreDeadline = restoreAt + LightConfig.RestoreRetryWindowMs;
            SetStatus(Ui.Text("恢复使用，等待 USB 就绪", "User resumed; waiting for USB"));
            Trace("interactive_resume_restore_scheduled", "delay_ms=" + LightConfig.RestoreDelayMs + " retry_window_ms=" + LightConfig.RestoreRetryWindowMs);
        }
        private void RestoreTick() {
            if (policy.Phase != LightPhase.RestorePending || clock.Elapsed.TotalMilliseconds < nextAttempt) return;
            if (clock.Elapsed.TotalMilliseconds > restoreDeadline) {
                policy.CompleteRestore(); SetStatus(Ui.Text("恢复灯色超时，请检查连接", "Color restore timed out; check connection")); Trace("restore_window_expired", ""); return;
            }
            Trace("resume_restore_begin", "rgb=" + LightConfig.Red + "," + LightConfig.Green + "," + LightConfig.Blue);
            Stopwatch duration = Stopwatch.StartNew();
            WriteResult result = NativeHid.Send(LightConfig.Red, LightConfig.Green, LightConfig.Blue, 1, 0);
            Log.Write("resume_restore", result, "duration_ms=" + duration.ElapsedMilliseconds + " phase=" + policy.Phase);
            if (result.Success) { policy.CompleteRestore(); SetStatus(Ui.Text("已恢复配置灯色，等待睡眠", "Configured color restored; waiting for sleep")); }
            else { nextAttempt = clock.Elapsed.TotalMilliseconds + LightConfig.RestoreRetryIntervalMs; SetStatus(FailureText(result)); }
            Trace("resume_restore_handled", "retry_in_ms=" + (result.Success ? "none" : LightConfig.RestoreRetryIntervalMs.ToString(CultureInfo.InvariantCulture)));
        }
        private static string FailureText(WriteResult result) {
            if (result.Outcome == "no_supported_device") return Ui.Text("未连接支持的 USB PS4/PS5 手柄", "Supported USB PS4/PS5 controller not connected");
            if (result.Outcome == "multiple_devices_refused") return Ui.Text("多个手柄，未发送指令", "Multiple controllers; no command sent");
            return Ui.Text("写入未完成，请检查连接/日志", "Write unavailable; check connection/log");
        }
        private void SetStatus(string text) { status.Text = text; string tooltip = "ControllerLight: " + text; tray.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip; }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            ending = true; timer.Stop(); SystemEvents.SessionSwitch -= SessionChanged;
            if (input != null) input.Dispose();
            Trace("watcher_stopping_no_light_write", "");
            tray.Visible = false; tray.Dispose(); appIcon.Dispose(); timer.Dispose();
            Log.Write("watcher_stopped_no_light_write", null); base.OnFormClosing(e);
        }
    }
    internal static class Program {
        // Legacy IPC names deliberately retained: old/new executable aliases must
        // share one instance and be able to stop a pre-rename installation.
        // 保留旧 IPC 标识，使新旧名称共用单实例，并能正常退出旧安装。
        private const string StopEvent = "Local\\DS4SleepLight_Stop_054C_09CC";
        [STAThread] private static int Main(string[] args) {
            try {
                LightConfig.Validate();
                if (args.Length == 1 && args[0] == "--stop") {
                    try { using (EventWaitHandle stop = EventWaitHandle.OpenExisting(StopEvent)) stop.Set(); } catch (WaitHandleCannotBeOpenedException) { }
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--diagnose") { Log.SaveDiagnostic(args[1], NativeHid.Diagnose()); return 0; }
                if (args.Length == 3 && args[0] == "--input-diagnose") {
                    int seconds; if (!int.TryParse(args[1], out seconds) || seconds < 1 || seconds > 30) throw new ArgumentException("Input diagnosis duration must be 1..30 seconds.");
                    InputSnapshot snapshot;
                    using (ControllerInput observer = new ControllerInput()) { observer.Enable(true); Thread.Sleep(seconds * 1000); snapshot = observer.Snapshot(); }
                    File.WriteAllText(Path.GetFullPath(args[2]), new JavaScriptSerializer().Serialize(new { seconds = seconds, sharedRead = true, controller = snapshot.Controller, connected = snapshot.Connected, status = snapshot.Status, reportsRead = snapshot.Reports, activitySamples = snapshot.Activity, reportsWritten = 0 }));
                    return snapshot.Reports > 0 ? 0 : 1;
                }
                if (args.Length == 3 && args[0] == "--test-cycle") {
                    int seconds; if (!int.TryParse(args[1], out seconds) || seconds < 1 || seconds > 30) throw new ArgumentException("Test duration must be 1..30 seconds.");
                    Log.Event("manual_test_begin", "seconds=" + seconds);
                    WriteResult off = NativeHid.Send(0, 0, 0, LightConfig.OffWriteAttempts, LightConfig.OffRepeatDelayMs);
                    WriteResult restored;
                    try { if (off.Success) Thread.Sleep(seconds * 1000); }
                    finally { restored = NativeHid.Send(LightConfig.Red, LightConfig.Green, LightConfig.Blue, 1, 0); }
                    File.WriteAllText(Path.GetFullPath(args[2]), new JavaScriptSerializer().Serialize(new { offWritten = off.Success, configuredColorWritten = restored.Success, offOutcome = off.Outcome, restoreOutcome = restored.Outcome, seconds = seconds }));
                    Log.Event("manual_test_completed", "seconds=" + seconds + " off=" + off.Success + " restored=" + restored.Success);
                    return off.Success && restored.Success ? 0 : 1;
                }
                if (args.Length != 0) throw new ArgumentException("Usage: ControllerLight.exe [--stop | --diagnose file.json | --input-diagnose seconds file.json | --test-cycle seconds file.json]");
                bool created;
                using (Mutex singleton = new Mutex(true, "Local\\DS4SleepLight_054C_09CC", out created)) {
                    if (!created) return 0;
                    Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                    using (EventWaitHandle stop = new EventWaitHandle(false, EventResetMode.AutoReset, StopEvent))
                    using (LightApp app = new LightApp()) {
                        RegisteredWaitHandle wait = ThreadPool.RegisterWaitForSingleObject(stop, delegate(object state, bool timedOut) { try { app.BeginInvoke(new Action(app.Close)); } catch (InvalidOperationException) { } }, null, -1, true);
                        try { Application.Run(app); } finally { wait.Unregister(null); }
                    }
                }
                return 0;
            } catch (Exception error) {
                Log.Write("fatal_" + error.GetType().Name, null);
                MessageBox.Show(Ui.Text("手柄灭灯工具无法完成操作：", "Controller light operation failed: ") + error.Message, "ControllerLight"); return 1;
            }
        }
    }
}
