using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
namespace XiaomiDesk {
 public class MdnsService {public string Instance,Endpoint;}
 public static class WirelessDiscovery {
  public static bool IsWirelessSerial(string serial){return !string.IsNullOrEmpty(serial)&&(serial.Contains(":")||serial.Contains("._adb-tls-connect._tcp"));}
  public static string Endpoint(string input){string s=(input??"").Trim();var m=Regex.Match(s,@"^(\d{1,3}(?:\.\d{1,3}){3}):(\d{1,5})$");int port;byte octet;if(!m.Success||!int.TryParse(m.Groups[2].Value,out port)||port<1||port>65535||m.Groups[1].Value.Split('.').Any(p=>!byte.TryParse(p,out octet)))throw new ArgumentException("Введите IP-адрес и порт с телефона, например 192.168.1.10:37000. Скопируйте адрес целиком, вместе с двоеточием.");return s;}
  public static List<MdnsService> Parse(string output){var list=new List<MdnsService>();foreach(var line in (output??"").Split('\n')){var m=Regex.Match(line.Trim(),@"^(\S+)\s+_adb-tls-connect\._tcp\.?\s+(\S+)$");if(!m.Success)continue;try{list.Add(new MdnsService{Instance=m.Groups[1].Value,Endpoint=Endpoint(m.Groups[2].Value)});}catch(ArgumentException){}}return list;}
  public static List<MdnsService> Candidates(ConnectionSettings s,string output){var list=new List<MdnsService>();string serial=s.Identity.StartsWith("serial:",StringComparison.Ordinal)?s.Identity.Substring(7):"";foreach(var service in Parse(output)){if((serial.Length>0&&service.Instance.StartsWith("adb-"+serial+"-",StringComparison.Ordinal))||(!string.IsNullOrEmpty(s.ServiceInstance)&&service.Instance==s.ServiceInstance))list.Add(service);}try{string saved=Endpoint(s.LastEndpoint);if(!list.Any(e=>e.Endpoint==saved))list.Add(new MdnsService{Endpoint=saved,Instance=""});}catch(ArgumentException){}return list.Take(3).ToList();}
 }
 public interface IConnectionTransport {
  Task<List<Device>> Devices();
  Task<string> Discover();
  Task Connect(string endpoint);
  Task<string> Identity(string serial);
 }
 public class ReconnectResult {public Device Device;public string Endpoint,ServiceInstance;}
 public class ReconnectEngine {
  readonly IConnectionTransport transport;
  public ReconnectEngine(IConnectionTransport transport){this.transport=transport;}
  public async Task<ReconnectResult> Find(ConnectionSettings settings,bool manual,Func<bool> cancelled){
   Func<bool> stop=delegate{return cancelled()||(!manual&&!settings.AutoConnect);};if(stop()||string.IsNullOrEmpty(settings.Identity))return null;
   var connected=await transport.Devices();if(stop())return null;var found=await Match(connected,settings,stop);if(found!=null||stop())return found;
   string services="";try{services=await transport.Discover();}catch{}if(stop())return null;
   foreach(var candidate in WirelessDiscovery.Candidates(settings,services)){
    if(stop())return null;try{await transport.Connect(candidate.Endpoint);if(stop())return null;connected=await transport.Devices();if(stop())return null;found=await Match(connected,settings,stop);if(found!=null){found.Endpoint=candidate.Endpoint;found.ServiceInstance=candidate.Instance;return found;}}catch{if(stop())return null;}
   }
   return null;
  }
  async Task<ReconnectResult> Match(List<Device> devices,ConnectionSettings s,Func<bool> stop){foreach(var d in devices.Where(d=>d.State=="device").OrderBy(d=>d.Serial==s.LastSerial?0:1).Take(10)){if(stop())return null;try{string key=await transport.Identity(d.Serial);if(stop())return null;if(key==s.Identity){string endpoint="";try{endpoint=WirelessDiscovery.Endpoint(d.Serial);}catch(ArgumentException){}return new ReconnectResult{Device=d,Endpoint=endpoint,ServiceInstance=d.Serial.Contains("._adb-tls-connect")?d.Serial.Split('.')[0]:""};}}catch{if(stop())return null;}}return null;}
 }
}
