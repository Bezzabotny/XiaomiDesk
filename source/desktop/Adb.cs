using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace XiaomiDesk {
 public class Adb {
  readonly object gate=new object();readonly List<Process> active=new List<Process>();bool stopped;
  public void CancelAll(){lock(gate){stopped=true;foreach(var p in active){try{if(!p.HasExited)p.Kill();}catch{}}}}
  public string Serial;
  public readonly string ToolDir;
  public Adb(string root){ToolDir=Path.Combine(root,"tools");}
  public Task<string> Run(bool selected,params string[] args){return RunTimeout(selected,30000,args);}
  public async Task<string> RunTimeout(bool selected,int timeout,params string[] args){
   if(selected&&string.IsNullOrEmpty(Serial))throw new InvalidOperationException("Сначала подключите и выберите телефон.");
   return await RunForSerial(selected?Serial:null,timeout,args);
  }
  public Task<string> RunForSerial(string serial,int timeout,params string[] args){return RunCancellable(serial,CancellationToken.None,timeout,args);}
  public async Task<string> RunCancellable(string serial,CancellationToken token,int timeout,params string[] args){
   token.ThrowIfCancellationRequested();
   string arguments=(!string.IsNullOrEmpty(serial)?"-s "+Core.Arg(serial)+" ":"")+string.Join(" ",args.Select(Core.Arg));
   var info=new ProcessStartInfo(Path.Combine(ToolDir,"adb.exe"),arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
   using(var p=new Process()){
    p.StartInfo=info;lock(gate){if(stopped)throw new OperationCanceledException();p.Start();active.Add(p);}
    var cancellation=token.Register(delegate{try{if(!p.HasExited)p.Kill();}catch{}});
    try{var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();bool exited=await Task.Run(delegate{return p.WaitForExit(timeout);});token.ThrowIfCancellationRequested();lock(gate){if(stopped)throw new OperationCanceledException();}if(!exited){try{p.Kill();}catch{}throw new TimeoutException("Телефон не ответил вовремя. Проверьте сеть или кабель и разрешение на экране Xiaomi.");}string text=await output;string err=await error;if(p.ExitCode!=0)throw new IOException(string.IsNullOrWhiteSpace(err)?text:err);return text;}
    finally{cancellation.Dispose();lock(gate){active.Remove(p);}}
   }
  }
  public Task<string> Shell(string command){return Run(true,"shell",command);}
  public Task<string> Activity(string args){return Shell("am start "+args);}
 }
}
