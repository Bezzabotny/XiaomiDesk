using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using XiaomiDesk;
class UISmoke {
 [STAThread]static int Main(string[] args){try{
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string testState=Path.Combine(args[0],"ui-settings-"+Guid.NewGuid().ToString("N")+".json");
  var startupStore=new MemoryStartupStore();
  using(var hub=new Hub(testState,startupStore)){
   hub.Size=new Size(1040,760);hub.Show();Application.DoEvents();var show=typeof(Hub).GetMethod("ShowPage",BindingFlags.Instance|BindingFlags.NonPublic);
   var output=args.Length>0?args[0]:".";Directory.CreateDirectory(output);
   foreach(string page in new[]{"Галерея","Ссылки","Инструменты","Обзор","Подключение","Файлы","Передачи","Экран телефона","Камера","Уведомления","Буфер обмена","Звонки и SMS","Интернет","Настройки"}){
    show.Invoke(hub,new object[]{page});hub.PerformLayout();Application.DoEvents();
    var panel=(Panel)typeof(Hub).GetField("content",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hub);var media=panel.Controls.OfType<StreamHost>().FirstOrDefault();if(media!=null&&media.Top<140)throw new Exception("Media covers its start/stop controls");
    using(var bitmap=new Bitmap(hub.Width,hub.Height)){hub.DrawToBitmap(bitmap,new Rectangle(Point.Empty,hub.Size));bitmap.Save(Path.Combine(output,page+".png"));if(bitmap.GetPixel(40,250).ToArgb()==bitmap.GetPixel(bitmap.Width-40,250).ToArgb())throw new Exception("Blank UI: controls not rendered");}
    Console.WriteLine("PASS render "+page);
   }
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   var allChecks=Descendants(hub).OfType<CheckBox>().ToArray();var startup=allChecks.First(c=>c.Text.StartsWith("Запускать XiaomiDesk"));if(startup.Checked||startupStore.Value!=null)throw new Exception("Startup enabled without user choice");startup.Checked=true;if(startupStore.Value==null||!startupStore.Value.EndsWith(" --background"))throw new Exception("Startup opt-in not registered");startup.Checked=false;if(startupStore.Value!=null)throw new Exception("Startup opt-out not removed");Console.WriteLine("PASS startup opt-in/out affects only injected registration store");
   var initialBridge=(Bridge)typeof(Hub).GetField("bridge",flags).GetValue(hub);typeof(Hub).GetMethod("Received",flags).Invoke(hub,new object[]{new PhoneEvent{kind="clipboard",text="unverified-phone",SessionToken=initialBridge.Token}});Application.DoEvents();if((string)typeof(Hub).GetField("lastClip",flags).GetValue(hub)!="")throw new Exception("Unverified phone event accepted before selection");Console.WriteLine("PASS reject phone events before identity verification");
   show.Invoke(hub,new object[]{"Подключение"});var auto=(CheckBox)typeof(Hub).GetField("autoCheck",flags).GetValue(hub);auto.Checked=false;if(new SettingsStore(testState).Load().AutoConnect)throw new Exception("Auto-connect opt-out not saved");show.Invoke(hub,new object[]{"Обзор"});show.Invoke(hub,new object[]{"Подключение"});auto=(CheckBox)typeof(Hub).GetField("autoCheck",flags).GetValue(hub);if(auto.Checked)throw new Exception("Auto-connect opt-out lost on navigation");Console.WriteLine("PASS auto-connect checkbox persists and survives page navigation");
   var source=new TaskCompletionSource<bool>();Func<Task> pending=delegate{return source.Task;};var operation=(Task)typeof(Hub).GetMethod("Use",flags).Invoke(hub,new object[]{pending});
   var devices=(ComboBox)typeof(Hub).GetField("devices",flags).GetValue(hub);var nav=(Control)typeof(Hub).GetField("sidebar",flags).GetValue(hub);
   if(devices.Enabled||nav.Enabled)throw new Exception("Device/nav changes allowed during transfer");source.SetResult(true);while(!operation.IsCompleted)Application.DoEvents();if(!devices.Enabled||!nav.Enabled)throw new Exception("Controls stay locked after operation");Console.WriteLine("PASS lock device and navigation during asynchronous operation");
   var change=typeof(Hub).GetMethod("SwitchSerial",flags);change.Invoke(hub,new object[]{"phone-a"});var bridge=(Bridge)typeof(Hub).GetField("bridge",flags).GetValue(hub);string oldToken=bridge.Token;
   typeof(Hub).GetMethod("Received",flags).Invoke(hub,new object[]{new PhoneEvent{kind="clipboard",text="old-phone-secret",SessionToken=oldToken}});change.Invoke(hub,new object[]{"phone-b"});Application.DoEvents();if((string)typeof(Hub).GetField("lastClip",flags).GetValue(hub)!="")throw new Exception("Stale phone event accepted");if(bridge.Token==oldToken)throw new Exception("Phone session not rotated");change.Invoke(hub,new object[]{null});Console.WriteLine("PASS drop queued events from previously selected phone");
   var runner=new Adb(args[1]);typeof(Hub).GetField("adb",flags).SetValue(hub,runner);change.Invoke(hub,new object[]{"phone-a"});
   var refresh=(Task)typeof(Hub).GetMethod("RefreshDevices",flags).Invoke(hub,new object[0]);var hold=new TaskCompletionSource<bool>();Func<Task> holdAction=delegate{return hold.Task;};var ongoing=(Task)typeof(Hub).GetMethod("Use",flags).Invoke(hub,new object[]{holdAction});
   while(!refresh.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}if(runner.Serial!="phone-a")throw new Exception("Background refresh switched device during operation");hold.SetResult(true);while(!ongoing.IsCompleted)Application.DoEvents();change.Invoke(hub,new object[]{null});Console.WriteLine("PASS defer in-flight background device refresh during operation");
   hub.Close();Application.DoEvents();if(hub.IsDisposed||hub.Visible||(bool)typeof(Hub).GetField("closing",flags).GetValue(hub))throw new Exception("Close-to-tray terminated the hub");hub.RestoreFromTray();Application.DoEvents();if(!hub.Visible)throw new Exception("Tray restore failed");Console.WriteLine("PASS close-to-tray preserves hub and restore reopens window");
   var completion=new TaskCompletionSource<bool>();using(var cancellation=new System.Threading.CancellationTokenSource()){
   typeof(Hub).GetField("transferActive",flags).SetValue(hub,true);typeof(Hub).GetField("transferCancel",flags).SetValue(hub,cancellation);typeof(Hub).GetField("transferCompletion",flags).SetValue(hub,completion);
   typeof(Hub).GetMethod("RequestExit",flags).Invoke(hub,new object[0]);Application.DoEvents();if(hub.IsDisposed||!cancellation.IsCancellationRequested)throw new Exception("Exit did not wait for transfer cancellation cleanup");typeof(Hub).GetField("transferActive",flags).SetValue(hub,false);completion.SetResult(true);var watch=System.Diagnostics.Stopwatch.StartNew();while(!hub.IsDisposed&&watch.ElapsedMilliseconds<2000)Application.DoEvents();if(!hub.IsDisposed)throw new Exception("Explicit exit did not dispose the hub after cleanup");}
   try{runner.Run(false,"unused").GetAwaiter().GetResult();throw new Exception("ADB still accepts commands after exit");}catch(OperationCanceledException){}Console.WriteLine("PASS explicit exit waits for transfer cleanup then disposes tray and ADB");
  }
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static System.Collections.Generic.IEnumerable<Control> Descendants(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in Descendants(c))yield return child;}}
 class MemoryStartupStore:IStartupStore {public string Value;public string Read(){return Value;}public void Write(string value){Value=value;}public void Delete(){Value=null;}}
}
