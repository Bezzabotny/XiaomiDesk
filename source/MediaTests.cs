using System;
using XiaomiDesk;
class MediaTests {
 static void Main(){
  if(MediaPolicy.WebUrl("https://example.com/a?q=x").Scheme!="https")throw new Exception("URL");
  foreach(string value in new[]{"file:///C:/Windows","javascript:alert(1)","https://user:pass@example.com","not a url"}){bool failed=false;try{MediaPolicy.WebUrl(value);}catch(ArgumentException){failed=true;}if(!failed)throw new Exception("Unsafe URL: "+value);}
  if(MediaPolicy.PhoneMediaPath("/storage/emulated/0/DCIM/a.jpg")!="/sdcard/DCIM/a.jpg")throw new Exception("Path");
  foreach(string value in new[]{"/data/private.jpg","/sdcard/../data/a","/sdcard/a\nb"}){bool failed=false;try{MediaPolicy.PhoneMediaPath(value);}catch(ArgumentException){failed=true;}if(!failed)throw new Exception("Unsafe path");}
  var battery=MediaPolicy.Battery("  level: 76\n  scale: 100\n  status: 2\n  temperature: 321\n");if(!battery.Contains("76%")||!battery.Contains("32.1"))throw new Exception(battery);
  if(!MediaPolicy.Battery("unavailable").Contains("недоступ"))throw new Exception("Missing battery");
  Console.WriteLine("PASS URL safety, media path confinement, battery and unavailable state");
 }
}
