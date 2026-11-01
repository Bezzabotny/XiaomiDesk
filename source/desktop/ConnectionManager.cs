using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public partial class Hub {
  SettingsStore settingsStore;ConnectionSettings settings;ReconnectEngine reconnectEngine;AdbConnectionTransport connectionTransport;
  bool reconnecting,sessionVerified;int reconnectVersion;DateTime lastAutoAttempt=DateTime.MinValue;Label savedInfo,wifiFeedback;CheckBox autoCheck;
  void InitializeConnection(string settingsFile){
   settingsStore=new SettingsStore(settingsFile??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"XiaomiDesk","connection.json"));
   settings=settingsStore.Load();bridge=new Bridge(settings.BridgePort,null);bridge.Start();
   connectionTransport=new AdbConnectionTransport(adb);reconnectEngine=new ReconnectEngine(connectionTransport);
  }
  async Task PollConnection(){
   if(busy||refreshing||reconnecting||closing)return;
   if(!settings.AutoConnect&&adb.Serial==null){state.Text="Автоподключение выключено";helper.Text="Откройте «Подключение» и подключитесь вручную, когда понадобится.";return;}
   await RefreshDevices();
   if(!closing&&!busy&&adb.Serial==null&&settings.AutoConnect&&!string.IsNullOrEmpty(settings.Identity)&&(DateTime.UtcNow-lastAutoAttempt).TotalSeconds>=20)await ReconnectSaved(false);
  }
  async Task<string> PreparePhoneSession(){
   Need();string serial=adb.Serial;string identity=await connectionTransport.Identity(serial);
   if(closing||serial!=adb.Serial)throw new OperationCanceledException();
   if(string.IsNullOrEmpty(identity))throw new Exception("Телефон ответил без постоянного идентификатора. Повторите подключение.");
   if(identity==settings.Identity){string token=SettingsStore.ReadToken(settings);if(token!=null&&token!=bridge.Token)bridge.UseToken(token);}
   sessionVerified=true;
   return identity;
  }
  async Task RememberCurrent(bool notify){
   string identity=await PreparePhoneSession();var d=devices.SelectedItem as Device;
   if(settings.Identity!=identity){settings.LastEndpoint="";settings.ServiceInstance="";}reconnectVersion++;
   settings.Identity=identity;settings.Name=d!=null?d.Model:"Xiaomi";settings.LastSerial=adb.Serial;
   try{settings.LastEndpoint=WirelessDiscovery.Endpoint(adb.Serial);}catch(ArgumentException){if(adb.Serial.Contains("._adb-tls-connect"))settings.ServiceInstance=adb.Serial.Split('.')[0];}
   settings.BridgePort=bridge.Port;settings.ProtectedToken=SettingsStore.ProtectToken(bridge.Token);settingsStore.Save(settings);UpdateSavedInfo();
   if(notify)SetWifiFeedback("Телефон сохранён: "+settings.Name+". Автоподключение "+(settings.AutoConnect?"включено.":"выключено."));
  }
  void UpdateSavedInfo(){if(savedInfo!=null&&!savedInfo.IsDisposed)savedInfo.Text=string.IsNullOrEmpty(settings.Identity)?"Телефон ещё не сохранён. Сначала выполните шаги ниже.":"Сохранён: "+settings.Name+"\n"+(string.IsNullOrEmpty(settings.LastEndpoint)?"Адрес будет найден автоматически в общей сети.":"Последний адрес: "+settings.LastEndpoint);}
  void SetWifiFeedback(string text){operation.Text=text;if(wifiFeedback!=null&&!wifiFeedback.IsDisposed)wifiFeedback.Text=text;}
  async Task ReconnectSaved(bool manual){
   if(reconnecting||closing)return;if(string.IsNullOrEmpty(settings.Identity)){if(manual)throw new Exception("Сначала подключите телефон по шагам ниже и нажмите «Подключить и запомнить».");return;}
   if(!manual&&!settings.AutoConnect)return;int requestVersion=reconnectVersion;reconnecting=true;lastAutoAttempt=DateTime.UtcNow;bool ownsBusy=false;
   try{
    SetWifiFeedback("Ищу сохранённый телефон в вашей сети…");
    var result=await reconnectEngine.Find(settings,manual,delegate{return closing||requestVersion!=reconnectVersion||(!manual&&busy);});
    if(closing||requestVersion!=reconnectVersion||(!manual&&(busy||!settings.AutoConnect)))return;
    if(result==null){SetWifiFeedback("Телефон не найден. Проверьте общую Wi-Fi-сеть и включите «Беспроводную отладку» на Xiaomi.");return;}
    if(!manual){busy=true;ownsBusy=true;sidebar.Enabled=false;devices.Enabled=false;}
    refreshing=true;try{int index=-1;for(int i=0;i<devices.Items.Count;i++)if(((Device)devices.Items[i]).Serial==result.Device.Serial){index=i;break;}if(index<0){devices.Items.Add(result.Device);index=devices.Items.Count-1;}devices.SelectedIndex=index;SwitchSerial(result.Device.Serial);}finally{refreshing=false;}
    string token=SettingsStore.ReadToken(settings);if(token!=null)bridge.UseToken(token);
    if(!string.IsNullOrEmpty(result.Endpoint))settings.LastEndpoint=result.Endpoint;if(!string.IsNullOrEmpty(result.ServiceInstance))settings.ServiceInstance=result.ServiceInstance;
    await RememberCurrent(false);UpdateState();
    try{await Configure();SetWifiFeedback("Подключён сохранённый телефон: "+settings.Name+". Можно открыть файлы или экран.");}
    catch(Exception e){if(closing)return;SetWifiFeedback("Телефон подключён. Помощник пока не отвечает — нажмите «Открыть помощник» и проверьте экран Xiaomi.");if(manual)MessageBox.Show(this,e.Message,"Помощник XiaomiDesk",MessageBoxButtons.OK,MessageBoxIcon.Information);}
   }catch(Exception e){if(!closing){SetWifiFeedback("Автоподключение пока не выполнено: "+Short(e.Message,110));if(manual)throw;}}
   finally{reconnecting=false;if(ownsBusy){busy=false;if(!closing){sidebar.Enabled=true;devices.Enabled=true;}}}
  }
  Task ForgetSaved(){
   reconnectVersion++;bool auto=settings.AutoConnect,closeToTray=settings.CloseToTray;settings=new ConnectionSettings{AutoConnect=auto,CloseToTray=closeToTray,BridgePort=bridge.Port};settingsStore.Save(settings);SwitchSerial(null);bridge.RotateToken();setupDone=false;
   refreshing=true;try{devices.SelectedIndex=-1;}finally{refreshing=false;}UpdateState();UpdateSavedInfo();SetWifiFeedback("Телефон забыт в XiaomiDesk. Для нового подключения пройдите шаги ниже.");return Task.FromResult(0);
  }
  async Task PairWifi(string address,string code){
   string endpoint=WirelessDiscovery.Endpoint(address);if(!System.Text.RegularExpressions.Regex.IsMatch(code??"",@"^\d{6}$"))throw new Exception("Введите шесть цифр из окна «Сопряжение с помощью кода» на Xiaomi.");
   string result=await adb.RunTimeout(false,30000,"pair",endpoint,code);
   if(result.IndexOf("Successfully paired",StringComparison.OrdinalIgnoreCase)<0)throw new Exception("Сопряжение не подтверждено. Откройте новое окно с кодом на телефоне и повторите.\n\n"+result);
   SetWifiFeedback("Шаг 2 выполнен: устройства сопряжены. Закройте окно с кодом на Xiaomi и выполните шаг 3.");
  }
  async Task ConnectAndRemember(string address){
   string endpoint=WirelessDiscovery.Endpoint(address);string result=await adb.RunTimeout(false,7000,"connect",endpoint);var found=Core.ParseDevices(await adb.Run(false,"devices","-l"));
   var device=found.FirstOrDefault(d=>d.Serial==endpoint&&d.State=="device");
   if(device==null){string mdns="";try{mdns=await connectionTransport.Discover();}catch{}var service=WirelessDiscovery.Parse(mdns).FirstOrDefault(s=>s.Endpoint==endpoint);if(service!=null)device=found.FirstOrDefault(d=>d.State=="device"&&(d.Serial==service.Instance+"._adb-tls-connect._tcp"||d.Serial==service.Instance+"._adb-tls-connect._tcp."));}
   if(device==null)throw new Exception("Телефон не подключился. Для шага 3 нужен адрес с главного экрана «Беспроводная отладка», а не из окна с кодом.\n\n"+result);
   refreshing=true;try{devices.Items.Clear();foreach(var d in found)devices.Items.Add(d);devices.SelectedIndex=found.IndexOf(device);SwitchSerial(device.Serial);}finally{refreshing=false;}
   await RememberCurrent(false);settings.LastEndpoint=endpoint;settingsStore.Save(settings);UpdateState();UpdateSavedInfo();
   SetWifiFeedback("Готово: телефон подключён по Wi-Fi и сохранён. Кабель не нужен. Для уведомлений нажмите «Открыть помощник».");
  }
  static string Endpoint(string text){return WirelessDiscovery.Endpoint(text);}
 }
}
