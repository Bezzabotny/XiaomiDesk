using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace XiaomiDesk {
 public class AdbTransferTransport:ITransferTransport {
  readonly Adb adb;readonly string serial;
  public AdbTransferTransport(Adb adb,string serial){this.adb=adb;this.serial=serial;}
  public async Task Push(string local,string remote,CancellationToken token){await adb.RunCancellable(serial,token,1800000,"push",local,Core.PhonePath(remote));}
  public async Task Pull(string remote,string local,CancellationToken token){await adb.RunCancellable(serial,token,1800000,"pull",Core.PhonePath(remote),local);}
  public async Task<long> RemoteSize(string remote,CancellationToken token){string result=await adb.RunCancellable(serial,token,3500,"shell","stat -c %s -- "+Core.ShellQuote(Core.PhonePath(remote)));long size;return long.TryParse(result.Trim(),out size)?size:-1;}
  public async Task<bool> Exists(string remote,CancellationToken token){string result=await adb.RunCancellable(serial,token,3500,"shell","if [ -e "+Core.ShellQuote(Core.PhonePath(remote))+" ]; then echo exists; else echo free; fi");if(result.Trim()!="exists"&&result.Trim()!="free")throw new IOException(result);return result.Trim()=="exists";}
  public async Task Move(string source,string destination,CancellationToken token){string a=Core.ShellQuote(Core.PhonePath(source)),b=Core.ShellQuote(Core.PhonePath(destination));await adb.RunCancellable(serial,token,5000,"shell","mv -n -- "+a+" "+b+" && if [ -e "+a+" ]; then echo 'Имя уже занято на телефоне'; exit 1; fi");}
  public async Task Remove(string remote){if(!remote.Contains(".xiaomidesk-")||!remote.EndsWith(".part",StringComparison.Ordinal))throw new ArgumentException("Можно удалять только временные файлы передачи");await adb.RunForSerial(serial,3500,"shell","rm -f -- "+Core.ShellQuote(Core.PhonePath(remote)));}
 }
}
