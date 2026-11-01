using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
namespace XiaomiDesk {
 public class ConnectionSettings {
  public bool AutoConnect=true;
  public bool CloseToTray=true;
  public string Identity="",Name="",LastSerial="",LastEndpoint="",ServiceInstance="",ProtectedToken="";
  public int BridgePort;
 }
 public class SettingsStore {
  public readonly string FilePath;
  public string Warning="";
  public SettingsStore(string path){FilePath=path;}
  public ConnectionSettings Load(){
   if(!File.Exists(FilePath))return new ConnectionSettings();
   try{if(new FileInfo(FilePath).Length>65536)throw new IOException();var s=new JavaScriptSerializer().Deserialize<ConnectionSettings>(File.ReadAllText(FilePath,Encoding.UTF8));if(s==null||s.BridgePort<0||s.BridgePort>65535)throw new IOException();s.Identity=s.Identity??"";s.Name=s.Name??"";s.LastSerial=s.LastSerial??"";s.LastEndpoint=s.LastEndpoint??"";s.ServiceInstance=s.ServiceInstance??"";s.ProtectedToken=s.ProtectedToken??"";return s;}
   catch{Warning="Не удалось прочитать настройки. Подключите и сохраните телефон заново.";return new ConnectionSettings();}
  }
  public void Save(ConnectionSettings settings){
   Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(settings),new UTF8Encoding(false));if(File.Exists(FilePath))File.Replace(temp,FilePath,null);else File.Move(temp,FilePath);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
  public static string ProtectToken(string token){if(!Regex.IsMatch(token??"",@"^[a-f0-9]{64}$"))throw new ArgumentException("Некорректный ключ подключения");return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(token),null,DataProtectionScope.CurrentUser));}
  public static string ReadToken(ConnectionSettings settings){try{var token=Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(settings.ProtectedToken),null,DataProtectionScope.CurrentUser));return Regex.IsMatch(token,@"^[a-f0-9]{64}$")?token:null;}catch{return null;}}
 }
}
