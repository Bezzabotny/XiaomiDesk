using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using XiaomiDesk;
class TransferTests {
 static int failures;static string root;
 static void Test(string n,Action a){try{a();Console.WriteLine("PASS "+n);}catch(Exception e){failures++;Console.WriteLine("FAIL "+n+": "+e.Message);}}
 static void Check(bool b){if(!b)throw new Exception("assertion failed");}
 static int Main(string[] args){root=args[0];Directory.CreateDirectory(root);
 Test("upload reports observed progress and promotes completed file",delegate{var t=new DiskPhone(root);string local=Path.Combine(root,"source.bin");byte[] source=new byte[1024*1024];for(int i=0;i<source.Length;i++)source[i]=(byte)(i%251);File.WriteAllBytes(local,source);var r=new TransferRecord{Direction="Upload",LocalPath=local,RemotePath="/sdcard/Download/file.bin"};bool intermediate=false;new TransferEngine(t).Execute(r,p=>{if(p.Bytes>0&&p.Bytes<p.Total&&p.BytesPerSecond>0)intermediate=true;},CancellationToken.None).GetAwaiter().GetResult();Check(r.Status=="Успешно"&&t.File(r.RemotePath).Length==1024*1024&&intermediate&&File.ReadAllBytes(t.PathFor(r.RemotePath)).SequenceEqual(source));Check(Directory.GetFiles(t.Root,"*.part").Length==0);});
 Test("cancelled download keeps existing destination and removes owned partial",delegate{var t=new DiskPhone(root);string local=Path.Combine(root,"existing.txt");File.WriteAllText(local,"original");File.WriteAllBytes(t.PathFor("/sdcard/large.bin"),new byte[2*1024*1024]);var r=new TransferRecord{Direction="Download",LocalPath=local,RemotePath="/sdcard/large.bin",OverwriteApproved=true};using(var c=new CancellationTokenSource()){c.CancelAfter(150);try{new TransferEngine(t).Execute(r,p=>{},c.Token).GetAwaiter().GetResult();throw new Exception("not cancelled");}catch(OperationCanceledException){}Check(File.ReadAllText(local)=="original"&&r.Status=="Отменено");Check(Directory.GetFiles(root,"*.part").Length==0);}});
 Test("upload refuses existing remote file and preserves its data",delegate{var t=new DiskPhone(root);File.WriteAllText(t.PathFor("/sdcard/existing.txt"),"phone-original");string local=Path.Combine(root,"new.txt");File.WriteAllText(local,"new");var r=new TransferRecord{Direction="Upload",LocalPath=local,RemotePath="/sdcard/existing.txt"};try{new TransferEngine(t).Execute(r,p=>{},CancellationToken.None).GetAwaiter().GetResult();throw new Exception("overwrite allowed");}catch(IOException){}Check(File.ReadAllText(t.PathFor(r.RemotePath))=="phone-original"&&r.Status=="Ошибка");});
 Test("failed transfer records error and never promotes partial file",delegate{var t=new DiskPhone(root){Fail=true};string local=Path.Combine(root,"bad.bin");File.WriteAllBytes(local,new byte[300000]);var r=new TransferRecord{Direction="Upload",LocalPath=local,RemotePath="/sdcard/fail.bin"};try{new TransferEngine(t).Execute(r,p=>{},CancellationToken.None).GetAwaiter().GetResult();throw new Exception("failure hidden");}catch(IOException){}Check(r.Status=="Ошибка"&&r.Error.Length>0&&!System.IO.File.Exists(t.PathFor(r.RemotePath)));});
 Test("history is bounded and unfinished transfers become interrupted",delegate{var h=new TransferHistory(Path.Combine(root,"history.json"));var records=new List<TransferRecord>();for(int i=0;i<130;i++)records.Add(new TransferRecord{Status=i==0?"Выполняется":"Успешно",LocalPath="файл.txt",Direction="Upload"});h.Save(records);var loaded=new TransferHistory(Path.Combine(root,"history.json")).Load();Check(loaded.Count==100&&loaded[0].Status=="Прервано"&&loaded[0].LocalPath=="файл.txt");});
 Test("download never replaces a local file without approval",delegate{var t=new DiskPhone(root);string local=Path.Combine(root,"keep.txt");File.WriteAllText(local,"keep");File.WriteAllText(t.PathFor("/sdcard/new.txt"),"replace");var r=new TransferRecord{Direction="Download",LocalPath=local,RemotePath="/sdcard/new.txt"};try{new TransferEngine(t).Execute(r,p=>{},CancellationToken.None).GetAwaiter().GetResult();throw new Exception("unapproved replacement allowed");}catch(IOException){}Check(File.ReadAllText(local)=="keep"&&r.Status=="Ошибка");});
 return failures==0?0:1;}
 class DiskPhone:ITransferTransport {
  static int nextId;public string Root;public bool Fail;public DiskPhone(string parent){Root=Path.Combine(parent,(++nextId).ToString("D2"));Directory.CreateDirectory(Root);}
  public string PathFor(string remote){return Path.Combine(Root,remote.Replace('/','_'));}
  public FileInfo File(string p){return new FileInfo(PathFor(p));}
  async Task Copy(string a,string b,CancellationToken token){using(var input=System.IO.File.OpenRead(a))using(var output=new FileStream(b,FileMode.CreateNew,FileAccess.Write,FileShare.ReadWrite)){byte[] buffer=new byte[65536];int n;while((n=await input.ReadAsync(buffer,0,buffer.Length,token))>0){await output.WriteAsync(buffer,0,n,token);await output.FlushAsync(token);if(Fail)throw new IOException("connection lost");await Task.Delay(40,token);}}}
  public Task Push(string l,string r,CancellationToken c){return Copy(l,PathFor(r),c);}
  public Task Pull(string r,string l,CancellationToken c){return Copy(PathFor(r),l,c);}
  public Task<long> RemoteSize(string r,CancellationToken c){return Task.FromResult(System.IO.File.Exists(PathFor(r))?new FileInfo(PathFor(r)).Length:-1L);}
  public Task<bool> Exists(string r,CancellationToken c){return Task.FromResult(System.IO.File.Exists(PathFor(r)));}
  public Task Move(string a,string b,CancellationToken c){c.ThrowIfCancellationRequested();System.IO.File.Move(PathFor(a),PathFor(b));return Task.FromResult(0);}
  public Task Remove(string r){if(System.IO.File.Exists(PathFor(r)))System.IO.File.Delete(PathFor(r));return Task.FromResult(0);}
 }
}
