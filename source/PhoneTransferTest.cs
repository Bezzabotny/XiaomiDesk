using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XiaomiDesk;
class PhoneTransferTest {
 static int Main(string[] args){try{Run(args).GetAwaiter().GetResult();return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
 static async Task Run(string[] args){
  var adb=new Adb(args[0]);string serial=args[1],expected=args[2],work=args[3];Directory.CreateDirectory(work);
  string query=expected.StartsWith("serial:")?"getprop ro.serialno":"settings get secure android_id";string actual=(expected.StartsWith("serial:")?"serial:":"android:")+(await adb.RunForSerial(serial,5000,"shell",query)).Trim();if(actual!=expected)throw new Exception("Selected phone identity mismatch");
  string source=Path.Combine(work,"source.bin"),download=Path.Combine(work,"roundtrip.bin"),remote="/sdcard/Download/XiaomiDesk-check-"+Guid.NewGuid().ToString("N")+".bin";
  byte[] bytes=new byte[2*1024*1024];new Random(20261030).NextBytes(bytes);File.WriteAllBytes(source,bytes);Exception failure=null;
  try{var engine=new TransferEngine(new AdbTransferTransport(adb,serial));var up=new TransferRecord{Direction="Upload",LocalPath=source,RemotePath=remote};await engine.Execute(up,p=>{},CancellationToken.None);Console.WriteLine("PASS actual phone upload: "+up.Bytes+" bytes; owned temporary file promoted");var down=new TransferRecord{Direction="Download",LocalPath=download,RemotePath=remote};await engine.Execute(down,p=>{},CancellationToken.None);if(!File.ReadAllBytes(download).SequenceEqual(bytes))throw new Exception("Round-trip data mismatch");Console.WriteLine("PASS actual phone download: byte-for-byte equality");}
  catch(Exception e){failure=e;}
  try{await adb.RunForSerial(serial,5000,"shell","rm -f -- "+Core.ShellQuote(remote));Console.WriteLine("PASS removed only the generated phone test file");}catch(Exception e){Console.WriteLine("Test file cleanup failed: "+remote);if(failure==null)failure=e;}
  adb.CancelAll();if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
 }
}
