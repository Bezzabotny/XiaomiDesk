using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public partial class Hub {
  void ToolsPage(FlowLayoutPanel p){
   Title(p,"Инструменты","Подключение и дополнительные возможности — в одном месте.");var row=Row(p);row.Controls.Add(Go("Подключение","Подключение",true,240));row.Controls.Add(Go("Камера","Камера",false,240));row=Row(p);row.Controls.Add(Go("Звонки и SMS","Звонки и SMS",false,240));row.Controls.Add(Go("Раздача интернета","Интернет",false,240));row=Row(p);row.Controls.Add(Go("Настройки приложения","Настройки",false,240));
  }
  void LinksPage(FlowLayoutPanel p){
   Title(p,"Ссылки между устройствами","Откройте страницу на Xiaomi или перенесите ссылку с телефона на ноутбук.");var link=new TextBox{Width=820,Font=new Font("Segoe UI",13),Text="https://",Margin=new Padding(0,8,0,16)};p.Controls.Add(link);var row=Row(p);
   row.Controls.Add(Btn("Вставить из Windows",delegate{if(Clipboard.ContainsText())link.Text=Clipboard.GetText();return Task.FromResult(0);},false,240));
   row.Controls.Add(Btn("Открыть на Xiaomi",async delegate{Need();var url=MediaPolicy.WebUrl(link.Text);await adb.Activity("-a android.intent.action.VIEW -d "+Core.ShellQuote(url.AbsoluteUri));},true,240));
   row=Row(p);row.Controls.Add(Btn("Получить ссылку с Xiaomi",async delegate{await HelperAction("read",null);},false,280));row.Controls.Add(Btn("Вставить полученную",delegate{link.Text=lastClip;return Task.FromResult(0);},false,240));
   row=Row(p);row.Controls.Add(Btn("Открыть в Windows",delegate{var url=MediaPolicy.WebUrl(link.Text);Process.Start(new ProcessStartInfo(url.AbsoluteUri){UseShellExecute=true});return Task.FromResult(0);},true,240));
   p.Controls.Add(LabelText("На Xiaomi скопируйте адрес страницы, затем нажмите «Получить ссылку». После ответа телефона вставьте полученную ссылку в поле. Открытие на ноутбуке — только по вашей кнопке.",10,false,820));
  }
  async Task Screenshot(){
   Need();string serial=adb.Serial;using(var d=new SaveFileDialog{Title="Сохранить скриншот",Filter="Изображение PNG|*.png",FileName="Xiaomi-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png",OverwritePrompt=true}){if(d.ShowDialog(this)!=DialogResult.OK)return;string temporary="/sdcard/Download/.xiaomidesk-"+Guid.NewGuid().ToString("N")+".png";
    Exception failure=null;try{await adb.RunForSerial(serial,15000,"shell","screencap -p "+Core.ShellQuote(temporary));if(adb.Serial!=serial)throw new Exception("Телефон изменился. Повторите скриншот.");await PerformTransfer(new TransferRecord{Direction="Download",RemotePath=temporary,LocalPath=d.FileName,OverwriteApproved=File.Exists(d.FileName)});}catch(Exception e){failure=e;}
    try{await adb.RunForSerial(serial,3000,"shell","rm -f -- "+Core.ShellQuote(temporary));}catch{}
    if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
   }
  }
  void NewOverview(FlowLayoutPanel p){
   Title(p,"Всё рядом.","Галерея, экран и ваши повседневные действия — без лишних окон.");
   var status=Card(p,"Ваш Xiaomi","Подключите телефон, затем обновите сведения.");var body=(Label)status.Controls[1];body.Height=70;status.Height=145;
   var row=Row(p);row.Controls.Add(Btn("Обновить сведения",async delegate{
    Need();string serial=adb.Serial;string battery;try{battery=MediaPolicy.Battery(await adb.RunForSerial(serial,6000,"shell","dumpsys battery"));}catch{battery="Батарея: сведения недоступны";}
    string storage;try{storage=await adb.RunForSerial(serial,6000,"shell","df -h /sdcard");}catch{storage="Память: сведения недоступны";}
    if(body.IsDisposed||adb.Serial!=serial)return;body.Text=battery+"\n"+MediaPolicy.Storage(storage);
   },false,240));row.Controls.Add(Go("Подключить телефон","Подключение",true,240));
   row=Row(p);row.Controls.Add(Go("Мои фото и видео","Галерея",true,240));row.Controls.Add(Go("Экран телефона","Экран телефона",false,240));row=Row(p);row.Controls.Add(Btn("Сделать скриншот",Screenshot,false,240));row.Controls.Add(Go("Передать ссылку","Ссылки",false,240));
   Card(p,"Локально, по Wi-Fi","Сохранённый телефон подключается автоматически, когда оба устройства доступны в одной сети. Изменить это можно в настройках.");
  }
 }
}
