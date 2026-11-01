using System;
using System.IO;
using Microsoft.Win32;
namespace XiaomiDesk {
 public interface IStartupStore {string Read();void Write(string value);void Delete();}
 public class UserStartupStore:IStartupStore {
  const string key=@"Software\Microsoft\Windows\CurrentVersion\Run";
  public string Read(){using(var k=Registry.CurrentUser.OpenSubKey(key,false)){return k==null?null:k.GetValue("XiaomiDesk") as string;}}
  public void Write(string value){using(var k=Registry.CurrentUser.CreateSubKey(key)){k.SetValue("XiaomiDesk",value,RegistryValueKind.String);}}
  public void Delete(){using(var k=Registry.CurrentUser.OpenSubKey(key,true)){if(k!=null)k.DeleteValue("XiaomiDesk",false);}}
 }
 public class StartupRegistration {
  readonly IStartupStore store;
  public StartupRegistration(IStartupStore store){this.store=store;}
  public bool Enabled {get{return !string.IsNullOrEmpty(store.Read());}}
  public static string Command(string executable){return Core.Arg(Path.GetFullPath(executable))+" --background";}
  public bool IsCurrent(string executable){return string.Equals(store.Read(),Command(executable),StringComparison.OrdinalIgnoreCase);}
  public void Set(bool enabled,string executable){if(enabled)store.Write(Command(executable));else store.Delete();}
 }
}
