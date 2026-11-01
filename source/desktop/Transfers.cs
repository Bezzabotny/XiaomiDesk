using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
namespace XiaomiDesk {
 public class TransferRecord {
  public string Id=Guid.NewGuid().ToString("N"),Direction,LocalPath,RemotePath,DeviceIdentity,Status,Error="",TemporaryPath="",StartedUtc=DateTime.UtcNow.ToString("o");public long Bytes;public bool OverwriteApproved;
 }
 public class TransferProgress {public long Bytes,Total;public double BytesPerSecond;public int Percent {get{return Total>0?(int)Math.Min(99,Bytes*100.0/Total):0;}}}
 public interface ITransferTransport {
  Task Push(string local,string remote,CancellationToken token);
  Task Pull(string remote,string local,CancellationToken token);
  Task<long> RemoteSize(string remote,CancellationToken token);
  Task<bool> Exists(string remote,CancellationToken token);
  Task Move(string source,string destination,CancellationToken token);
  Task Remove(string remote);
 }
 public class TransferEngine {
  readonly ITransferTransport transport;
  public TransferEngine(ITransferTransport transport){this.transport=transport;}
  public async Task Execute(TransferRecord record,Action<TransferProgress> progress,CancellationToken token){
   bool upload=record.Direction=="Upload";string temp=null;Exception failure=null;var watch=Stopwatch.StartNew();record.Status="Выполняется";record.Error="";record.Bytes=0;
   try{
    token.ThrowIfCancellationRequested();string target=Core.PhonePath(record.RemotePath);if(record.Direction!="Upload"&&record.Direction!="Download")throw new ArgumentException("Неизвестное направление передачи");
    long total;if(upload){if(!File.Exists(record.LocalPath))throw new FileNotFoundException("Исходный файл не найден",record.LocalPath);if(await transport.Exists(target,token))throw new IOException("Файл с таким именем уже есть на телефоне. Выберите другое имя.");total=new FileInfo(record.LocalPath).Length;temp=target+".xiaomidesk-"+Guid.NewGuid().ToString("N")+".part";}
    else{if(File.Exists(record.LocalPath)&&!record.OverwriteApproved)throw new IOException("Замена файла на ноутбуке не подтверждена.");total=await transport.RemoteSize(target,token);temp=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(record.LocalPath)),".xiaomidesk-"+Guid.NewGuid().ToString("N")+".part");}
    record.TemporaryPath=temp;progress(new TransferProgress{Bytes=0,Total=total});Task copy=upload?transport.Push(record.LocalPath,temp,token):transport.Pull(target,temp,token);
    while(!copy.IsCompleted){await Task.WhenAny(copy,Task.Delay(250));if(copy.IsCompleted)break;long bytes=0;try{bytes=upload?await transport.RemoteSize(temp,token):File.Exists(temp)?new FileInfo(temp).Length:0;}catch{}record.Bytes=Math.Max(record.Bytes,Math.Max(0,bytes));progress(new TransferProgress{Bytes=record.Bytes,Total=total,BytesPerSecond=record.Bytes/Math.Max(0.1,watch.Elapsed.TotalSeconds)});}
    await copy;token.ThrowIfCancellationRequested();
    if(upload){record.Bytes=Math.Max(total,await transport.RemoteSize(temp,token));await transport.Move(temp,target,token);}else{record.Bytes=new FileInfo(temp).Length;if(File.Exists(record.LocalPath)){if(!record.OverwriteApproved)throw new IOException("На ноутбуке появился файл с таким именем. Выберите новое имя.");File.Replace(temp,record.LocalPath,null);}else File.Move(temp,record.LocalPath);}
    record.Status="Успешно";progress(new TransferProgress{Bytes=record.Bytes,Total=record.Bytes,BytesPerSecond=record.Bytes/Math.Max(0.1,watch.Elapsed.TotalSeconds)});
   }catch(OperationCanceledException e){record.Status="Отменено";failure=e;}
   catch(Exception e){record.Status="Ошибка";record.Error=e.Message;failure=e;}
   if(temp!=null){try{if(upload)await transport.Remove(temp);else if(File.Exists(temp))File.Delete(temp);record.TemporaryPath="";}catch{record.Error+=(record.Error.Length>0?"\n":"")+"Временный файл может остаться: "+temp;}}
   if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
  }
 }
 public class TransferHistory {
  readonly string path;
  public TransferHistory(string path){this.path=path;}
  public List<TransferRecord> Load(){try{if(!File.Exists(path)||new FileInfo(path).Length>2097152)return new List<TransferRecord>();var list=new JavaScriptSerializer().Deserialize<List<TransferRecord>>(File.ReadAllText(path,Encoding.UTF8))??new List<TransferRecord>();list=list.Where(r=>r!=null).Take(100).ToList();foreach(var r in list)if(r.Status=="Выполняется"||r.Status=="Ожидает")r.Status="Прервано";return list;}catch{return new List<TransferRecord>();}}
  public void Save(List<TransferRecord> records){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(records.Take(100).ToList()),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
 }
}
