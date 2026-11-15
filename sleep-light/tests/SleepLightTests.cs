using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace DS4SleepLight {
    internal static class Tests {
        private static readonly List<string> checks = new List<string>();
        private static void Check(string name, bool condition) { if (!condition) throw new Exception(name); checks.Add(name); }
        private static void Case(string name, Action body) { body(); checks.Add(name); }
        private static byte[] NeutralReport() { byte[] report = new byte[64]; report[0] = 1; report[1] = report[2] = report[3] = report[4] = 128; report[5] = 8; return report; }
        private static byte[] NeutralDualSenseReport() { byte[] report = NeutralReport(); report[5] = 0; report[8] = 8; return report; }
        private static bool Activity(ControllerActivity detector, byte[] report) { bool activity; if (!detector.Observe(report, report.Length, out activity)) throw new Exception("Valid report rejected"); return activity; }
        internal static int Main(string[] args) {
            try {
                SleepLightConfig.Validate();
                Check("configuration valid", true);
                byte[] off = LightReport.Make(0, 0, 0), color = LightReport.Make(7, 83, 255);
                Check("USB report length", off.Length == 32);
                Check("USB report id", off[0] == 5);
                Check("only color and blink flags", off[1] == 6);
                Check("motor flag disabled", (off[1] & 1) == 0);
                Check("motor bytes unused", off[4] == 0 && off[5] == 0 && color[4] == 0 && color[5] == 0);
                Check("off RGB zero", off[6] == 0 && off[7] == 0 && off[8] == 0);
                Check("RGB positions", color[6] == 7 && color[7] == 83 && color[8] == 255);
                Check("blink cleared", color[9] == 0 && color[10] == 0);
                for (int i = 2; i < 32; i++) if (i < 6 || i > 8) Check("unrelated byte " + i + " untouched", color[i] == 0);
                Case("invalid RGB rejected", delegate { bool rejected = false; try { LightReport.Make(0, -1, 0); } catch (ArgumentOutOfRangeException) { rejected = true; } Check("negative channel rejected", rejected); });
                Case("large RGB rejected", delegate { bool rejected = false; try { LightReport.Make(256, 0, 0); } catch (ArgumentOutOfRangeException) { rejected = true; } Check("large channel rejected", rejected); });
                Check("USB v2 accepted", LightReport.Supported(0x054c, 0x09cc, 1, 5, 32));
                Check("DS4 v1 rejected", !LightReport.Supported(0x054c, 0x05c4, 1, 5, 32));
                Check("DualSense cannot use DS4 output", !LightReport.Supported(0x054c, 0x0ce6, 1, 5, 32));
                ControllerKind selected;
                Check("USB DualSense selects its own protocol", LightReport.TryKind(0x054c, 0x0ce6, 1, 5, 64, 48, out selected) && selected == ControllerKind.DualSense);
                Check("USB DS4 keeps its original protocol", LightReport.TryKind(0x054c, 0x09cc, 1, 5, 64, 32, out selected) && selected == ControllerKind.DualShock4V2);
                Check("DualSense Bluetooth refused", !LightReport.TryKind(0x054c, 0x0ce6, 1, 5, 78, 78, out selected));
                Check("DualSense short input capability refused", !LightReport.TryKind(0x054c, 0x0ce6, 1, 5, 10, 48, out selected));
                Check("unknown Sony controller refused", !LightReport.Supported(0x054c, 0x0df2, 1, 5, 48));
                Check("other vendor refused", !LightReport.Supported(0x1234, 0x0ce6, 1, 5, 48));
                Check("DualSense non-gamepad collection refused", !LightReport.Supported(0x054c, 0x0ce6, 1, 2, 48));
                byte[] dsOff = LightReport.Make(ControllerKind.DualSense, 0, 0, 0), dsColor = LightReport.Make(ControllerKind.DualSense, 7, 83, 255);
                Check("DualSense Windows USB report length and id", dsOff.Length == 48 && dsOff[0] == 2 && dsColor.Length == 48);
                Check("DualSense only light enable flags", dsColor[1] == 0 && dsColor[2] == (SleepLightConfig.ControlDualSensePlayerLeds ? 0x14 : 0x04) && dsColor[39] == 0);
                Check("DualSense RGB layout", dsColor[45] == 7 && dsColor[46] == 83 && dsColor[47] == 255);
                Check("DualSense RGB and player lights off", dsOff[44] == 0 && dsOff[45] == 0 && dsOff[46] == 0 && dsOff[47] == 0);
                Check("DualSense player lights use configured mask", dsColor[44] == (SleepLightConfig.ControlDualSensePlayerLeds ? SleepLightConfig.DualSensePlayerLeds : 0));
                bool unrelatedClear = true;
                for (int i = 3; i < 44; i++) unrelatedClear &= dsColor[i] == 0 && dsOff[i] == 0;
                Check("DualSense motors triggers audio mute and LED animations untouched", unrelatedClear);
                Check("Bluetooth report rejected", !LightReport.Supported(0x054c, 0x09cc, 1, 5, 78));
                Check("non-controller interface rejected", !LightReport.Supported(0x054c, 0x09cc, 1, 2, 32));
                SleepPolicy p = new SleepPolicy(true);
                Check("startup does not schedule a light write", p.Phase == LightPhase.Ready && !p.UserResume() && !p.Unlock());
                Check("first suspend requests off", p.Suspend());
                Check("duplicate suspend suppressed", !p.Suspend());
                p.AutomaticResume(); Check("automatic resume keeps lights off", p.Phase == LightPhase.WaitingForUser);
                Check("S3 automatic wake to S4 requests off again", p.Suspend());
                Check("S3 to S4 never restores", p.Phase == LightPhase.Suspended);
                p.AutomaticResume(); p.Lock();
                Check("locked user resume defers restoration", !p.UserResume() && p.Phase == LightPhase.WaitingForUnlock);
                Check("unlock schedules restoration", p.Unlock() && p.Phase == LightPhase.RestorePending);
                Check("duplicate resume does not restart delay", !p.UserResume());
                Check("duplicate unlock does not restart delay", !p.Unlock());
                p.Lock(); Check("lock cancels pending restoration", p.Phase == LightPhase.WaitingForUnlock);
                Check("later unlock reschedules restoration", p.Unlock());
                Check("another suspend cancels pending restore", p.Suspend() && p.Phase == LightPhase.Suspended);
                p.CompleteRestore(); Check("stale completion cannot clear suspend", p.Phase == LightPhase.Suspended);
                Check("direct interactive resume supported", p.UserResume());
                p.CompleteRestore(); Check("successful restore returns to ready", p.Phase == LightPhase.Ready);
                p.Lock(); Check("normal lock/unlock does not relight", !p.Unlock() && p.Phase == LightPhase.Ready);
                p.Suspend(); p.AutomaticResume(); Check("unlock-only resume supported", p.Unlock());
                SleepPolicy noRestore = new SleepPolicy(false); noRestore.Suspend(); noRestore.AutomaticResume();
                Check("restore disabled sends no color", !noRestore.UserResume() && noRestore.Phase == LightPhase.Ready);
                ControllerActivity detector = new ControllerActivity(32, 12);
                byte[] report = NeutralReport();
                Check("first neutral input is only a baseline", !Activity(detector, report));
                report[7] = 0xfc; report[10] = 91; report[13] = 255; report[20] = 7; report[30] = 1; report[35] = 49;
                Check("counter sensors battery and touch motion ignored", !Activity(detector, report));
                report[1] = 145; report[2] = 102; report[8] = 5; report[6] = 4;
                Check("stick drift and tiny digital trigger noise ignored", !Activity(detector, report));
                report[1] = 160; Check("deadzone boundary stays idle", !Activity(detector, report));
                report[1] = 161; Check("stick movement past deadzone is activity", Activity(detector, report));
                Check("held stick prevents false idle", Activity(detector, report));
                report = NeutralReport(); Check("stick release resets inactivity clock", Activity(detector, report));
                Check("neutral stream does not count as activity", !Activity(detector, report));
                report[9] = 12; Check("right trigger threshold is activity", Activity(detector, report));
                report = NeutralReport(); Activity(detector, report);
                report[8] = 12; Check("left trigger threshold is activity", Activity(detector, report));
                report = NeutralReport(); Activity(detector, report);
                report[6] = 1; Check("shoulder press is activity", Activity(detector, report));
                Check("held button remains activity", Activity(detector, report));
                report[6] = 0; Check("button release is activity", Activity(detector, report));
                report[5] = 0; Check("dpad direction is activity", Activity(detector, report));
                report = NeutralReport(); Activity(detector, report); report[7] = 2;
                Check("touchpad click is activity", Activity(detector, report));
                report[7] = 1; Check("PS button is activity", Activity(detector, report));
                report = NeutralReport(); Activity(detector, report); report[5] |= 0x20;
                Check("face button is activity", Activity(detector, report));
                bool observed; report = NeutralReport(); report[0] = 0x11;
                Check("Bluetooth input format refused", !detector.Observe(report, 64, out observed));
                report = NeutralReport(); Check("short input refused", !detector.Observe(report, 10, out observed));
                report[5] = 15; Check("invalid dpad refused", !detector.Observe(report, 64, out observed));
                ControllerActivity dsDetector = new ControllerActivity(ControllerKind.DualSense, 32, 12);
                byte[] dsInput = NeutralDualSenseReport();
                Check("DualSense first input is a baseline", !Activity(dsDetector, dsInput));
                dsInput[7] = 255; dsInput[10] = 0xf8; dsInput[11] = 255; dsInput[16] = 192; dsInput[33] = 59; dsInput[53] = 11;
                Check("DualSense sequence gyro touch battery and reserved bits ignored", !Activity(dsDetector, dsInput));
                dsInput[5] = 11; dsInput[6] = 11; dsInput[9] = 12; dsInput[1] = 160;
                Check("DualSense trigger noise and deadzone boundary ignored", !Activity(dsDetector, dsInput));
                dsInput[5] = 12; Check("DualSense L2 analog location recognized", Activity(dsDetector, dsInput));
                Check("DualSense held trigger stays active", Activity(dsDetector, dsInput));
                dsInput = NeutralDualSenseReport(); Check("DualSense trigger release counts", Activity(dsDetector, dsInput));
                dsInput[6] = 12; Check("DualSense R2 analog location recognized", Activity(dsDetector, dsInput));
                dsInput = NeutralDualSenseReport(); Activity(dsDetector, dsInput); dsInput[9] = 1;
                Check("DualSense shoulder recognized", Activity(dsDetector, dsInput));
                Check("DualSense held button stays active", Activity(dsDetector, dsInput));
                dsInput[9] = 0; Check("DualSense button release counts", Activity(dsDetector, dsInput));
                dsInput[8] = 0; Check("DualSense dpad location recognized", Activity(dsDetector, dsInput));
                dsInput = NeutralDualSenseReport(); Activity(dsDetector, dsInput); dsInput[8] |= 0x20;
                Check("DualSense face button recognized", Activity(dsDetector, dsInput));
                dsInput = NeutralDualSenseReport(); Activity(dsDetector, dsInput); dsInput[10] = 2;
                Check("DualSense touchpad click recognized", Activity(dsDetector, dsInput));
                dsInput[10] = 1; Check("DualSense PS button recognized", Activity(dsDetector, dsInput));
                dsInput[10] = 4; Check("DualSense mute button recognized without changing mute state", Activity(dsDetector, dsInput));
                dsInput = NeutralDualSenseReport(); Activity(dsDetector, dsInput); dsInput[3] = 161;
                Check("DualSense stick outside deadzone recognized", Activity(dsDetector, dsInput));
                dsInput = NeutralDualSenseReport(); dsInput[0] = 0x31;
                Check("DualSense Bluetooth report rejected", !dsDetector.Observe(dsInput, 64, out observed));
                dsInput = NeutralDualSenseReport(); Check("DualSense short report rejected", !dsDetector.Observe(dsInput, 10, out observed));
                dsInput[8] = 15; Check("DualSense invalid dpad rejected", !dsDetector.Observe(dsInput, 64, out observed));
                IdleLightPolicy idle = new IdleLightPolicy(180000, 5000, 3000);
                Check("no writes before input connection", idle.Next(500000) == IdleLightAction.None);
                idle.Connected(1000); Check("startup baseline does not write a color", idle.Next(1000) == IdleLightAction.None);
                Check("three minutes minus one millisecond remains lit", idle.Next(180999) == IdleLightAction.None);
                Check("three minute deadline requests off", idle.Next(181000) == IdleLightAction.Off);
                idle.Completed(IdleLightAction.Off, true, 181000);
                Check("idle does not send off every timer tick", idle.Next(181100) == IdleLightAction.None);
                Check("idle off refreshed at configured interval", idle.Next(186000) == IdleLightAction.Off);
                idle.Completed(IdleLightAction.Off, true, 186000); idle.Activity(186001);
                Check("real controller input requests configured color", idle.Next(186001) == IdleLightAction.Color);
                idle.Completed(IdleLightAction.Color, false, 186001);
                Check("busy output does not cause rapid retry", idle.Next(186002) == IdleLightAction.None);
                Check("failed color write retried at interval", idle.Next(189001) == IdleLightAction.Color);
                idle.Completed(IdleLightAction.Color, true, 189001);
                Check("active use does not repeatedly override Steam color", idle.Next(200000) == IdleLightAction.None);
                idle.Activity(300000); Check("activity extends inactivity deadline", idle.Next(400000) == IdleLightAction.None);
                idle.Disconnected(); Check("disconnect prevents output", idle.Next(999999) == IdleLightAction.None);
                idle.Suspend(); idle.Connected(1000000);
                Check("resume baseline reasserts off rather than restoring color", idle.Next(1000000) == IdleLightAction.Off);
                idle.Completed(IdleLightAction.Off, true, 1000000); idle.Activity(1000100);
                Check("input after resume restores color", idle.Next(1000100) == IdleLightAction.Color);
                idle.Suspend(); Check("suspend cancels pending color", idle.Next(1000200) == IdleLightAction.None);
                SleepPolicy inputOnly = new SleepPolicy(false); inputOnly.Suspend(); inputOnly.Lock(); inputOnly.AutomaticResume(); inputOnly.UserResume();
                Check("idle mode unlock cannot request a resume color", !inputOnly.Unlock() && inputOnly.Phase == LightPhase.Ready);
                IdleLightPolicy oneOff = new IdleLightPolicy(180000, 0, 3000); oneOff.Connected(0); oneOff.Next(180000); oneOff.Completed(IdleLightAction.Off, true, 180000);
                Check("zero refresh disables repeated idle off writes", oneOff.Next(900000) == IdleLightAction.None);
                string logFolder = Path.Combine(Path.GetTempPath(), "DS4SleepLight-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(logFolder);
                try {
                    string blocked = Path.Combine(logFolder, "blocked"); File.WriteAllText(blocked, "a file cannot be a log directory");
                    Check("unwritable log directory is nonfatal", !Log.TryAppend(blocked, "test\n"));
                    string writable = Path.Combine(logFolder, "fallback");
                    Check("log can continue in a writable fallback", Log.TryAppend(writable, "fallback\n"));
                    string file = Path.Combine(writable, "events.log");
                    using (FileStream large = File.Open(file, FileMode.Open, FileAccess.Write)) large.SetLength(Log.MaximumBytes + 1);
                    Check("oversized log rotates successfully", Log.TryAppend(writable, "after rotation\n"));
                    Check("rotation keeps previous log", File.Exists(file + ".old"));
                    Check("rotation starts a small active log", File.ReadAllText(file) == "after rotation\n");
                } finally {
                    // Delete only this freshly created, validated test directory.
                    string full = Path.GetFullPath(logFolder), temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith("DS4SleepLight-tests-", StringComparison.Ordinal)) Directory.Delete(full, true);
                }
                string json = new JavaScriptSerializer().Serialize(new { passed = checks.Count, hardwareWrites = 0, checks = checks });
                if (args.Length == 1) File.WriteAllText(args[0], json);
                Console.WriteLine("PASS: " + checks.Count + " offline checks; no USB operations."); return 0;
            } catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
    }
}
