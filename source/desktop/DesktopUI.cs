using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public partial class Hub {
  NotifyIcon tray;bool explicitExit,backgroundNotice,exitPending,exitReady;StartupRegistration startupRegistration;bool startInTray;
  void InitializeDesktop(IStartupStore startupStore,bool background){
   startInTray=background;startupRegistration=new StartupRegistration(startupStore??new UserStartupStore());
   var menu=new ContextMenuStrip();menu.Items.Add("Открыть XiaomiDesk",null,delegate{RestoreWindow();});
   menu.Items.Add("Подключить сохранённый телефон",null,async delegate{RestoreWindow();await Use(async delegate{await ReconnectSaved(true);});});
   menu.Items.Add("Передачи",null,delegate{RestoreWindow();ShowPage("Передачи");});menu.Items.Add("Настройки",null,delegate{RestoreWindow();ShowPage("Настройки");});
   menu.Items.Add("Отменить передачу",null,delegate{if(transferCancel!=null)transferCancel.Cancel();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Выход",null,delegate{RequestExit();});
   tray=new NotifyIcon{Icon=SystemIcons.Application,Text="XiaomiDesk — телефон рядом",Visible=true,ContextMenuStrip=menu};tray.DoubleClick+=delegate{RestoreWindow();};
   Resize+=delegate{if(WindowState==FormWindowState.Minimized&&settings.CloseToTray)HideToTray();};
   if(background){ShowInTaskbar=false;WindowState=FormWindowState.Minimized;}
  }
  void HideToTray(){if(closing)return;if(stream!=null)stream.Stop();Hide();ShowInTaskbar=false;if(!backgroundNotice&&tray!=null){backgroundNotice=true;tray.ShowBalloonTip(3500,"XiaomiDesk работает в фоне","Связь и передачи продолжаются. Открыть окно или выйти можно через значок возле часов.",ToolTipIcon.Info);}}
  public void RestoreFromTray(){RestoreWindow();}
  void RestoreWindow(){if(closing||IsDisposed)return;ShowInTaskbar=true;Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Activate();}
  void RequestExit(){explicitExit=true;Close();}
  async void OnHubClosing(object sender,FormClosingEventArgs e){
   if(e.CloseReason==CloseReason.UserClosing&&!explicitExit&&settings.CloseToTray){e.Cancel=true;HideToTray();return;}
   if(e.CloseReason==CloseReason.UserClosing&&!exitReady&&(transferActive||exitPending)){
    e.Cancel=true;if(exitPending)return;exitPending=true;if(transferCancel!=null)transferCancel.Cancel();operation.Text="Завершение передачи и очистка временных файлов…";
    Task cleanup=transferCompletion==null?Task.Delay(0):transferCompletion.Task;await Task.WhenAny(cleanup,Task.Delay(12000));if(closing||IsDisposed)return;exitReady=true;BeginInvoke(new Action(Close));return;
   }
   ShutdownResources();
  }
  void ShutdownResources(){if(closing)return;closing=true;if(timer!=null)timer.Stop();if(transferCancel!=null)transferCancel.Cancel();if(transferActive&&transfers!=null&&transfers.Count>0){var record=transfers[0];record.Status="Прервано";if(!string.IsNullOrEmpty(record.TemporaryPath))record.Error="Временный файл может остаться: "+record.TemporaryPath;SaveHistory();}if(stream!=null)stream.Stop();ClearGallery();if(bridge!=null)bridge.Dispose();if(adb!=null)adb.CancelAll();if(tray!=null){tray.Visible=false;tray.Dispose();tray=null;}}
  protected override void Dispose(bool disposing){if(disposing)ShutdownResources();base.Dispose(disposing);}
  void SettingsPage(FlowLayoutPanel p){
   Title(p,"Настройки","Фоновая работа и запуск Windows управляются отдельно от подключения телефона.");
   var closeBox=new CheckBox{Text="При закрытии окна сворачивать к часам и сохранять соединение",Checked=settings.CloseToTray,Width=820,Height=40,ForeColor=ink};
   bool changingClose=false;closeBox.CheckedChanged+=delegate{if(changingClose)return;bool previous=settings.CloseToTray;settings.CloseToTray=closeBox.Checked;try{settingsStore.Save(settings);}catch(Exception e){settings.CloseToTray=previous;changingClose=true;closeBox.Checked=previous;changingClose=false;MessageBox.Show(this,e.Message,"Сохранение настройки");}};p.Controls.Add(closeBox);
   bool configured=false,current=false;string startupError="";try{configured=startupRegistration.Enabled;current=startupRegistration.IsCurrent(Application.ExecutablePath);}catch(Exception e){startupError=e.Message;}
   var startupBox=new CheckBox{Text="Запускать XiaomiDesk вместе с Windows (в фоне)",Checked=configured,Width=820,Height=40,ForeColor=ink,Enabled=startupError.Length==0};
   bool changing=false;startupBox.CheckedChanged+=delegate{if(changing)return;try{startupRegistration.Set(startupBox.Checked,Application.ExecutablePath);operation.Text=startupBox.Checked?"Автозапуск включён для этой копии программы.":"Автозапуск выключен.";}catch(Exception e){changing=true;startupBox.Checked=!startupBox.Checked;changing=false;MessageBox.Show(this,"Не удалось изменить автозапуск: "+e.Message,"XiaomiDesk");}};p.Controls.Add(startupBox);
   p.Controls.Add(LabelText(startupError.Length>0?"Автозапуск недоступен: "+startupError:"Галочка меняет запуск только для вашего пользователя Windows. Чтобы путь оставался рабочим, храните программу в постоянной папке. Настройки Windows могут отдельно запрещать запуск.",10,false,820));
   if(configured&&!current){p.Controls.Add(LabelText("Автозапуск указывает на другую копию XiaomiDesk.",11,true,820));Row(p).Controls.Add(Btn("Использовать эту версию",delegate{startupRegistration.Set(true,Application.ExecutablePath);ShowPage("Настройки");return Task.FromResult(0);},false,280));}
   Card(p,"Закрыть окно и выйти — разные действия","Крестик и сворачивание оставляют приложение возле часов, если включена верхняя галочка. Полное завершение: правой кнопкой по значку → «Выход». Выход отменит текущую передачу. При скрытии окна экран/камера останавливаются.");
   p.Controls.Add(LabelText("Проверка телефона и управления",17,true,820));p.Controls.Add(LabelText("Проверка читает состояние соединения, проверяет доступность UHID и пробует нейтральную команду ввода. Она не включает системные разрешения и не подтверждает работу мыши или набора текста.",10,false,820));
   var report=new TextBox{Width=820,Height=170,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text="Нажмите «Проверить телефон», когда он подключён.",BackColor=Color.White};
   Row(p).Controls.Add(Btn("Проверить телефон",async delegate{Need();string serial=adb.Serial;string model=(await adb.RunForSerial(serial,5000,"shell","getprop ro.product.model")).Trim();string sdk;
    try{string output=await adb.RunForSerial(serial,5000,"shell","input keyevent 0");sdk=output.Contains("INJECT_EVENTS")?"Обычный SDK-ввод: Android отказал (INJECT_EVENTS).":"Обычный SDK-ввод: тестовая команда принята; фактический ввод ещё нужно проверить.";}catch(Exception e){sdk=e.Message.Contains("INJECT_EVENTS")?"Обычный SDK-ввод: Android отказал (INJECT_EVENTS). Используйте режим физической мыши/клавиатуры.":"Обычный SDK-ввод: "+Short(e.Message,150);}
    string uhid;try{uhid=(await adb.RunForSerial(serial,5000,"shell","if (exec 3<>/dev/uhid); then echo available; else echo unavailable; fi")).Trim()=="available"?"UHID: интерфейс доступен; движение и набор текста этим тестом не проверяются.":"UHID: интерфейс недоступен.";}catch(Exception e){uhid="UHID: не удалось проверить — "+Short(e.Message,100);}
    if(!report.IsDisposed)report.Text="Телефон: "+model+Environment.NewLine+"Канал: "+(WirelessDiscovery.IsWirelessSerial(serial)?"Wi-Fi":"USB")+Environment.NewLine+sdk+Environment.NewLine+uhid;
   },true,245));p.Controls.Add(report);
   Card(p,"История передач","Сохраняются последние 100 записей: имена и пути файлов, время, направление и результат. Содержимое файлов, уведомлений и буфера обмена в историю не пишется. Очистка доступна в разделе «Передачи».");
   Row(p).Controls.Add(Btn("Полностью выйти",delegate{RequestExit();return Task.FromResult(0);},false,245));
  }
 }
}
