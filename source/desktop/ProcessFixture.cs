using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
class ProcessFixture {
 static void Main(string[] args){if(args[0]=="devices"){Thread.Sleep(400);Console.WriteLine("List of devices attached\nphone-b device model:Second_phone");return;}File.WriteAllText(args[0],Process.GetCurrentProcess().Id.ToString());Thread.Sleep(30000);}
}
