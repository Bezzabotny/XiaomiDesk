using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
namespace XiaomiDesk {
 public class PhoneEvent {public string kind,app,title,text;[ScriptIgnore]public string SessionToken;}
 public class Bridge:IDisposable {
  public string Token {get;private set;}
  public int Port {get;private set;}
  public event Action<PhoneEvent> Received;
  readonly object sessionLock=new object();string clipboard;
  readonly int preferredPort;
  public void UseToken(string token){if(!Regex.IsMatch(token??"",@"^[a-f0-9]{64}$"))throw new ArgumentException("Некорректный ключ подключения");lock(sessionLock){Token=token;clipboard=null;}}
  public void RotateToken(){var bytes=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(bytes);lock(sessionLock){Token=BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();clipboard=null;}}
  public void PutClipboard(string text){if(text==null||text.Length>16000)throw new ArgumentException("Максимум 16 000 символов");lock(sessionLock){clipboard=text;}}
  TcpListener listener;volatile bool stopped;Semaphore slots=new Semaphore(8,8);
  public Bridge():this(0,null){}
  public Bridge(int port,string token){preferredPort=port>=0&&port<=65535?port:0;if(token==null)RotateToken();else UseToken(token);}
  public void Start(){listener=new TcpListener(IPAddress.Loopback,preferredPort);try{listener.Start(8);}catch(SocketException){if(preferredPort==0)throw;listener.Stop();listener=new TcpListener(IPAddress.Loopback,0);listener.Start(8);}Port=((IPEndPoint)listener.LocalEndpoint).Port;var t=new Thread(Accept);t.IsBackground=true;t.Start();}
  void Accept(){while(!stopped){try{var c=listener.AcceptTcpClient();if(!slots.WaitOne(0)){c.Close();continue;}ThreadPool.QueueUserWorkItem(delegate{try{Handle(c);}catch{}finally{c.Close();slots.Release();}});}catch{if(stopped)return;}}}
  static string ReadLine(Stream s,int max){var b=new List<byte>();while(b.Count<max){int n=s.ReadByte();if(n<0)throw new IOException("EOF");if(n==10)return Encoding.ASCII.GetString(b.ToArray()).TrimEnd('\r');b.Add((byte)n);}throw new IOException("Header too long");}
  static void Reply(Stream s,int status){var data=Encoding.ASCII.GetBytes("HTTP/1.1 "+status+" Result\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");s.Write(data,0,data.Length);}
  void Handle(TcpClient c){
   c.ReceiveTimeout=4000;c.SendTimeout=4000;using(var s=c.GetStream()){
    var request=ReadLine(s,1024);var h=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);int size=0;string line;
    while((line=ReadLine(s,4096))!=""){size+=line.Length;if(size>8192){Reply(s,431);return;}int p=line.IndexOf(':');if(p>0)h[line.Substring(0,p)]=line.Substring(p+1).Trim();}
    string token;h.TryGetValue("X-XiaomiDesk-Token",out token);if(!Same(token,Token)){Reply(s,403);return;}
    if(request=="GET /clipboard HTTP/1.1"||request=="GET /clipboard HTTP/1.0"){
     string text;lock(sessionLock){if(!Same(token,Token)){Reply(s,403);return;}text=clipboard;clipboard=null;}
     if(text==null){Reply(s,410);return;}var body=Encoding.UTF8.GetBytes(text);var head=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: "+body.Length+"\r\nConnection: close\r\n\r\n");s.Write(head,0,head.Length);s.Write(body,0,body.Length);return;
    }
    if(request!="POST /event HTTP/1.1"&&request!="POST /event HTTP/1.0"){Reply(s,404);return;}
    string len;int length;if(h.ContainsKey("Transfer-Encoding")||!h.TryGetValue("Content-Length",out len)||!int.TryParse(len,out length)||length<0){Reply(s,400);return;}if(length>65536){Reply(s,413);return;}
    if(h.ContainsKey("Expect")){var cont=Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");s.Write(cont,0,cont.Length);}
    var bytes=new byte[length];int read=0;while(read<length){int n=s.Read(bytes,read,length-read);if(n==0)throw new IOException();read+=n;}
    PhoneEvent ev;try{ev=new JavaScriptSerializer().Deserialize<PhoneEvent>(Encoding.UTF8.GetString(bytes));if(ev==null||(ev.kind!="notification"&&ev.kind!="clipboard"&&ev.kind!="hello")||TooLong(ev.app,200)||TooLong(ev.title,1000)||TooLong(ev.text,16000)){Reply(s,400);return;}}catch{Reply(s,400);return;}
    if(!Same(token,Token)){Reply(s,403);return;}ev.SessionToken=token;Reply(s,200);var handler=Received;if(handler!=null)handler(ev);
   }
  }
  static bool TooLong(string s,int n){return s!=null&&s.Length>n;}
  static bool Same(string a,string b){if(a==null||a.Length!=b.Length)return false;int diff=0;for(int i=0;i<a.Length;i++)diff|=a[i]^b[i];return diff==0;}
  public void Dispose(){stopped=true;if(listener!=null)listener.Stop();}
 }
}
