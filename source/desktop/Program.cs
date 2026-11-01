using System;
using System.Windows.Forms;
using System.Threading;
namespace XiaomiDesk {
 static class Program {
  [STAThread]static void Main(string[] args){try{
   string name="Local\\XiaomiDesk-"+System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;bool first;
   using(var mutex=new Mutex(true,name,out first)){
    if(!first){try{using(var signal=EventWaitHandle.OpenExisting(name+"-show"))signal.Set();}catch{MessageBox.Show("XiaomiDesk уже запущен. Откройте его через значок возле часов.","XiaomiDesk");}return;}
    using(var showSignal=new EventWaitHandle(false,EventResetMode.AutoReset,name+"-show")){
     Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=delegate(object s,System.Threading.ThreadExceptionEventArgs e){MessageBox.Show(e.Exception.Message,"XiaomiDesk");};
     using(var hub=new Hub(null,null,Array.IndexOf(args,"--background")>=0)){
      var registration=ThreadPool.RegisterWaitForSingleObject(showSignal,delegate{try{if(hub.IsHandleCreated&&!hub.IsDisposed)hub.BeginInvoke(new Action(hub.RestoreFromTray));}catch{}},null,Timeout.Infinite,false);
      try{Application.Run(hub);}finally{registration.Unregister(null);}
     }
    }
    mutex.ReleaseMutex();
   }
  }catch(Exception e){try{System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-error.txt"),e.ToString());}catch{}MessageBox.Show("Не удалось запустить XiaomiDesk: "+e.Message,"XiaomiDesk");}}
 }
}
