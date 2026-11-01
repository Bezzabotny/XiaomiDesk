using System;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using XiaomiDesk;
class RealMediaProbe {
 [STAThread]static int Main(string[] args){int result=1;Application.EnableVisualStyles();using(var window=new Form{Width=700,Height=700,Text="XiaomiDesk: проверка записи"}){var host=new StreamHost{Dock=DockStyle.Fill};window.Controls.Add(host);window.Shown+=async delegate{try{var adb=new Adb(args[0]){Serial=args[1]};await host.Start(adb,false,true,args[2]);await Task.Delay(3000);host.Stop();if(!File.Exists(args[2])||new FileInfo(args[2]).Length<1000||!host.RecordingResult.StartsWith("Запись сохранена:"))throw new Exception("Recording did not finalize: "+host.RecordingResult+"\n"+host.Diagnostics);Console.WriteLine("PASS embedded screen, UHID initialization and recording finalized through Stop");result=0;}catch(Exception e){Console.Error.WriteLine(e.Message);}finally{host.Stop();window.Close();}};Application.Run(window);}return result;}
}
