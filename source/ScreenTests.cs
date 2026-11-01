using System;
using XiaomiDesk;
class ScreenTests {
 static int Main(){int failed=0;Action<string,Action> test=delegate(string n,Action a){try{a();Console.WriteLine("PASS "+n);}catch(Exception e){failed++;Console.WriteLine("FAIL "+n+": "+e.Message);}};
 test("screen uses physical input without read-only control flag",delegate{string s=ScreenOptions.Arguments("phone","screen",false,true);if(!s.Contains("--keyboard=uhid")||!s.Contains("--mouse=uhid")||s.Contains("--no-control"))throw new Exception("UHID controls missing");});
 test("camera remains read only without mouse capture",delegate{string s=ScreenOptions.Arguments("phone","camera",true,true);if(!s.Contains("--no-control")||s.Contains("--mouse=uhid")||s.Contains("--keyboard=uhid"))throw new Exception("camera wrongly captures input");});
 test("SDK remains available as explicit alternative",delegate{string s=ScreenOptions.Arguments("phone","screen",false,false);if(!s.Contains("--keyboard=sdk")||!s.Contains("--mouse=sdk"))throw new Exception("SDK fallback missing");});
 test("permission refusal explains correct input mode",delegate{string s=ScreenOptions.Explain("java.lang.SecurityException: requires INJECT_EVENTS");if(!s.Contains("физической")||!s.Contains("INJECT_EVENTS"))throw new Exception("missing permission diagnosis");});
 test("Windows child-process startup failure has an actionable explanation",delegate{string s=ScreenOptions.Explain("adb devices returned 3221225794");if(!s.Contains("0xC0000142")||!s.Contains("Windows"))throw new Exception("Raw process failure is not explained");});
 return failed==0?0:1;}
}
