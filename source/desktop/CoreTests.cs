using System;
using XiaomiDesk;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
class CoreTests {
 static int failures=0, passed=0;
 static void Test(string n,Action a){try{a();Console.WriteLine("PASS "+n);passed++;}catch(Exception e){Console.WriteLine("FAIL "+n+": "+e.Message);failures++;}}
 static void Eq(object a,object b){if(!object.Equals(a,b))throw new Exception("expected ["+b+"] got ["+a+"]");}
 static void Reject(Action a){try{a();}catch(ArgumentException){return;}throw new Exception("unsafe input accepted");}
 static int Main(string[] args){
 Test("ignore adb banner and preserve unauthorized devices",delegate{var d=Core.ParseDevices("* daemon started successfully *\r\nList of devices attached\r\nABC device product:x model:Xiaomi_17_Ultra device:x transport_id:1\r\nDEF unauthorized\r\nGHI offline\r\n");Eq(d.Count,3);Eq(d[0].Model,"Xiaomi 17 Ultra");Eq(d[1].State,"unauthorized");Eq(d[2].Serial,"GHI");});
 Test("shell paths cannot interpolate quotes or substitutions",delegate{Eq(Core.ShellQuote("a'b $(id)"),"'a'\\''b $(id)'");Reject(delegate{Core.ShellQuote("a\nb");});});
 Test("Windows quoting escapes literal quote and trailing slash",delegate{Eq(Core.Arg("a\"b"),"\"a\\\"b\"");Eq(Core.Arg("C:\\with space\\"),"\"C:\\with space\\\\\"");});
 Test("restrict browsing to shared storage and resolve dot segments",delegate{Eq(Core.PhonePath("/sdcard/Download/../DCIM"),"/sdcard/DCIM");Eq(Core.PhonePath("/sdcard"),"/sdcard");Reject(delegate{Core.PhonePath("/sdcard/../../data");});Reject(delegate{Core.PhonePath("/data/system");});});
 Test("dial cannot inject URI or command syntax",delegate{Eq(Core.Number("+7 (999) 123-45-67"),"+79991234567");Reject(delegate{Core.Number("123;reboot");});Reject(delegate{Core.Number("");});});
 Test("bridge accepts only authenticated bounded valid events",delegate{using(var b=new Bridge()){
 b.Start();PhoneEvent received=null;var done=new ManualResetEvent(false);b.Received+=delegate(PhoneEvent e){received=e;done.Set();};
 Eq(Post(b,"wrong","{\"kind\":\"clipboard\",\"text\":\"bad\"}"),403);Eq(received,null);
 Eq(Post(b,b.Token,"not json"),400);
 Eq(Post(b,b.Token,"{\"kind\":\"unknown\"}"),400);
 Eq(Post(b,b.Token,new string('x',70000)),413);
 Eq(Post(b,b.Token,"{\"kind\":\"notification\",\"app\":\"test.app\",\"title\":\"Привет\",\"text\":\"Сообщение\"}"),200);
 if(!done.WaitOne(2000))throw new Exception("event not delivered");Eq(received.title,"Привет");Eq(received.text,"Сообщение");
 }});
 Test("rotating session rejects previous phone and drops old clipboard",delegate{using(var b=new Bridge()){b.Start();string old=b.Token;b.PutClipboard("private");b.RotateToken();if(old==b.Token)throw new Exception("token reused");Eq(Post(b,old,"{\"kind\":\"hello\"}"),403);Eq(Get(b,b.Token),"410:");Eq(Post(b,b.Token,"{\"kind\":\"hello\"}"),200);}});
 Test("long Russian clipboard travels outside command arguments once",delegate{using(var b=new Bridge()){b.Start();string s=new string('Я',16000);b.PutClipboard(s);Eq(Get(b,"wrong"),"403:");Eq(Get(b,b.Token),"200:"+s);Eq(Get(b,b.Token),"410:");}});
 Test("closing stops a running transfer process and blocks later commands",delegate{string marker=Path.Combine(args[0],"started-"+Guid.NewGuid().ToString("N")+".txt");var runner=new Adb(args[0]);Process child=null;try{var task=runner.Run(false,marker);for(int i=0;i<50&&!File.Exists(marker);i++)Thread.Sleep(50);if(!File.Exists(marker))throw new Exception("fixture process never started");child=Process.GetProcessById(int.Parse(File.ReadAllText(marker)));runner.CancelAll();if(!child.WaitForExit(3000))throw new Exception("transfer process survived cancellation");try{task.GetAwaiter().GetResult();throw new Exception("cancelled transfer reported success");}catch(OperationCanceledException){}try{runner.Run(false,marker).GetAwaiter().GetResult();throw new Exception("commands allowed after close");}catch(OperationCanceledException){}}finally{if(child!=null){try{if(!child.HasExited)child.Kill();}catch{}child.Dispose();}if(File.Exists(marker))File.Delete(marker);}});
 Test("restricted USB install gives actionable Xiaomi guidance and preserves raw error",delegate{string raw="Failure [INSTALL_FAILED_USER_RESTRICTED: Install canceled by user]";string explanation=Core.InstallFailure(raw);if(!explanation.Contains("Установка через USB")||!explanation.Contains("XiaomiDesk.apk")||!explanation.Contains(raw))throw new Exception("USB restriction guidance missing");Eq(Core.InstallFailure("INSTALL_FAILED_INSUFFICIENT_STORAGE"),"INSTALL_FAILED_INSUFFICIENT_STORAGE");});
 Test("cancelling one transfer leaves ADB available for later operations",delegate{string marker=Path.Combine(args[0],"cancel-"+Guid.NewGuid().ToString("N")+".txt");var runner=new Adb(args[0]);Process child=null;try{using(var c=new CancellationTokenSource()){var task=runner.RunCancellable(null,c.Token,60000,marker);for(int i=0;i<50&&!File.Exists(marker);i++)Thread.Sleep(50);if(!File.Exists(marker))throw new Exception("fixture did not start");child=Process.GetProcessById(int.Parse(File.ReadAllText(marker)));c.Cancel();try{task.GetAwaiter().GetResult();throw new Exception("cancel not reported");}catch(OperationCanceledException){}if(!child.WaitForExit(3000))throw new Exception("cancelled child survived");string devices=runner.Run(false,"devices","-l").GetAwaiter().GetResult();if(!devices.Contains("phone-b"))throw new Exception("runner disabled by transfer cancel");}}finally{runner.CancelAll();if(child!=null)child.Dispose();if(File.Exists(marker))File.Delete(marker);}});
 Console.WriteLine(passed+" passed, "+failures+" failed");return failures==0?0:1;
 }
 static int Post(Bridge b,string token,string body){var r=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+b.Port+"/event");r.Method="POST";r.Proxy=null;r.Timeout=3000;r.Headers["X-XiaomiDesk-Token"]=token;var bytes=Encoding.UTF8.GetBytes(body);r.ContentLength=bytes.Length;using(var s=r.GetRequestStream())s.Write(bytes,0,bytes.Length);try{using(var response=(HttpWebResponse)r.GetResponse())return (int)response.StatusCode;}catch(WebException e){if(e.Response==null)throw;using(var response=(HttpWebResponse)e.Response)return (int)response.StatusCode;}}
 static string Get(Bridge b,string token){var r=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+b.Port+"/clipboard");r.Proxy=null;r.Timeout=3000;r.Headers["X-XiaomiDesk-Token"]=token;HttpWebResponse response;try{response=(HttpWebResponse)r.GetResponse();}catch(WebException e){if(e.Response==null)throw;response=(HttpWebResponse)e.Response;}using(response)using(var reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8))return (int)response.StatusCode+":"+reader.ReadToEnd();}
}
