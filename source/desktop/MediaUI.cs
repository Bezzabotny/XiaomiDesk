using System;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public partial class Hub {
  void Media(bool camera){
   var top=new Panel{Dock=DockStyle.Top,Height=camera?126:266};content.Controls.Add(top);
   top.Controls.Add(new Label{Text=camera?"Камера Xiaomi":"Экран телефона",Font=new Font("Segoe UI",24,FontStyle.Bold),ForeColor=ink,AutoSize=true});
   top.Controls.Add(new Label{Text=camera?"Просмотр камеры внутри XiaomiDesk.":"Смотрите и управляйте телефоном с ноутбука.",ForeColor=muted,Location=new Point(0,48),Size=new Size(810,28)});
   var physical=new CheckBox{Text="Физическая мышь и клавиатура (UHID) — для Xiaomi",Checked=true,Location=new Point(0,82),Width=760,Height=28,ForeColor=ink};if(!camera)top.Controls.Add(physical);
   var row=new FlowLayoutPanel{Location=new Point(0,camera?78:116),Width=860,Height=48};top.Controls.Add(row);
   stream=new StreamHost{Dock=DockStyle.Fill,Margin=new Padding(0),MinimumSize=new Size(400,300)};content.Controls.Add(stream);stream.BringToFront();var active=stream;
   Label recordingLabel=null;if(!camera){recordingLabel=new Label{Size=new Size(405,40),ForeColor=Color.FromArgb(165,65,80)};active.RecordingStatusChanged+=delegate(string value){if(!recordingLabel.IsDisposed)recordingLabel.Text=value;};}
   row.Controls.Add(Btn(camera?"Включить камеру":"Показать экран",async delegate{Need();await active.Start(adb,camera,physical.Checked);},true,200));
   row.Controls.Add(Btn("Остановить",delegate{active.Stop();if(active.RecordingResult!=null)MessageBox.Show(this,active.RecordingResult,"Запись экрана");return Task.FromResult(0);},false,130));
   if(!camera){
    var capture=new FlowLayoutPanel{Location=new Point(0,210),Width=860,Height=48};top.Controls.Add(capture);capture.Controls.Add(Btn("Скриншот",Screenshot,false,180));capture.Controls.Add(Btn("Записать экран",async delegate{Need();using(var folder=new FolderBrowserDialog{Description="Папка для записи экрана (без звука)"}){if(folder.ShowDialog(this)!=DialogResult.OK)return;string file=FreeLocalName(folder.SelectedPath,"Xiaomi-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".mkv");using(File.Create(file)){}await active.Start(adb,false,physical.Checked,file);operation.Text="● Идёт запись без звука: "+file+". Нажмите «Остановить» для сохранения.";}},true,200));capture.Controls.Add(recordingLabel);
    var back=Btn("Назад",async delegate{Need();await adb.Shell("input keyevent 4");},false,92);var home=Btn("Домой",async delegate{Need();await adb.Shell("input keyevent 3");},false,92);back.Visible=false;home.Visible=false;row.Controls.Add(back);row.Controls.Add(home);
    physical.CheckedChanged+=delegate{active.Stop();back.Visible=!physical.Checked;home.Visible=!physical.Checked;operation.Text="Режим изменён. Нажмите «Показать экран» заново.";};
    row.Controls.Add(Btn("Раскладка",async delegate{Need();await adb.Activity("-a android.settings.HARD_KEYBOARD_SETTINGS");},false,125));
    top.Controls.Add(new Label{Text="Щёлкните внутри изображения для управления. Левый Alt возвращает мышь ноутбуку.\nВ UHID используйте экранные кнопки/жесты Android для «Домой» и «Назад».",ForeColor=muted,Location=new Point(0,165),Size=new Size(830,42),Font=new Font("Segoe UI",9)});
   }
   row.Controls.Add(Btn("Журнал",delegate{string log=active.Diagnostics;MessageBox.Show(this,string.IsNullOrWhiteSpace(log)?"Сначала запустите экран или камеру.":log,"Диагностика потока",MessageBoxButtons.OK,MessageBoxIcon.Information);return Task.FromResult(0);},false,105));
  }
 }
}
