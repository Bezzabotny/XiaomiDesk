using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public class StreamHost:Panel {
  [DllImport("user32.dll")]static extern IntPtr SetParent(IntPtr child,IntPtr parent);
  [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr h,int index);
  [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int index,int value);
  [DllImport("user32.dll")]static extern bool MoveWindow(IntPtr h,int x,int y,int w,int height,bool redraw);
  [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int cmd);
  [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
  Process process;IntPtr child;bool stopping;string errors="",recordingFile;
  public string RecordingResult {get;private set;}
  public event Action<string> RecordingStatusChanged;
  readonly object logLock=new object();
  public string Diagnostics {get{lock(logLock){return ScreenOptions.Explain(errors);}}}
  void Append(string line){if(line==null)return;lock(logLock){errors+=line+"\n";if(errors.Length>16000)errors=errors.Substring(errors.Length-16000);}}
  public StreamHost(){BackColor=Color.FromArgb(17,24,30);Resize+=delegate{Fit();};}
  public async Task Start(Adb adb,bool camera,bool hardwareInput=true,string recording=null){
   Stop();stopping=false;errors="";RecordingResult=null;recordingFile=recording;string title="XiaomiDesk-"+Guid.NewGuid().ToString("N");
   string args=ScreenOptions.Arguments(adb.Serial,title,camera,hardwareInput);
   if(recording!=null)args+=" --no-audio --record="+Core.Arg(recording);
   process=new Process{StartInfo=new ProcessStartInfo(System.IO.Path.Combine(adb.ToolDir,"scrcpy.exe"),args){WorkingDirectory=adb.ToolDir,UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true}};
   process.StartInfo.EnvironmentVariables["ADB"]=System.IO.Path.Combine(adb.ToolDir,"adb.exe");
   var active=process;process.ErrorDataReceived+=delegate(object s,DataReceivedEventArgs e){Append(e.Data);};process.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){Append(e.Data);};
   process.Start();process.BeginErrorReadLine();process.BeginOutputReadLine();
   for(int i=0;i<100;i++){await Task.Delay(100);if(stopping||IsDisposed)return;if(active.HasExited){Stop();throw new Exception("Не удалось открыть поток. "+Diagnostics);}active.Refresh();child=active.MainWindowHandle;if(child!=IntPtr.Zero){SetParent(child,Handle);int style=GetWindowLong(child,-16);SetWindowLong(child,-16,(style & unchecked((int)~0x80C40000))|0x40000000);ShowWindow(child,5);Fit();if(recordingFile!=null&&RecordingStatusChanged!=null)RecordingStatusChanged("● Идёт запись экрана без звука");return;}}
   Stop();throw new Exception("Окно потока не появилось. Проверьте разрешение подключения на Xiaomi.");
  }
  void Fit(){if(child!=IntPtr.Zero)MoveWindow(child,0,0,ClientSize.Width,ClientSize.Height,true);}
  public void Stop(){stopping=true;child=IntPtr.Zero;var p=process;process=null;bool graceful=false;if(p!=null){try{if(!p.HasExited){p.CloseMainWindow();if(!p.WaitForExit(5000)){p.Kill();p.WaitForExit(1000);Append("Поток завершён принудительно. Если шла запись, проверьте файл перед использованием.");}}graceful=p.HasExited&&p.ExitCode==0;}catch{}p.Dispose();}if(recordingFile!=null){string file=recordingFile;recordingFile=null;try{if(!System.IO.File.Exists(file)||new System.IO.FileInfo(file).Length==0){if(System.IO.File.Exists(file))System.IO.File.Delete(file);RecordingResult="Запись не началась. Откройте журнал потока.";}else RecordingResult=(graceful?"Запись сохранена: ":"Запись прервана; проверьте файл: ")+file;}catch{RecordingResult="Проверьте файл записи: "+file;}if(RecordingStatusChanged!=null)RecordingStatusChanged(RecordingResult);}}
  protected override void Dispose(bool disposing){if(disposing)Stop();base.Dispose(disposing);}
 }
}
