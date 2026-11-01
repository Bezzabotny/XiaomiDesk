using System;
using System.Globalization;
using System.Text.RegularExpressions;
namespace XiaomiDesk {
 public static class MediaPolicy {
  public static Uri WebUrl(string value){Uri u;if(!Uri.TryCreate((value??"").Trim(),UriKind.Absolute,out u)||(u.Scheme!="http"&&u.Scheme!="https")||string.IsNullOrEmpty(u.Host)||!string.IsNullOrEmpty(u.UserInfo))throw new ArgumentException("Введите ссылку, начинающуюся с https:// или http://, без логина и пароля.");return u;}
  public static string PhoneMediaPath(string value){string p=value??"";if(p.StartsWith("/storage/emulated/0/",StringComparison.Ordinal))p="/sdcard/"+p.Substring(20);if(!p.StartsWith("/sdcard/",StringComparison.Ordinal)||p.Contains("/../")||p.EndsWith("/..")||p.IndexOfAny(new[]{'\r','\n','\0'})>=0)throw new ArgumentException("Этот оригинал недоступен через файловое подключение.");return p;}
  public static string Storage(string value){var lines=(value??"").Trim().Split('\n');if(lines.Length<2)return "Память: сведения недоступны";var fields=Regex.Split(lines[lines.Length-1].Trim(),@"\s+");if(fields.Length<6)return "Память: сведения недоступны";return "Память: "+fields[2]+" занято из "+fields[1]+" · свободно "+fields[3];}
  public static string Battery(string output){var level=Regex.Match(output??"",@"(?m)^\s*level:\s*(\d+)");var scale=Regex.Match(output??"",@"(?m)^\s*scale:\s*(\d+)");if(!level.Success||!scale.Success||scale.Groups[1].Value=="0")return "Батарея: сведения недоступны";double percent=100.0*double.Parse(level.Groups[1].Value,CultureInfo.InvariantCulture)/double.Parse(scale.Groups[1].Value,CultureInfo.InvariantCulture);var temp=Regex.Match(output??"",@"(?m)^\s*temperature:\s*(-?\d+)");bool charging=Regex.IsMatch(output??"",@"(?m)^\s*status:\s*(2|5)\s*$");return Math.Round(percent)+"% · "+(charging?"на зарядке":"от батареи")+(temp.Success?" · "+(double.Parse(temp.Groups[1].Value,CultureInfo.InvariantCulture)/10).ToString("0.0",CultureInfo.InvariantCulture)+" °C":"");}
 }
}
