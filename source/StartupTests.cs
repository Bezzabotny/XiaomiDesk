using System;
using XiaomiDesk;
class StartupTests {
 class MemoryStore:IStartupStore {public string Value;public string Read(){return Value;}public void Write(string value){Value=value;}public void Delete(){Value=null;}}
 static int Main(){string path=@"C:\Тестовая папка\XiaomiDesk.exe";var store=new MemoryStore();var registration=new StartupRegistration(store);if(registration.Enabled)throw new Exception("Enabled by default");registration.Set(true,path);if(store.Value!="\"C:\\Тестовая папка\\XiaomiDesk.exe\" --background")throw new Exception("Broken startup quoting");if(!registration.Enabled||!registration.IsCurrent(path))throw new Exception("Registration not recognized");registration.Set(false,path);if(registration.Enabled||store.Value!=null)throw new Exception("Autostart not disabled");Console.WriteLine("PASS quoted Unicode startup path, explicit opt-in, opt-out");return 0;}
}
