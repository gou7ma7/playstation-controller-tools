using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DS4SleepLight {
    internal sealed class WriteResult {
        internal string Outcome; internal int Matches, Writes, Win32Error;
        internal string Controller = "none";
        internal bool Success { get { return Outcome == "written"; } }
        internal WriteResult(string outcome, int matches, int writes, int error) { Outcome = outcome; Matches = matches; Writes = writes; Win32Error = error; }
    }
    internal static partial class NativeHid {
        private sealed class Target { internal string Path; internal ControllerKind Kind; }
        [StructLayout(LayoutKind.Sequential)] private struct InterfaceData { internal int Size; internal Guid ClassGuid; internal int Flags; internal IntPtr Reserved; }
        [StructLayout(LayoutKind.Sequential)] private struct Attributes { internal int Size; internal ushort Vendor, Product, Version; }
        [StructLayout(LayoutKind.Sequential)] private struct Caps {
            internal ushort Usage, UsagePage, InputLength, OutputLength, FeatureLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] internal ushort[] Reserved;
            internal ushort LinkNodes, InputButtons, InputValues, InputIndices, OutputButtons, OutputValues, OutputIndices, FeatureButtons, FeatureValues, FeatureIndices;
        }
        [StructLayout(LayoutKind.Sequential)] private struct Overlapped { internal UIntPtr Internal, InternalHigh; internal uint Offset, OffsetHigh; internal IntPtr Event; }
        [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
        [DllImport("hid.dll")] private static extern bool HidD_GetAttributes(SafeFileHandle handle, ref Attributes attributes);
        [DllImport("hid.dll")] private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
        [DllImport("hid.dll")] private static extern bool HidD_FreePreparsedData(IntPtr data);
        [DllImport("hid.dll")] private static extern int HidP_GetCaps(IntPtr data, out Caps caps);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string enumerator, IntPtr parent, uint flags);
        [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref InterfaceData data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint length, out uint required, IntPtr device);
        [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool WriteFile(SafeFileHandle file, IntPtr buffer, uint count, out uint written, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetOverlappedResult(SafeFileHandle file, IntPtr overlapped, out uint count, bool wait);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CancelIoEx(SafeFileHandle file, IntPtr overlapped);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeWaitHandle CreateEvent(IntPtr security, bool manual, bool initial, string name);
        [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(SafeWaitHandle handle, uint timeout);

        private static List<Target> Paths() {
            Guid guid; HidD_GetHidGuid(out guid);
            IntPtr set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 0x12);
            if (set == new IntPtr(-1)) throw new IOException("HID enumeration failed.");
            List<Target> paths = new List<Target>();
            try {
                for (uint index = 0; ; index++) {
                    InterfaceData item = new InterfaceData(); item.Size = Marshal.SizeOf(typeof(InterfaceData));
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, index, ref item)) {
                        if (Marshal.GetLastWin32Error() != 259) throw new IOException("HID enumeration incomplete.");
                        break;
                    }
                    uint required; SetupDiGetDeviceInterfaceDetail(set, ref item, IntPtr.Zero, 0, out required, IntPtr.Zero);
                    if (required < 8) continue;
                    IntPtr detail = Marshal.AllocHGlobal((int)required);
                    try {
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        if (!SetupDiGetDeviceInterfaceDetail(set, ref item, detail, required, out required, IntPtr.Zero)) continue;
                        string path = Marshal.PtrToStringUni(IntPtr.Add(detail, 4));
                        if (path == null || (path.IndexOf("vid_054c&pid_09cc", StringComparison.OrdinalIgnoreCase) < 0 && path.IndexOf("vid_054c&pid_0ce6", StringComparison.OrdinalIgnoreCase) < 0)) continue;
                        using (SafeFileHandle file = Open(path, 0)) {
                            if (file.IsInvalid) throw new IOException("Target capabilities inaccessible; enumeration refused.");
                            Attributes attributes = new Attributes(); attributes.Size = Marshal.SizeOf(typeof(Attributes));
                            IntPtr prep;
                            if (!HidD_GetAttributes(file, ref attributes) || !HidD_GetPreparsedData(file, out prep)) throw new IOException("Target capabilities incomplete; enumeration refused.");
                            try {
                                Caps caps;
                                if (HidP_GetCaps(prep, out caps) != 0x110000) throw new IOException("Target capabilities invalid; enumeration refused.");
                                ControllerKind kind;
                                if (LightReport.TryKind(attributes.Vendor, attributes.Product, caps.UsagePage, caps.Usage, caps.InputLength, caps.OutputLength, out kind)) paths.Add(new Target { Path = path, Kind = kind });
                            } finally { HidD_FreePreparsedData(prep); }
                        }
                    } finally { Marshal.FreeHGlobal(detail); }
                }
            } finally { SetupDiDestroyDeviceInfoList(set); }
            return paths;
        }
        private static SafeFileHandle Open(string path, uint access) { return CreateFile(path, access, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero); }
        internal static WriteResult Diagnose() { return RecordedExecute(0, 0, 0, 0, 0); }
        internal static WriteResult Send(int red, int green, int blue, int attempts, int repeatDelayMs) { return RecordedExecute(red, green, blue, attempts, repeatDelayMs); }
        private static WriteResult RecordedExecute(int red, int green, int blue, int attempts, int repeatDelayMs) {
            Stopwatch duration = Stopwatch.StartNew();
            string operation = attempts == 0 ? "diagnose" : "light";
            string rgb = attempts == 0 ? "none" : red + "," + green + "," + blue;
            Log.Event("usb_operation_begin", "operation=" + operation + " attempts=" + attempts + " gap_ms=" + repeatDelayMs + " rgb=" + rgb);
            WriteResult result = null;
            try { result = Execute(red, green, blue, attempts, repeatDelayMs); return result; }
            finally { Log.Write("usb_operation_completed", result, "operation=" + operation + " duration_ms=" + duration.ElapsedMilliseconds); }
        }
        internal static WriteResult SendQuiet(int red, int green, int blue) { return Execute(red, green, blue, 1, 0, false); }
        private static WriteResult Execute(int red, int green, int blue, int attempts, int repeatDelayMs, bool verbose = true) {
            List<Target> paths;
            Stopwatch enumeration = Stopwatch.StartNew();
            if (verbose) Log.Event("usb_enumeration_begin", "");
            try { paths = Paths(); } catch (IOException) { return new WriteResult("enumeration_failed", 0, 0, Marshal.GetLastWin32Error()); }
            if (verbose) Log.Event("usb_enumeration_completed", "matches=" + paths.Count + " duration_ms=" + enumeration.ElapsedMilliseconds);
            if (paths.Count != 1) return new WriteResult(paths.Count == 0 ? "no_supported_device" : "multiple_devices_refused", paths.Count, 0, 0);
            Target target = paths[0];
            using (SafeFileHandle file = Open(target.Path, 0x40000000)) {
                if (file.IsInvalid) return new WriteResult("output_unavailable", 1, 0, Marshal.GetLastWin32Error()) { Controller = target.Kind.ToString() };
                if (attempts == 0) return new WriteResult("output_accessible_no_write", 1, 0, 0) { Controller = target.Kind.ToString() };
                byte[] report = LightReport.Make(target.Kind, red, green, blue);
                if (verbose) Log.Event("usb_light_target", "controller=" + target.Kind + " output_bytes=" + report.Length);
                int writes = 0, error = 0;
                for (int i = 0; i < attempts; i++) {
                    if (i > 0) System.Threading.Thread.Sleep(repeatDelayMs);
                    if (verbose) Log.Event("usb_write_attempt_begin", "attempt=" + (i + 1));
                    Stopwatch attemptDuration = Stopwatch.StartNew();
                    bool completed = Write(file, report, out error);
                    if (verbose) Log.Event("usb_write_attempt_completed", "attempt=" + (i + 1) + " success=" + completed + " win32=" + error + " duration_ms=" + attemptDuration.ElapsedMilliseconds);
                    if (completed) writes++;
                    else break;
                }
                return new WriteResult(writes == attempts ? "written" : "write_failed", 1, writes, error) { Controller = target.Kind.ToString() };
            }
        }
        private static bool Write(SafeFileHandle file, byte[] report, out int error) {
            error = 0;
            using (SafeWaitHandle completion = CreateEvent(IntPtr.Zero, true, false, null)) {
                if (completion.IsInvalid) { error = Marshal.GetLastWin32Error(); return false; }
                GCHandle buffer = GCHandle.Alloc(report, GCHandleType.Pinned);
                IntPtr ov = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Overlapped)));
                bool pending = false;
                try {
                    Overlapped data = new Overlapped(); data.Event = completion.DangerousGetHandle();
                    Marshal.StructureToPtr(data, ov, false);
                    uint written;
                    if (!WriteFile(file, buffer.AddrOfPinnedObject(), (uint)report.Length, out written, ov)) {
                        error = Marshal.GetLastWin32Error();
                        if (error != 997) return false;
                        pending = true;
                        if (WaitForSingleObject(completion, (uint)SleepLightConfig.UsbWriteTimeoutMs) != 0) { error = 1460; return false; }
                        bool completed = GetOverlappedResult(file, ov, out written, false);
                        if (!completed) { error = Marshal.GetLastWin32Error(); pending = error == 996; return false; }
                        pending = false;
                    }
                    if (written != report.Length) { error = 29; return false; }
                    error = 0; return true;
                } finally {
                    // Keep the unmanaged OVERLAPPED and pinned buffer alive until canceled IO completes.
                    // 取消后等待 IO 完成，才释放 OVERLAPPED 与缓冲区。
                    if (pending) {
                        uint ignored;
                        Log.Event("usb_cancel_wait_begin", "write_win32=" + error);
                        Stopwatch cancellation = Stopwatch.StartNew();
                        CancelIoEx(file, ov);
                        bool completed = GetOverlappedResult(file, ov, out ignored, true);
                        int cancelError = completed ? 0 : Marshal.GetLastWin32Error();
                        Log.Event("usb_cancel_wait_completed", "completed=" + completed + " win32=" + cancelError + " duration_ms=" + cancellation.ElapsedMilliseconds);
                    }
                    Marshal.FreeHGlobal(ov); buffer.Free();
                }
            }
        }
    }
}
