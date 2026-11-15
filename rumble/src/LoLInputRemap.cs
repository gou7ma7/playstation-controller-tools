using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LoLDamageRumble
{
    internal enum CtrlAction { None, Down, Up }
    internal struct KeyDecision
    {
        internal bool Swallow;
        internal CtrlAction Action;
        internal KeyDecision(bool swallow, CtrlAction action) { Swallow = swallow; Action = action; }
    }
    // Physical Space and right Ctrl ownership only; no text or key history.
    internal sealed class SpaceCtrlPolicy
    {
        internal bool SpaceDown, SuppressedSpace, Active, PhysicalCtrl, OwnCtrl;
        internal CtrlAction Release()
        {
            Active = false;
            bool release = OwnCtrl && !PhysicalCtrl;
            OwnCtrl = false;
            return release ? CtrlAction.Up : CtrlAction.None;
        }
        internal KeyDecision Key(int vk, bool down, bool injected, bool game)
        {
            if (injected) return new KeyDecision(false, CtrlAction.None);
            if (vk == 0xA3)
            {
                PhysicalCtrl = down;
                if (!down && Active && game)
                { OwnCtrl = true; return new KeyDecision(true, CtrlAction.None); }
                return new KeyDecision(false, CtrlAction.None);
            }
            if (vk != 0x20) return new KeyDecision(false, CtrlAction.None);
            if (down)
            {
                if (SpaceDown) return new KeyDecision(SuppressedSpace, CtrlAction.None);
                SpaceDown = true; SuppressedSpace = game; Active = game;
                if (game && !PhysicalCtrl) { OwnCtrl = true; return new KeyDecision(true, CtrlAction.Down); }
                return new KeyDecision(game, CtrlAction.None);
            }
            SpaceDown = false;
            bool swallow = SuppressedSpace; SuppressedSpace = false;
            return new KeyDecision(swallow, Release());
        }
        internal void InjectionFailed(CtrlAction action)
        { if (action == CtrlAction.Down) { OwnCtrl = false; Active = false; } else if (action == CtrlAction.Up) OwnCtrl = true; }
    }

    internal static class InputNative
    {
        internal static readonly UIntPtr Marker = new UIntPtr(0x4C534352);
        [StructLayout(LayoutKind.Sequential)] internal struct Keyboard
        { internal ushort Key, Scan; internal uint Flags, Time; internal UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct Mouse
        { internal int X, Y; internal uint Data, Flags, Time; internal UIntPtr Extra; }
        [StructLayout(LayoutKind.Explicit)] internal struct Data
        { [FieldOffset(0)] internal Keyboard Keyboard; [FieldOffset(0)] internal Mouse Mouse; }
        [StructLayout(LayoutKind.Sequential)] internal struct Input { internal uint Type; internal Data Data; }
        [StructLayout(LayoutKind.Sequential)] internal struct KeyData
        { internal uint Key, Scan, Flags, Time; internal UIntPtr Extra; }
        internal delegate IntPtr Hook(int code, IntPtr message, IntPtr data);
        [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int id, Hook callback, IntPtr module, uint thread);
        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] input, int size);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
        internal static uint ForegroundPid() { uint pid; GetWindowThreadProcessId(GetForegroundWindow(), out pid); return pid; }
        internal static Input Ctrl(CtrlAction action)
        { return new Input { Type = 1, Data = new Data { Keyboard = new Keyboard {
            Key = 0xA3, Scan = 0x1D, Flags = action == CtrlAction.Up ? 3u : 1u, Extra = Marker } } }; }
    }

    internal sealed class InputWorker : Form
    {
        readonly SpaceCtrlPolicy policy = new SpaceCtrlPolicy();
        readonly InputNative.Hook callback;
        readonly Timer timer = new Timer { Interval = 20 };
        readonly string root = AppDomain.CurrentDomain.BaseDirectory;
        IntPtr hook;
        uint cachedPid;
        int cacheTick, nameTick, mappedPresses, releases, failures;
        bool game, closing;
        DateTime nextCheck;
        string error = "";
        internal InputWorker()
        {
            ShowInTaskbar = false; var unused = Handle;
            nextCheck = DateTime.UtcNow.AddSeconds(2);
            if (!HostAlive(false)) throw new InvalidOperationException("No enabled LoL host; input worker will not install a hook.");
            callback = OnKey;
            policy.PhysicalCtrl = (InputNative.GetAsyncKeyState(0xA3) & 0x8000) != 0;
            RefreshFocus();
            hook = InputNative.SetWindowsHookEx(13, callback, InputNative.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            timer.Tick += delegate {
                RefreshFocus();
                if (!game || (!policy.Active && policy.OwnCtrl)) Send(policy.Release());
                if (DateTime.UtcNow >= nextCheck)
                {
                    nextCheck = DateTime.UtcNow.AddSeconds(1);
                    if (!HostAlive()) { Close(); return; }
                    Status(true);
                }
            };
            SystemEvents.SessionSwitch += Session;
            SystemEvents.PowerModeChanged += Power;
            timer.Start(); Status(true);
        }
        protected override void SetVisibleCore(bool value) { base.SetVisibleCore(false); }
        void RefreshFocus()
        {
            uint pid = InputNative.ForegroundPid();
            if (pid != cachedPid || unchecked(Environment.TickCount - nameTick) >= 1000)
            {
                game = false;
                try { using (var p = Process.GetProcessById((int)pid))
                    game = string.Equals(p.ProcessName, "League of Legends", StringComparison.OrdinalIgnoreCase); }
                catch (ArgumentException) { } catch (System.ComponentModel.Win32Exception) { }
                cachedPid = pid; nameTick = Environment.TickCount;
            }
            cacheTick = Environment.TickCount;
        }
        IntPtr OnKey(int code, IntPtr message, IntPtr data)
        {
            if (code < 0) return InputNative.CallNextHookEx(hook, code, message, data);
            int kind = message.ToInt32();
            if (kind != 0x100 && kind != 0x101 && kind != 0x104 && kind != 0x105)
                return InputNative.CallNextHookEx(hook, code, message, data);
            var key = (InputNative.KeyData)Marshal.PtrToStructure(data, typeof(InputNative.KeyData));
            if (key.Key != 0x20 && key.Key != 0xA3)
                return InputNative.CallNextHookEx(hook, code, message, data);
            bool injected = (key.Flags & 0x10) != 0 || key.Extra == InputNative.Marker;
            bool scoped = game && InputNative.ForegroundPid() == cachedPid
                && unchecked(Environment.TickCount - cacheTick) >= 0 && unchecked(Environment.TickCount - cacheTick) < 250;
            if (!scoped) Send(policy.Release());
            var decision = policy.Key((int)key.Key, kind == 0x100 || kind == 0x104, injected, scoped);
            Send(decision.Action);
            return decision.Swallow ? new IntPtr(1) : InputNative.CallNextHookEx(hook, code, message, data);
        }
        void Send(CtrlAction action)
        {
            if (action == CtrlAction.None) return;
            if (InputNative.SendInput(1, new[] { InputNative.Ctrl(action) }, Marshal.SizeOf(typeof(InputNative.Input))) == 1)
            { if (action == CtrlAction.Down) mappedPresses++; else releases++; }
            else { failures++; error = "Right Ctrl injection failed; check input worker permissions."; policy.InjectionFailed(action); }
        }
        bool HostAlive(bool checkFreshness = true)
        {
            try
            {
                string text;
                using (var file = new FileStream(Path.Combine(root, "input-host.json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(file)) text = reader.ReadToEnd();
                var host = new JavaScriptSerializer().DeserializeObject(text) as System.Collections.Generic.Dictionary<string, object>;
                if (host == null || !Convert.ToBoolean(host["enabled"])) return false;
                DateTime tick = DateTime.Parse(Convert.ToString(host["utc"]), null, System.Globalization.DateTimeStyles.RoundtripKind);
                if (checkFreshness && (DateTime.UtcNow - tick.ToUniversalTime()).TotalSeconds > 4) return false;
                using (var p = Process.GetProcessById(Convert.ToInt32(host["pid"])))
                    return p.Id != Process.GetCurrentProcess().Id && string.Equals(p.MainModule.FileName, Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        void Status(bool running)
        {
            try { File.WriteAllText(Path.Combine(root, "input-status.json"), new JavaScriptSerializer().Serialize(new {
                running = running, processId = Process.GetCurrentProcess().Id, gameForeground = game,
                spaceHeld = policy.SpaceDown, ownRightCtrl = policy.OwnCtrl, mappedPresses = mappedPresses,
                releases = releases, failures = failures, error = error, utc = DateTime.UtcNow.ToString("o") })); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        void Session(object sender, SessionSwitchEventArgs e)
        { if (e.Reason == SessionSwitchReason.SessionLock || e.Reason == SessionSwitchReason.SessionLogoff) PostRelease(); }
        void Power(object sender, PowerModeChangedEventArgs e)
        { if (e.Mode == PowerModes.Suspend) PostRelease(); }
        void PostRelease() { if (!closing) try { BeginInvoke((Action)delegate { game = false; Send(policy.Release()); }); } catch (InvalidOperationException) { } }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            closing = true; timer.Stop();
            if (hook != IntPtr.Zero) { InputNative.UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
            Send(policy.Release()); Status(false);
            SystemEvents.SessionSwitch -= Session; SystemEvents.PowerModeChanged -= Power;
            timer.Dispose(); base.OnFormClosed(e);
        }
        internal static void Run()
        {
            bool first;
            using (var mutex = new System.Threading.Mutex(true, @"Local\LoLInputWorker_v1", out first))
            {
                if (!first) return;
                try { Application.EnableVisualStyles(); using (var worker = new InputWorker()) Application.Run(worker); }
                catch (Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "input-error.log"), ex.ToString()); }
            }
        }
    }

    // The ordinary tray/HID process never installs a keyboard hook.
    internal sealed class InputRemapHost : IDisposable
    {
        internal const string TaskName = "PlayStationControllerTools-LoLInput";
        readonly string root = AppDomain.CurrentDomain.BaseDirectory;
        DateTime nextWrite;
        string configurationError = "", startupError = "", writeError = "";
        internal bool Enabled { get; private set; }
        internal string LastError { get { return (configurationError + " " + startupError + " " + writeError).Trim(); } }
        internal InputRemapHost()
        {
            try {
                string config = Path.Combine(root, "input-remap.json");
                if (File.Exists(config)) {
                    var data = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(config)) as System.Collections.Generic.Dictionary<string, object>;
                    if (data == null || data.Count != 1 || !data.ContainsKey("enabled") || !(data["enabled"] is bool)) throw new ArgumentException("input-remap.json expects only enabled: boolean.");
                    Enabled = (bool)data["enabled"];
                }
            } catch (Exception ex) { Enabled = false; configurationError = ex.Message; }
            Write(); if (Enabled) Start();
        }
        internal void SetEnabled(bool enabled)
        { Enabled = enabled; Write(); if (enabled) Start(); }
        void Start()
        {
            try
            {
                var query = new ProcessStartInfo("schtasks.exe", "/Query /TN \"" + TaskName + "\"") {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                bool installed;
                using (var p = Process.Start(query)) { p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd(); installed = p.WaitForExit(3000) && p.ExitCode == 0; }
                if (installed) using (var p = Process.Start(new ProcessStartInfo("schtasks.exe", "/Run /TN \"" + TaskName + "\"") { UseShellExecute = false, CreateNoWindow = true })) { }
                else using (var p = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--input-worker") { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden })) { }
                startupError = ""; Write();
            }
            catch (Exception ex) { startupError = ex.Message; }
        }
        void Write()
        {
            try {
                string file = Path.Combine(root, "input-host.json"), temp = file + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(new {
                    enabled = Enabled, pid = Process.GetCurrentProcess().Id, utc = DateTime.UtcNow.ToString("o") }));
                if (File.Exists(file)) File.Replace(temp, file, null); else File.Move(temp, file);
                writeError = "";
            }
            catch (IOException ex) { writeError = ex.Message; } catch (UnauthorizedAccessException ex) { writeError = ex.Message; }
            nextWrite = DateTime.UtcNow.AddSeconds(1);
        }
        internal void Tick() { if (DateTime.UtcNow >= nextWrite) Write(); }
        public void Dispose() { Enabled = false; Write(); }
    }
}
