using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
namespace XiaomiDesk {
 public class Device { public string Serial,State,Model; public override string ToString(){return Model+" · "+State;} }
 public static class Core {
  public static string InstallFailure(string message){if(message==null||message.IndexOf("INSTALL_FAILED_USER_RESTRICTED",StringComparison.OrdinalIgnoreCase)<0)return message;return "Xiaomi заблокировал установку помощника.\n\nНа телефоне: Настройки → Расширенные настройки → Для разработчиков → Установка через USB. Разрешите её, если этот пункт доступен, затем повторите установку с разблокированным экраном и подтвердите запрос на Xiaomi.\n\nДругой способ: перенесите phone/XiaomiDesk.apk на телефон и откройте файл для ручной установки. После установки нажмите «Открыть помощник» в программе. Если телефон управляется организацией, ограничение может задаваться её политикой.\n\nСообщение телефона:\n"+message;}
  public static List<Device> ParseDevices(string s){
   var list=new List<Device>();
   foreach(string line in s.Split('\n')){var m=Regex.Match(line.Trim(),@"^(\S+)\s+(device|unauthorized|offline|recovery|sideload)\b(.*)$");if(!m.Success)continue;var model=Regex.Match(m.Groups[3].Value,@"\bmodel:(\S+)");list.Add(new Device{Serial=m.Groups[1].Value,State=m.Groups[2].Value,Model=model.Success?model.Groups[1].Value.Replace('_',' '):m.Groups[1].Value});}
   return list;
  }
  public static string ShellQuote(string s){if(s==null||s.IndexOfAny(new char[]{'\r','\n','\0'})>=0)throw new ArgumentException("Недопустимые символы");return "'"+s.Replace("'","'\\''")+"'";}
  public static string Arg(string s){
   if(s==null||s.IndexOf('\0')>=0)throw new ArgumentException("Недопустимый аргумент");var b=new StringBuilder("\"");int slashes=0;
   foreach(char c in s){if(c=='\\'){slashes++;continue;}if(c=='\"'){b.Append('\\',slashes*2+1);b.Append(c);}else{b.Append('\\',slashes);b.Append(c);}slashes=0;}
   b.Append('\\',slashes*2);return b.Append('"').ToString();
  }
  public static string PhonePath(string s){
   if(s==null||!(s=="/sdcard"||s.StartsWith("/sdcard/",StringComparison.Ordinal)))throw new ArgumentException("Доступна только общая память /sdcard");ShellQuote(s);var parts=new List<string>();foreach(var p in s.Substring(7).Split('/')){if(p==""||p==".")continue;if(p==".."){if(parts.Count==0)throw new ArgumentException("Выход за общую память запрещён");parts.RemoveAt(parts.Count-1);}else parts.Add(p);}return "/sdcard"+(parts.Count==0?"":"/"+string.Join("/",parts));
  }
  public static string Number(string s){var n=Regex.Replace(s??"",@"[\s()\-]","");if(!Regex.IsMatch(n,@"^\+?[0-9]{1,20}$"))throw new ArgumentException("Введите номер: цифры и необязательный +");return n;}
 }
}
