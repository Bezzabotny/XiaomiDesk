using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using XiaomiDesk;
class ConnectionTests {
 static int fails,passed;
 static void Test(string name,Action work){try{work();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){fails++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void Check(bool value){if(!value)throw new Exception("assertion failed");}
 static void MainTest(string root){
 Test("preferences and encrypted helper credential survive restart",delegate{string path=Path.Combine(root,"settings.json");var store=new SettingsStore(path);var fresh=store.Load();Check(fresh.AutoConnect&&fresh.Identity=="");var s=new ConnectionSettings{AutoConnect=false,Identity="serial:ABC",Name="Мой Xiaomi",LastEndpoint="192.168.1.7:38888",BridgePort=32123,ProtectedToken=SettingsStore.ProtectToken(new string('a',64))};store.Save(s);var again=new SettingsStore(path).Load();Check(!again.AutoConnect&&again.Identity=="serial:ABC"&&again.Name=="Мой Xiaomi"&&again.BridgePort==32123);Check(SettingsStore.ReadToken(again)==new string('a',64));Check(!File.ReadAllText(path).Contains(new string('a',64)));});
 Test("broken settings safely fall back and explain why",delegate{string path=Path.Combine(root,"bad.json");File.WriteAllText(path,"{broken");var store=new SettingsStore(path);var s=store.Load();Check(s.Identity==""&&store.Warning.Length>0);});
 Test("helper bridge restores chosen port and credential after restart",delegate{string key=new string('b',64);int port;using(var first=new Bridge(0,key)){first.Start();port=first.Port;}using(var second=new Bridge(port,key)){second.Start();Check(second.Port==port&&second.Token==key);}});
 Test("busy helper port falls back without losing credential",delegate{string key=new string('c',64);using(var first=new Bridge()){first.Start();using(var second=new Bridge(first.Port,key)){second.Start();Check(second.Port!=first.Port&&second.Token==key);}}});
 Test("mDNS uses connection services and follows saved serial across port changes",delegate{var s=new ConnectionSettings{Identity="serial:ABC",LastEndpoint="192.168.1.7:30000"};var found=WirelessDiscovery.Candidates(s,"List of discovered mdns services\nadb-OTHER-q _adb-tls-connect._tcp 192.168.1.9:40000\nadb-ABC-new _adb-tls-pairing._tcp 192.168.1.7:40001\nadb-ABC-new _adb-tls-connect._tcp 192.168.1.8:40002\nbad _adb-tls-connect._tcp 999.1.1.1:0");Check(found.Count==2&&found[0].Endpoint=="192.168.1.8:40002"&&found[1].Endpoint=="192.168.1.7:30000");});
 Test("wireless serial labels include mDNS aliases and IP endpoints",delegate{Check(WirelessDiscovery.IsWirelessSerial("adb-ABC-random._adb-tls-connect._tcp"));Check(WirelessDiscovery.IsWirelessSerial("192.168.1.2:40000"));Check(!WirelessDiscovery.IsWirelessSerial("ABC123"));});
 Test("disabled autoconnect performs no device operations",delegate{var fake=new PhoneTransport{ThrowIfUsed=true};var result=new ReconnectEngine(fake).Find(new ConnectionSettings{AutoConnect=false,Identity="serial:ABC"},false,delegate{return false;}).GetAwaiter().GetResult();Check(result==null);});
 Test("reconnect finds saved phone after address change, ignores another phone",delegate{var fake=new PhoneTransport();fake.Connected.Add(new Device{Serial="other",State="device"});fake.Keys["other"]="serial:OTHER";fake.Discovery="adb-ABC-new _adb-tls-connect._tcp 192.168.1.8:40002";fake.ArrivalEndpoint="192.168.1.8:40002";fake.ArrivalIdentity="serial:ABC";var s=new ConnectionSettings{Identity="serial:ABC",LastEndpoint="192.168.1.7:30000"};var r=new ReconnectEngine(fake).Find(s,false,delegate{return false;}).GetAwaiter().GetResult();Check(r!=null&&r.Device.Serial=="192.168.1.8:40002"&&r.Endpoint=="192.168.1.8:40002");});
 Test("an IP reused by another trusted phone is never selected",delegate{var fake=new PhoneTransport{ArrivalEndpoint="192.168.1.7:30000",ArrivalIdentity="serial:OTHER"};var r=new ReconnectEngine(fake).Find(new ConnectionSettings{Identity="serial:ABC",LastEndpoint="192.168.1.7:30000"},true,delegate{return false;}).GetAwaiter().GetResult();Check(r==null);});
 Test("opt-out during discovery prevents connection",delegate{var s=new ConnectionSettings{Identity="serial:ABC",LastEndpoint="192.168.1.7:30000"};var fake=new PhoneTransport{BeforeDiscovery=delegate{s.AutoConnect=false;},ThrowOnConnect=true};var r=new ReconnectEngine(fake).Find(s,false,delegate{return false;}).GetAwaiter().GetResult();Check(r==null);});
 Test("cancellation does not return an already connected saved phone",delegate{var fake=new PhoneTransport{ThrowIfUsed=true};var r=new ReconnectEngine(fake).Find(new ConnectionSettings{Identity="serial:ABC"},true,delegate{return true;}).GetAwaiter().GetResult();Check(r==null);});
 }
 static int Main(string[] args){Directory.CreateDirectory(args[0]);MainTest(args[0]);Console.WriteLine(passed+" passed, "+fails+" failed");return fails==0?0:1;}
 class PhoneTransport:IConnectionTransport {
  public bool ThrowIfUsed,ThrowOnConnect;public string Discovery="",ArrivalEndpoint="",ArrivalIdentity="";public Action BeforeDiscovery;
  public List<Device> Connected=new List<Device>();public Dictionary<string,string> Keys=new Dictionary<string,string>();
  void Guard(){if(ThrowIfUsed)throw new Exception("Unexpected device operation");}
  public Task<List<Device>> Devices(){Guard();return Task.FromResult(new List<Device>(Connected));}
  public Task<string> Discover(){Guard();if(BeforeDiscovery!=null)BeforeDiscovery();return Task.FromResult(Discovery);}
  public Task Connect(string endpoint){Guard();if(ThrowOnConnect)throw new Exception("Connect after opt-out");if(endpoint==ArrivalEndpoint){Connected.Add(new Device{Serial=endpoint,State="device",Model="Xiaomi"});Keys[endpoint]=ArrivalIdentity;}return Task.FromResult(0);}
  public Task<string> Identity(string serial){Guard();return Task.FromResult(Keys.ContainsKey(serial)?Keys[serial]:"");}
 }
}
