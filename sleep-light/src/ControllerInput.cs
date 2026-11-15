using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace DS4SleepLight {
    internal sealed class InputSnapshot {
        internal bool Connected;
        internal long Generation, Reports, Activity;
        internal double LastReportMs;
        internal string Status = "waiting";
        internal string Controller = "none";
    }
    internal sealed class ControllerInput : IDisposable {
        private readonly object gate = new object();
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private readonly Thread worker;
        private volatile bool enabled, stopping;
        private int epoch;
        private readonly InputSnapshot state = new InputSnapshot();
        internal static double Now { get { return Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency; } }
        internal ControllerInput() { worker = new Thread(Run); worker.IsBackground = true; worker.Name = "Controller input observer"; worker.Start(); }
        internal void Enable(bool value) {
            lock (gate) {
                if (enabled == value) return;
                enabled = value; Interlocked.Increment(ref epoch); state.Connected = false;
            }
            wake.Set();
        }
        internal InputSnapshot Snapshot() {
            lock (gate) { return new InputSnapshot { Connected = state.Connected, Generation = state.Generation, Reports = state.Reports, Activity = state.Activity, LastReportMs = state.LastReportMs, Status = state.Status, Controller = state.Controller }; }
        }
        private void SetStatus(string status) {
            lock (gate) {
                state.Connected = false;
                if (state.Status != status) { state.Status = status; Log.Event("input_status", "status=" + status); }
            }
        }
        private void Run() {
            try {
                while (!stopping) {
                    if (!enabled) { wake.WaitOne(200); continue; }
                    int session = epoch;
                    try {
                        using (NativeHid.InputStream stream = NativeHid.OpenInput()) {
                            ControllerActivity detector = new ControllerActivity(stream.Kind, SleepLightConfig.StickDeadzone, SleepLightConfig.TriggerThreshold);
                            bool first = true;
                            while (enabled && !stopping && epoch == session) {
                                int length;
                                if (!stream.Read(delegate { return stopping || !enabled || epoch != session; }, out length)) break;
                                bool activity;
                                if (!detector.Observe(stream.Buffer, length, out activity)) { SetStatus("unsupported_input_report"); break; }
                                lock (gate) {
                                    if (stopping || !enabled || epoch != session) break;
                                    if (first) { state.Generation++; first = false; state.Controller = stream.Kind.ToString(); Log.Event("input_connected_no_light_write", "controller=" + stream.Kind + " shared_read=true input_bytes=64"); }
                                    state.Connected = true; state.Status = "connected"; state.Reports++; state.LastReportMs = Now;
                                    if (activity) state.Activity++;
                                }
                            }
                        }
                        SetStatus(enabled ? "stream_ended" : "paused");
                    } catch (IOException error) { SetStatus(error.Message); }
                    catch (System.ComponentModel.Win32Exception) { SetStatus("input_access_failed"); }
                    if (enabled && !stopping && epoch == session) wake.WaitOne(3000);
                }
            } catch (Exception error) { SetStatus("reader_failed_" + error.GetType().Name); }
            finally { Log.Event("input_reader_stopped_no_light_write", ""); }
        }
        public void Dispose() {
            stopping = true; Interlocked.Increment(ref epoch); wake.Set();
            // Never release resources underneath pending IO; the worker owns its stream.
            // IO 由读取线程取消和释放，不能在 UI 线程提前释放缓冲区。
            if (worker.Join(1000)) wake.Dispose();
        }
    }
    internal static partial class NativeHid {
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadFile(SafeFileHandle file, IntPtr buffer, uint count, out uint read, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ResetEvent(SafeWaitHandle handle);
        internal static InputStream OpenInput() {
            List<Target> paths = Paths();
            if (paths.Count != 1) throw new IOException(paths.Count == 0 ? "no_supported_input_device" : "multiple_input_devices_refused");
            SafeFileHandle file = Open(paths[0].Path, 0x80000000);
            if (file.IsInvalid) { file.Dispose(); throw new IOException("shared_input_unavailable"); }
            return new InputStream(file, paths[0].Kind);
        }
        internal sealed class InputStream : IDisposable {
            internal readonly byte[] Buffer = new byte[64];
            internal readonly ControllerKind Kind;
            private readonly SafeFileHandle file;
            private readonly SafeWaitHandle completion;
            private GCHandle pinned;
            private IntPtr ov;
            internal InputStream(SafeFileHandle handle, ControllerKind kind) {
                Kind = kind; file = handle; completion = CreateEvent(IntPtr.Zero, true, false, null);
                if (completion.IsInvalid) { file.Dispose(); completion.Dispose(); throw new IOException("input_event_unavailable"); }
                try { pinned = GCHandle.Alloc(Buffer, GCHandleType.Pinned); ov = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Overlapped))); }
                catch { Dispose(); throw; }
            }
            internal bool Read(Func<bool> canceled, out int length) {
                length = 0; bool pending = false;
                try {
                    if (!ResetEvent(completion)) return false;
                    Overlapped data = new Overlapped(); data.Event = completion.DangerousGetHandle(); Marshal.StructureToPtr(data, ov, false);
                    uint read;
                    if (!ReadFile(file, pinned.AddrOfPinnedObject(), 64, out read, ov)) {
                        if (Marshal.GetLastWin32Error() != 997) return false;
                        pending = true; double deadline = ControllerInput.Now + 2000;
                        while (true) {
                            uint wait = WaitForSingleObject(completion, 100);
                            if (wait == 0) break;
                            if (wait != 258 || canceled() || ControllerInput.Now >= deadline) return false;
                        }
                        bool done = GetOverlappedResult(file, ov, out read, false);
                        if (!done) { pending = Marshal.GetLastWin32Error() == 996; return false; }
                        pending = false;
                    }
                    if (canceled()) return false;
                    length = (int)read; return true;
                } finally {
                    if (pending) { uint ignored; CancelIoEx(file, ov); GetOverlappedResult(file, ov, out ignored, true); }
                }
            }
            public void Dispose() { if (ov != IntPtr.Zero) { Marshal.FreeHGlobal(ov); ov = IntPtr.Zero; } if (pinned.IsAllocated) pinned.Free(); completion.Dispose(); file.Dispose(); }
        }
    }
}
