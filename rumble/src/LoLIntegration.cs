using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

namespace LoLDamageRumble {
    // Optional runtime integration. An absent file leaves the standalone feedback app unchanged.
    internal sealed class LoLIntegrationSettings {
        internal bool Enabled,ClearTriggerResistance;
        internal uint SteamInputAppId;
        internal string SteamExecutable="",GameProcessName="League of Legends";
        internal int PollMilliseconds=200,TriggerRefreshMilliseconds=0;
        internal void Validate() {
            if(PollMilliseconds<100 || PollMilliseconds>5000) throw new ArgumentException("Integration poll interval must be 100..5000 ms.");
            if(TriggerRefreshMilliseconds!=0 && (TriggerRefreshMilliseconds<500 || TriggerRefreshMilliseconds>60000)) throw new ArgumentException("Trigger refresh must be 0 (on entry only) or 500..60000 ms.");
            if(Enabled && (SteamInputAppId==0 || !Path.IsPathRooted(SteamExecutable) || !String.Equals(Path.GetFileName(SteamExecutable),"steam.exe",StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Enabled integration requires the Steam executable and a nonzero local AppID.");
            if(String.IsNullOrWhiteSpace(GameProcessName) || GameProcessName.IndexOfAny(new char[]{'\\','/','\0'})>=0 || GameProcessName.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Use a process name without a path or .exe suffix.");
        }
        internal static LoLIntegrationSettings Parse(string json) {
            Dictionary<string,object> data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
            if(data==null) throw new ArgumentException("Integration settings must be a JSON object.");
            LoLIntegrationSettings settings=new LoLIntegrationSettings();
            foreach(string key in data.Keys) if(key!="enabled" && key!="clearTriggerResistance" && key!="steamExecutable" && key!="steamInputAppId" && key!="gameProcessName" && key!="pollMilliseconds" && key!="triggerRefreshMilliseconds") throw new ArgumentException("Unknown integration setting: "+key);
            object value;
            if(data.TryGetValue("enabled",out value)) { if(!(value is bool)) throw new ArgumentException("enabled must be boolean."); settings.Enabled=(bool)value; }
            if(data.TryGetValue("clearTriggerResistance",out value)) { if(!(value is bool)) throw new ArgumentException("clearTriggerResistance must be boolean."); settings.ClearTriggerResistance=(bool)value; }
            if(data.TryGetValue("steamExecutable",out value)) settings.SteamExecutable=Convert.ToString(value);
            if(data.TryGetValue("gameProcessName",out value)) settings.GameProcessName=Convert.ToString(value);
            if(data.TryGetValue("steamInputAppId",out value) && !UInt32.TryParse(Convert.ToString(value),out settings.SteamInputAppId)) throw new ArgumentException("steamInputAppId must be an unsigned integer.");
            int parsed;
            if(data.TryGetValue("pollMilliseconds",out value)) { if(!Int32.TryParse(Convert.ToString(value),out parsed)) throw new ArgumentException("pollMilliseconds must be an integer."); settings.PollMilliseconds=parsed; }
            if(data.TryGetValue("triggerRefreshMilliseconds",out value)) { if(!Int32.TryParse(Convert.ToString(value),out parsed)) throw new ArgumentException("triggerRefreshMilliseconds must be an integer."); settings.TriggerRefreshMilliseconds=parsed; }
            settings.Validate(); return settings;
        }
    }

    internal sealed class LoLContextPolicy {
        private readonly uint appId;
        private readonly double refreshSeconds;
        private uint? selected;
        private bool previousForeground,resetApplied;
        private double nextTriggerReset;
        internal LoLContextPolicy(uint targetAppId,int triggerRefreshMilliseconds) { appId=targetAppId; refreshSeconds=triggerRefreshMilliseconds/1000.0; }
        internal uint DesiredSelection(bool foreground) { return foreground ? appId : 0; }
        internal bool NeedsSelection(bool foreground) { return !selected.HasValue || selected.Value!=DesiredSelection(foreground); }
        internal void SelectionSucceeded(bool foreground) { selected=DesiredSelection(foreground); }
        internal void InvalidateSelection() { selected=null; }
        internal bool NeedsTriggerReset(bool foreground,bool resetEnabled,double now) {
            if(!foreground) { previousForeground=false; resetApplied=false; nextTriggerReset=0; return false; }
            bool entered=!previousForeground; previousForeground=true;
            if(!resetEnabled) { resetApplied=false; nextTriggerReset=0; return false; }
            if(entered || (!resetApplied && now>=nextTriggerReset) || (refreshSeconds>0 && now>=nextTriggerReset)) { resetApplied=true; nextTriggerReset=now+refreshSeconds; return true; }
            return false;
        }
        internal void TriggerResetFailed(double now) { resetApplied=false; nextTriggerReset=now+2; }
    }

    internal static class TriggerReport {
        internal static byte[] Clear(ControllerKind kind) {
            if(kind!=ControllerKind.DualSense) throw new ArgumentException("Trigger reset supports USB DualSense only.");
            byte[] report=new byte[48]; report[0]=2; report[1]=0x0c;
            // Right/left trigger valid flags only. Off (0x05), with all effect parameters zero.
            report[11]=5; report[22]=5; return report;
        }
    }

    internal sealed class LoLIntegration : IDisposable {
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
        private readonly LoLIntegrationSettings settings;
        private readonly LoLContextPolicy policy;
        private readonly Func<bool> clearTriggers;
        private readonly string logFile;
        private double nextPoll,nextSelectionAttempt;
        private int steamProcessId;
        private string configurationError="",selectionError="",triggerError="";
        internal bool Configured { get { return settings.Enabled; } }
        internal bool Enabled,Foreground;
        internal bool ClearTriggerResistance { get { return settings.ClearTriggerResistance; } }
        internal uint SelectedAppId;
        internal int TriggerResetsWritten;
        internal string LastError { get { return (configurationError+" "+selectionError+" "+triggerError).Trim(); } }
        internal LoLIntegration(string configurationFile,Func<bool> resetTriggers) {
            settings=new LoLIntegrationSettings(); clearTriggers=resetTriggers;
            logFile=Path.Combine(Path.GetDirectoryName(configurationFile),"integration.log");
            try { if(File.Exists(configurationFile)) settings=LoLIntegrationSettings.Parse(File.ReadAllText(configurationFile)); }
            catch(Exception ex) { if(!(ex is IOException) && !(ex is UnauthorizedAccessException) && !(ex is ArgumentException) && !(ex is InvalidOperationException)) throw; configurationError=ex.Message; }
            Enabled=settings.Enabled; policy=new LoLContextPolicy(settings.SteamInputAppId,settings.TriggerRefreshMilliseconds);
            if(Enabled) Log("started; foreground mapping and optional trigger reset enabled");
            if(LastError.Length>0) Log("configuration_error: "+LastError);
        }
        private void Log(string message) {
            try {
                if(File.Exists(logFile) && new FileInfo(logFile).Length>512*1024) { string previous=logFile+".1"; if(File.Exists(previous)) File.Delete(previous); File.Move(logFile,previous); }
                File.AppendAllText(logFile,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+message+Environment.NewLine);
            } catch(IOException) {} catch(UnauthorizedAccessException) {}
        }
        private bool Request(uint appId) {
            try {
                ProcessStartInfo start=new ProcessStartInfo(settings.SteamExecutable,"steam://forceinputappid/"+appId);
                start.UseShellExecute=true; start.WindowStyle=ProcessWindowStyle.Hidden;
                using(Process process=Process.Start(start)) {}
                SelectedAppId=appId; selectionError=""; Log(appId==0 ? "Steam automatic input selection restored" : "LoL Steam Input layout selected"); return true;
            } catch(Exception ex) {
                if(!(ex is System.ComponentModel.Win32Exception) && !(ex is InvalidOperationException) && !(ex is FileNotFoundException)) throw;
                selectionError="Steam selection failed: "+ex.Message; Log(selectionError); return false;
            }
        }
        internal void Tick(bool desktopReady,double now) {
            if(!Configured || now<nextPoll) return; nextPoll=now+settings.PollMilliseconds/1000.0;
            bool foreground=false;
            if(Enabled && desktopReady) {
                uint processId; GetWindowThreadProcessId(GetForegroundWindow(),out processId);
                try { if(processId!=0) using(Process process=Process.GetProcessById((int)processId)) { foreground=String.Equals(process.ProcessName,settings.GameProcessName,StringComparison.OrdinalIgnoreCase); } }
                catch(ArgumentException) {} catch(InvalidOperationException) {}
            }
            if(Foreground!=foreground) { Foreground=foreground; Log(foreground ? "LoL match window entered foreground" : "LoL match window left foreground"); }
            Process[] steam=Process.GetProcessesByName("steam"); int currentSteam=0;
            try { if(steam.Length>0) currentSteam=steam[0].Id; } finally { foreach(Process process in steam) process.Dispose(); }
            if(currentSteam!=steamProcessId) { steamProcessId=currentSteam; policy.InvalidateSelection(); nextSelectionAttempt=0; if(currentSteam==0) SelectedAppId=0; }
            if(currentSteam!=0 && now>=nextSelectionAttempt && policy.NeedsSelection(foreground)) {
                nextSelectionAttempt=now+2;
                if(Request(policy.DesiredSelection(foreground))) policy.SelectionSucceeded(foreground);
            }
            if(policy.NeedsTriggerReset(foreground,settings.ClearTriggerResistance,now)) {
                try { if(clearTriggers()) { TriggerResetsWritten++; if(TriggerResetsWritten==1) Log(settings.TriggerRefreshMilliseconds==0 ? "DualSense trigger resistance cleared; on-entry-only fallback" : "DualSense trigger resistance cleared; periodic fallback only while LoL is foreground"); } else policy.TriggerResetFailed(now); triggerError=""; }
                catch(IOException ex) { policy.TriggerResetFailed(now); string error="Trigger reset failed: "+ex.Message; if(triggerError!=error) Log(error); triggerError=error; }
            }
        }
        internal void Suspend(double now) { nextPoll=0; Tick(false,now); }
        public void Dispose() {
            if(Configured && SelectedAppId!=0) { Process[] steam=Process.GetProcessesByName("steam"); bool running=steam.Length>0; foreach(Process process in steam) process.Dispose(); if(running) Request(0); }
            Enabled=false; Foreground=false;
            if(Configured) Log("stopped; trigger writes released to other games/tools");
        }
    }
}
