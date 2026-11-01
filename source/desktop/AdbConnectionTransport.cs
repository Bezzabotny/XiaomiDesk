using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace XiaomiDesk {
 public class AdbConnectionTransport:IConnectionTransport {
  readonly Adb adb;
  public AdbConnectionTransport(Adb adb){this.adb=adb;}
  public async Task<List<Device>> Devices(){return Core.ParseDevices(await adb.RunTimeout(false,5000,"devices","-l"));}
  public Task<string> Discover(){return adb.RunTimeout(false,5000,"mdns","services");}
  public async Task Connect(string endpoint){await adb.RunTimeout(false,5000,"connect",WirelessDiscovery.Endpoint(endpoint));}
  public async Task<string> Identity(string serial){string id=(await adb.RunForSerial(serial,4000,"shell","getprop ro.serialno")).Trim();if(!string.IsNullOrEmpty(id)&&!id.Equals("unknown",StringComparison.OrdinalIgnoreCase))return "serial:"+id;id=(await adb.RunForSerial(serial,4000,"shell","settings get secure android_id")).Trim();return !string.IsNullOrEmpty(id)&&id!="null"?"android:"+id:"";}
 }
}
