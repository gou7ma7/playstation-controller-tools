using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace LoLDamageRumble {
    internal static class Tests {
        private static readonly List<string> passed=new List<string>();
        private static readonly RumbleSettings defaults=RegressionSettings.Create();
        private static Pattern Make(Feedback feedback) { return Pattern.Make(feedback,defaults); }
        private static void Reject(Action action,string label) {
            bool rejected=false; try { action(); } catch(ArgumentException) { rejected=true; }
            Check(rejected,label);
        }
        private static string Signature(Pattern pattern) {
            List<string> items=new List<string>();
            foreach(MotorStep step in pattern.Steps) items.Add(step.Duration+":"+step.Fast.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+":"+step.Slow.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture));
            return pattern.Priority+"|"+String.Join("|",items.ToArray());
        }
        private static void Check(bool condition,string label) { if(!condition) throw new Exception("FAIL: "+label); passed.Add(label); }
        private static PlayerSnapshot P(double hp,double resource) { return P(hp,1000,resource,100,"MANA"); }
        private static PlayerSnapshot P(double hp,double max,double resource,double resourceMax,string type) {
            return new PlayerSnapshot { Health=hp,MaxHealth=max,Resource=resource,MaxResource=resourceMax,ResourceType=type,Identity="玩家#CN1",Names=new HashSet<string>(new string[]{"玩家#CN1","玩家"},StringComparer.OrdinalIgnoreCase) };
        }
        private static bool Has(List<Feedback> list,FeedbackKind kind) { foreach(Feedback item in list) if(item.Kind==kind) return true; return false; }
        private static Dictionary<string,object> E(int id,string kind,string killer,params string[] assists) {
            return new Dictionary<string,object> { {"EventID",id},{"EventTime",id*10.0},{"EventName",kind},{"KillerName",killer},{"Assisters",assists} };
        }
        private static void Main(string[] args) {
            try { Run(args); } catch(Exception ex) {
                while(ex.InnerException!=null) ex=ex.InnerException;
                Console.WriteLine(ex.GetType().Name+": "+ex.Message); Environment.ExitCode=1;
            }
        }
        private static void Run(string[] args) {
            ProtocolTests();
            IntegrationTests();
            FeedbackDetector detector=new FeedbackDetector(defaults.Detection);
            Check(detector.Player(P(1000,100),0).Count==0,"startup establishes baseline");
            Check(detector.Player(P(990,100),.1).Count==0,"small damage is silent");
            Check(detector.Player(P(970,100),.2).Count==0,"sub-threshold accumulated damage is silent");
            Check(Has(detector.Player(P(900,100),.4),FeedbackKind.HeavyDamage),"10 percent within 500 ms triggers heavy damage");
            Check(!Has(detector.Player(P(780,100),.5),FeedbackKind.HeavyDamage),"heavy damage cooldown prevents flooding");
            detector.Reset(); detector.Player(P(1000,100),0); detector.Player(P(950,100),.4);
            Check(!Has(detector.Player(P(900,100),.8),FeedbackKind.HeavyDamage),"slow chip damage outside window is silent");
            detector.Reset(); detector.Player(P(1000,100),0); detector.Player(P(950,100),.1); detector.Player(P(1020,100),.2);
            Check(!Has(detector.Player(P(970,100),.3),FeedbackKind.HeavyDamage),"healing does not accumulate as damage");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(700,1200,100,100,"MANA"),.1),FeedbackKind.HeavyDamage),"max health changes reset baseline");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(700,100),2),FeedbackKind.HeavyDamage),"data gaps suppress stale damage");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(Has(detector.Player(P(0,100),.1),FeedbackKind.HeavyDamage),"fatal large hit still triggers heavy damage");
            Check(!Has(detector.Player(P(0,100),.2),FeedbackKind.LowHealth),"dead players do not trigger low health");
            detector.Reset(); detector.Player(P(300,100),0);
            Check(Has(detector.Player(P(250,100),.1),FeedbackKind.LowHealth),"crossing 25 percent triggers low health");
            Check(!Has(detector.Player(P(200,100),.2),FeedbackKind.LowHealth),"remaining low does not repeat warning");
            detector.Player(P(300,100),.3);
            Check(!Has(detector.Player(P(250,100),.4),FeedbackKind.LowHealth),"hysteresis stops threshold chatter");
            detector.Player(P(400,100),9);
            Check(Has(detector.Player(P(240,100),9.1),FeedbackKind.LowHealth),"warning rearms after recovery");
            detector.Reset(); Check(detector.Player(P(100,5),0).Count==0,"starting low does not fabricate a new crossing");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(Has(detector.Player(P(1000,20),.1),FeedbackKind.LowResource),"low mana triggers at 20 percent");
            Check(!Has(detector.Player(P(1000,0),.2),FeedbackKind.LowResource),"empty resource does not continuously repeat");
            detector.Reset(); detector.Player(P(1000,1000,100,200,"ENERGY"),0);
            Check(Has(detector.Player(P(1000,1000,30,200,"ENERGY"),.1),FeedbackKind.LowResource),"energy resource uses the same low threshold");
            detector.Reset(); detector.Player(P(1000,1000,100,100,"FURY"),0);
            Check(!Has(detector.Player(P(1000,1000,0,100,"FURY"),.1),FeedbackKind.LowResource),"fury and rage are not mistaken for empty mana");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(1000,1000,10,200,"MANA"),.1),FeedbackKind.LowResource),"resource capacity changes reset crossing");

            HashSet<string> names=P(1000,100).Names;
            List<Dictionary<string,object>> events=new List<Dictionary<string,object>> { E(0,"GameStart",""),E(1,"ChampionKill","玩家#CN1") };
            detector.Reset(); Check(detector.Events(events,names).Count==0,"existing kills are skipped on startup");
            events.Add(E(2,"ChampionKill","玩家"));
            Check(Has(detector.Events(events,names),FeedbackKind.Kill),"kill matches legacy player alias");
            Check(detector.Events(events,names).Count==0,"cumulative events are deduplicated");
            events.Add(E(3,"ChampionKill","敌人","玩家#CN1"));
            Check(Has(detector.Events(events,names),FeedbackKind.Assist),"assists match the explicit assister list");
            events.Add(E(4,"ChampionKill","队友","别人"));
            Check(detector.Events(events,names).Count==0,"other players kills stay silent");
            events.Add(E(5,"ChampionKill","玩家#CN1"));
            Dictionary<string,object> multi=E(6,"Multikill","玩家#CN1"); multi["KillStreak"]=3; events.Add(multi);
            List<Feedback> batch=detector.Events(events,names);
            Check(batch.Count==1 && batch[0].Kind==FeedbackKind.Multikill && batch[0].Count==3,"multikill replaces ordinary kill in same batch");
            detector.ResetEvents(); events.Add(E(7,"ChampionKill","玩家#CN1"));
            Check(detector.Events(events,names).Count==0,"event reconnection does not replay a backlog");
            Check(detector.Events(new List<Dictionary<string,object>> { E(0,"GameStart","") },names).Count==0,"new matches reset old event ids");

            List<string> writes=new List<string>(); int starts=0;
            PatternScheduler scheduler=new PatternScheduler(delegate(double fast,double slow) { writes.Add(fast+","+slow); },delegate(Pattern pattern) { starts++; },defaults.Scheduling);
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.LowResource,0,1)),0);
            scheduler.Tick(.08); Check(!scheduler.MotorsOn,"pattern gaps turn motors off");
            scheduler.Tick(.19); Check(scheduler.MotorsOn,"pattern resumes after intentional gap");
            scheduler.Tick(.27); scheduler.Tick(.38); scheduler.Tick(.46);
            Check(!scheduler.MotorsOn && writes[writes.Count-1]=="0,0" && starts==1,"multi-pulse pattern ends with motors off");
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.Kill,0,1)),1);
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.HeavyDamage,.2,1)),1.02);
            Check(starts==3 && scheduler.MotorsOn,"heavy hit immediately preempts kill celebration");
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.LowHealth,0,1)),1.03);
            Check(starts==3,"low health queues without mixing into heavy hit");
            scheduler.Tick(1.24); Check(starts==4,"queued low health starts after heavy hit");
            scheduler.Cancel(); Check(!scheduler.MotorsOn && writes[writes.Count-1]=="0,0","pause and exit cancel queued effects and stop motors");
            scheduler.Tick(2); Check(starts==4,"cancelled effects do not replay");
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.Multikill,0,5)),3);
            scheduler.Tick(4); Check(!scheduler.MotorsOn,"timer stalls cancel stale multi-pulse sequences");

            if(args.Length>1 && File.Exists(args[1])) {
                Dictionary<string,object> root=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(args[1]));
                Dictionary<string,object> active=Json.Object(Json.Value(root,"activePlayer"));
                bool rejected=false; try { PlayerSnapshot.Parse(active); } catch(IOException) { rejected=true; }
                Check(rejected,"zero-health-capacity placeholder in synthetic fixture is rejected");
                Dictionary<string,object> stats=Json.Object(Json.Value(active,"championStats")); stats["maxHealth"]=1000; stats["currentHealth"]=800;
                PlayerSnapshot sample=PlayerSnapshot.Parse(active);
                Check(sample.MaxHealth==1000 && sample.Names.Count>0,"Live Client schema parses with representative live health values");
                Dictionary<string,object> eventRoot=Json.Object(Json.Value(root,"events"));
                List<Dictionary<string,object>> fixtureEvents=new List<Dictionary<string,object>>();
                foreach(object item in Json.Array(Json.Value(eventRoot,"Events"))) fixtureEvents.Add(Json.Object(item));
                detector.Reset(); Check(detector.Events(fixtureEvents,sample.Names).Count==0,"synthetic event fixture initializes without false feedback");
            }
            ConfigurationTests(args);
            string report=new JavaScriptSerializer().Serialize(new { passed=passed.Count,preset=FeedbackConfig.SelectedPreset,englishUi=Ui.English,scenarios=passed });
            if(args.Length>0) File.WriteAllText(args[0],report);
            Console.WriteLine("PASS: "+passed.Count+" behavior checks; no controller writes performed.");
        }
        private static void ConfigurationTests(string[] args) {
            foreach(string preset in FeedbackConfig.PresetNames) {
                RumbleSettings c=FeedbackConfig.Create(preset); c.Validate();
                Check(c.Name==preset,"preset validates: "+preset);
            }
            if(args.Length>2) Check(FeedbackConfig.SelectedPreset==args[2] && FeedbackConfig.Current.Name==args[2],"compiled preset matches build selection");
            if(args.Length>3) { bool expected=args[3]=="auto" ? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName!="zh" : args[3]=="en"; Check(Ui.English==expected,"compiled UI language matches build selection"); }
            Check(Ui.Text("中文","English")== (Ui.English?"English":"中文"),"UI localization resolves consistently");
            Check(defaults.MasterIntensity==.65,"reference inputs preserve 65 percent master intensity");
            bool enabled=true; foreach(EffectRule rule in defaults.Effects.Values) enabled=enabled&&rule.Enabled;
            Check(enabled,"reference inputs preserve all six enabled effects");
            Check(Signature(Make(new Feedback(FeedbackKind.HeavyDamage,.10,1)))=="100|220:0.75:0.82","reference inputs preserve heavy damage waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.LowHealth,0,1)))=="80|160:0.25:1|100:0:0|160:0.25:1","reference inputs preserve low health waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.LowResource,0,1)))=="40|80:0.9:0|110:0:0|80:0.9:0|110:0:0|80:0.9:0","reference inputs preserve low resource waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.Kill,0,1)))=="60|90:0.9:0.2|80:0:0|150:1:0.45","reference inputs preserve kill waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.Assist,0,1)))=="50|150:0.6:0.15","reference inputs preserve assist waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.Multikill,0,3)))=="70|110:1:0.6|80:0:0|110:1:0.6|80:0:0|110:1:0.6","reference inputs preserve multikill waveform");
            RumbleSettings custom=RegressionSettings.Create();
            RumbleSettings copy=FeedbackConfig.Create("default"); double originalSlow=copy.Effects[FeedbackKind.HeavyDamage].Steps[0].Slow;
            copy.Effects[FeedbackKind.HeavyDamage].Steps[0].Slow=originalSlow>.5?.1:.9;
            Check(FeedbackConfig.Create("default").Effects[FeedbackKind.HeavyDamage].Steps[0].Slow==originalSlow,"presets do not share mutable waveform state");
            Pattern detached=Pattern.Make(new Feedback(FeedbackKind.Kill,0,1),custom); detached.Steps[0].Duration=99;
            Check(custom.Effects[FeedbackKind.Kill].Steps[0].Duration==90,"generated patterns do not mutate configuration steps");
            custom=RegressionSettings.Create(); custom.Detection.HeavyDamageRatio=.20; custom.Detection.HeavyWindowSeconds=.25; custom.Validate();
            FeedbackDetector detector=new FeedbackDetector(custom.Detection); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(850,100),.1),FeedbackKind.HeavyDamage),"custom damage threshold ignores 15 percent loss");
            Check(Has(detector.Player(P(800,100),.2),FeedbackKind.HeavyDamage),"custom damage threshold triggers at 20 percent");
            detector.Reset(); detector.Player(P(1000,100),0); detector.Player(P(850,100),.1);
            Check(!Has(detector.Player(P(800,100),.3),FeedbackKind.HeavyDamage),"custom rolling damage window excludes old peak");
            custom.Detection.LowHealthRatio=.10; custom.Detection.HealthRearmRatio=.20; custom.Validate();
            detector=new FeedbackDetector(custom.Detection); detector.Player(P(250,100),0);
            Check(!Has(detector.Player(P(150,100),.1),FeedbackKind.LowHealth) && Has(detector.Player(P(100,100),.2),FeedbackKind.LowHealth),"custom low health threshold controls crossing");
            custom.Detection.ResourceTypes=new string[]{"MANA"}; custom.Validate();
            detector=new FeedbackDetector(custom.Detection); detector.Player(P(1000,1000,100,100,"ENERGY"),0);
            Check(!Has(detector.Player(P(1000,1000,10,100,"ENERGY"),.1),FeedbackKind.LowResource),"MANA-only configuration excludes ENERGY");
            custom=RegressionSettings.Create(); custom.Effects[FeedbackKind.HeavyDamage].DamageSlowGain=0;
            Check(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,1,1),custom).Steps[0].Slow==.82,"zero gain disables dynamic heavy-hit scaling");
            custom.Effects[FeedbackKind.HeavyDamage].DamageSlowGain=10;
            Check(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,1,1),custom).Steps[0].Slow==1,"dynamic strength is capped at one");
            custom.Effects[FeedbackKind.LowResource].RepeatCount=1;
            Check(Pattern.Make(new Feedback(FeedbackKind.LowResource,0,1),custom).Steps.Length==1,"custom repeat count changes pulse sequence");
            custom.Effects[FeedbackKind.Multikill].RepeatForKillCount=false; custom.Effects[FeedbackKind.Multikill].RepeatCount=2;
            Check(Pattern.Make(new Feedback(FeedbackKind.Multikill,0,5),custom).Steps.Length==3,"multikill can use a fixed repeat count");
            custom=RegressionSettings.Create(); custom.Scheduling.QueueExpirySeconds=.1;
            int starts=0; PatternScheduler scheduler=new PatternScheduler(delegate(double f,double s){},delegate(Pattern p){starts++;},custom.Scheduling);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,.1,1),custom),0);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.LowHealth,0,1),custom),.02);
            scheduler.Tick(.22); Check(starts==1,"custom queue lifetime discards stale warnings"); scheduler.Cancel();
            custom=RegressionSettings.Create(); custom.Effects[FeedbackKind.Assist].Priority=200; starts=0;
            scheduler=new PatternScheduler(delegate(double f,double s){},delegate(Pattern p){starts++;},custom.Scheduling);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,.1,1),custom),0);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.Assist,0,1),custom),.01);
            Check(starts==2,"configured priority changes preemption behavior"); scheduler.Cancel();
            Reject(delegate { FeedbackConfig.Create("missing"); },"unknown presets fail validation");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.MasterIntensity=1.1; c.Validate(); },"out-of-range master intensity rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Detection.HealthRearmRatio=.20; c.Validate(); },"health rearm must exceed low threshold");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Detection.ResourceTypes=new string[]{"AMMO"}; c.Validate(); },"unsupported resources rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Detection.ResourceTypes=new string[]{"MANA","MANA"}; c.Validate(); },"duplicate resource types rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Kill].Steps[0].Duration=0; c.Validate(); },"zero-duration steps rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Kill].Steps[0].Fast=double.NaN; c.Validate(); },"non-finite motor strength rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Kill].RepeatForKillCount=true; c.Validate(); },"kill-count repetition restricted to multikill");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Assist].DamageSlowGain=1; c.Validate(); },"damage scaling restricted to heavy damage");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Multikill].Steps=new MotorStep[]{new MotorStep(2000,1,1),new MotorStep(2000,1,1)}; c.Validate(); },"overlong effect sequences rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Scheduling.QueueCapacity=0; c.Validate(); },"empty queue capacity rejected");
        }
        private static void ProtocolTests() {
            Check(MotorReport.Identify(0x054c,0x09cc,1,5,64,32)==ControllerKind.Ds4V2,"USB DS4 v2 descriptor accepted");
            Check(MotorReport.Identify(0x054c,0x0ce6,1,5,64,48)==ControllerKind.DualSense,"USB DualSense descriptor accepted");
            Check(MotorReport.Identify(0x054c,0x0ce6,1,5,78,78)==ControllerKind.None,"Bluetooth DualSense descriptor refused");
            Check(MotorReport.Identify(0x054c,0x09cc,1,5,78,78)==ControllerKind.None,"Bluetooth DS4 descriptor refused");
            Check(MotorReport.Identify(0x1234,0x0ce6,1,5,64,48)==ControllerKind.None,"other vendor refused");
            Check(MotorReport.Identify(0x054c,0x05c4,1,5,64,32)==ControllerKind.None,"DS4 first generation remains unsupported");
            Check(MotorReport.Identify(0x054c,0x0df2,1,5,64,48)==ControllerKind.None,"untested DualSense Edge refused");
            Check(MotorReport.Identify(0x054c,0x0ce6,0xff00,5,64,48)==ControllerKind.None,"vendor-specific HID collection refused");
            Check(MotorReport.Identify(0x054c,0x0ce6,1,6,64,48)==ControllerKind.None,"keyboard HID collection refused");
            Check(MotorReport.Identify(0x054c,0x09cc,1,5,64,48)==ControllerKind.None,"DS4 cannot receive a DualSense frame");
            Check(MotorReport.Identify(0x054c,0x0ce6,1,5,64,32)==ControllerKind.None,"DualSense cannot receive a DS4 frame");
            foreach(ControllerKind kind in new ControllerKind[]{ControllerKind.Ds4V2,ControllerKind.DualSense}) {
                bool ds4=kind==ControllerKind.Ds4V2;
                byte[] on=MotorReport.Make(kind,85,170), off=MotorReport.Make(kind,0,0);
                Check(on.Length==(ds4?32:48) && off.Length==on.Length,kind+" USB frame size is correct for start and stop");
                Check(on[0]==(ds4?5:2) && off[0]==on[0] && on[1]==(ds4?1:3) && off[1]==on[1],kind+" start and stop enable only the compatible motor protocol");
                int fast=ds4?4:3, slow=ds4?5:4;
                Check(on[fast]==85 && on[slow]==170 && off[fast]==0 && off[slow]==0,kind+" independent right/left strengths and zero stop preserved");
                bool isolated=true;
                for(int i=2;i<on.Length;i++) if(i!=fast && i!=slow && (on[i]!=0 || off[i]!=0)) isolated=false;
                Check(isolated,kind+" light, player LED, trigger, audio and microphone fields remain disabled");
            }
            Reject(delegate { MotorReport.Make(ControllerKind.None,255,255); },"unknown motor protocol cannot emit a report");
        }
        private static void IntegrationTests() {
            LoLIntegrationSettings disabled=LoLIntegrationSettings.Parse("{}");
            Check(!disabled.Enabled && !disabled.ClearTriggerResistance,"absent optional settings preserve standalone rumble");
            LoLIntegrationSettings enabled=LoLIntegrationSettings.Parse("{\"enabled\":true,\"steamExecutable\":\"C:\\\\Steam\\\\steam.exe\",\"steamInputAppId\":123,\"clearTriggerResistance\":true}");
            Check(enabled.Enabled && enabled.SteamInputAppId==123 && enabled.ClearTriggerResistance,"local Steam integration settings validated");
            Reject(delegate { LoLIntegrationSettings.Parse("{\"enabled\":\"false\"}"); },"string booleans cannot accidentally enable integration");
            Reject(delegate { LoLIntegrationSettings.Parse("{\"pollMilliseconds\":1}"); },"integration polling cannot flood Steam");
            Reject(delegate { LoLIntegrationSettings.Parse("{\"triggerRefreshMilliseconds\":1}"); },"trigger reset refresh cannot flood HID");
            Reject(delegate { LoLIntegrationSettings.Parse("{\"enabled\":true}"); },"enabled integration needs an explicit local layout");
            Reject(delegate { LoLIntegrationSettings.Parse("{\"gameProcessName\":\"League of Legends.exe\"}"); },"foreground rule uses process name without executable suffix");
            Reject(delegate { LoLIntegrationSettings.Parse("{\"unexpected\":true}"); },"unknown integration options rejected");
            LoLContextPolicy policy=new LoLContextPolicy(123,2000);
            Check(policy.DesiredSelection(false)==0 && policy.DesiredSelection(true)==123,"only the actual foreground match selects LoL mapping");
            Check(policy.NeedsSelection(false),"initial desktop selection restores Steam automatic mode");
            policy.SelectionSucceeded(false); Check(!policy.NeedsSelection(false),"stable foreground does not repeatedly launch Steam");
            Check(policy.NeedsSelection(true) && policy.NeedsSelection(true),"failed selection remains retryable");
            policy.SelectionSucceeded(true); Check(!policy.NeedsSelection(true) && policy.NeedsSelection(false),"leaving LoL releases the forced layout");
            policy.InvalidateSelection(); Check(policy.NeedsSelection(true),"Steam restart reselects the required layout");
            Check(!policy.NeedsTriggerReset(false,true,0),"background LoL does not write trigger effects");
            Check(policy.NeedsTriggerReset(true,true,.1),"entering LoL clears stale trigger resistance immediately");
            Check(!policy.NeedsTriggerReset(true,true,.2) && !policy.NeedsTriggerReset(true,true,2),"stable LoL respects trigger refresh interval");
            Check(policy.NeedsTriggerReset(true,true,2.1),"foreground trigger clear refresh is bounded");
            Check(!policy.NeedsTriggerReset(false,true,2.2) && !policy.NeedsTriggerReset(false,true,99),"other games receive no trigger resets");
            Check(policy.NeedsTriggerReset(true,true,99.1),"returning to LoL clears newly inherited resistance");
            Check(!policy.NeedsTriggerReset(true,false,100),"disabling reset leaves triggers untouched");
            Check(policy.NeedsTriggerReset(true,true,100.1),"reenabling reset clears on the next foreground check");
            Check(LoLIntegrationSettings.Parse("{}").TriggerRefreshMilliseconds==0,"entry-only fallback is the new default");
            Check(LoLIntegrationSettings.Parse("{\"triggerRefreshMilliseconds\":0}").TriggerRefreshMilliseconds==0,"zero refresh selects entry-only mode");
            LoLContextPolicy once=new LoLContextPolicy(123,0);
            Check(once.NeedsTriggerReset(true,true,0),"entry-only mode clears once on entering foreground");
            Check(!once.NeedsTriggerReset(true,true,2) && !once.NeedsTriggerReset(true,true,600),"entry-only mode never periodically clears a stable foreground session");
            Check(!once.NeedsTriggerReset(false,true,601) && once.NeedsTriggerReset(true,true,602),"entry-only mode rearms after leaving and returning to LoL");
            Check(!once.NeedsTriggerReset(true,false,603) && once.NeedsTriggerReset(true,true,604),"entry-only mode rearms after disabling and enabling reset");
            once.TriggerResetFailed(604);
            Check(!once.NeedsTriggerReset(true,true,605) && once.NeedsTriggerReset(true,true,606),"failed entry-only write retries with bounded two-second backoff");
            Check(!once.NeedsTriggerReset(true,true,1000),"successful retry does not turn entry-only mode into a periodic writer");
            byte[] clear=TriggerReport.Clear(ControllerKind.DualSense);
            Check(clear.Length==48 && clear[0]==2 && clear[1]==0x0c,"trigger reset uses USB DualSense frame and only trigger valid flags");
            Check(clear[11]==5 && clear[22]==5,"both triggers receive the explicit Off mode");
            bool isolated=true; for(int i=2;i<clear.Length;i++) if(i!=11 && i!=22 && clear[i]!=0) isolated=false;
            Check(isolated,"trigger reset leaves motor, LED, microphone and audio fields disabled");
            Reject(delegate { TriggerReport.Clear(ControllerKind.Ds4V2); },"DS4 cannot receive a DualSense trigger report");
        }
    }
}
