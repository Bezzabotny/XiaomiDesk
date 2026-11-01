using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public partial class Hub {
  TransferHistory historyStore;List<TransferRecord> transfers;CancellationTokenSource transferCancel;bool transferActive;TaskCompletionSource<bool> transferCompletion;
  ProgressBar transferBar;Label transferText;Button cancelTransfer;ListView transferList;string transferSummary="Нет активных передач",historyWarning="";TransferProgress currentProgress;
  void InitializeTransfers(){historyStore=new TransferHistory(Path.Combine(Path.GetDirectoryName(settingsStore.FilePath),"transfers.json"));transfers=historyStore.Load();}
  void SaveHistory(){try{historyStore.Save(transfers);historyWarning="";}catch(Exception e){historyWarning="История не сохранена: "+e.Message;}}
  static string SizeText(long n){if(n<0)return "неизвестно";string[] units={"Б","КБ","МБ","ГБ","ТБ"};double value=n;int i=0;while(value>=1024&&i<units.Length-1){value/=1024;i++;}return value.ToString(i==0?"0":"0.0")+" "+units[i];}
  void ProgressUI(FlowLayoutPanel p){
   transferBar=new ProgressBar{Width=820,Height=16,Minimum=0,Maximum=100,Margin=new Padding(0,5,0,8)};p.Controls.Add(transferBar);
   transferText=LabelText(transferSummary,10,false,820);transferText.Height=40;p.Controls.Add(transferText);
   var row=Row(p);row.Height=45;cancelTransfer=Btn("Отменить передачу",delegate{return Task.FromResult(0);},false,220);cancelTransfer.Click+=delegate{if(transferCancel!=null){transferCancel.Cancel();transferSummary="Отменяем передачу…";UpdateTransferView();}};row.Controls.Add(cancelTransfer);row.Controls.Add(Go("История передач","Передачи",false,200));UpdateTransferView();
  }
  void UpdateTransferView(){
   if(closing)return;if(transferText!=null&&!transferText.IsDisposed)transferText.Text=transferSummary+(historyWarning.Length>0?"\n"+historyWarning:"");
   if(cancelTransfer!=null&&!cancelTransfer.IsDisposed)cancelTransfer.Enabled=transferActive;
   if(transferBar!=null&&!transferBar.IsDisposed){bool unknown=transferActive&&(currentProgress==null||currentProgress.Total<=0);transferBar.Style=unknown?ProgressBarStyle.Marquee:ProgressBarStyle.Continuous;transferBar.Value=transferActive?(currentProgress==null?0:currentProgress.Percent):transferSummary.StartsWith("Готово",StringComparison.Ordinal)?100:0;}
  }
  async Task PerformTransfer(TransferRecord record){
   Need();if(transferActive)throw new Exception("Дождитесь текущей передачи.");string serial=adb.Serial;string identity=await PreparePhoneSession();
   if(!string.IsNullOrEmpty(record.DeviceIdentity)&&identity!=record.DeviceIdentity)throw new Exception("Эта передача относится к другому телефону. Выберите исходное устройство.");record.DeviceIdentity=identity;
   transfers.Insert(0,record);if(transfers.Count>100)transfers.RemoveAt(transfers.Count-1);record.Status="Выполняется";SaveHistory();
   transferCompletion=new TaskCompletionSource<bool>();transferCancel=new CancellationTokenSource();transferActive=true;currentProgress=null;transferSummary="Подготовка: "+Path.GetFileName(record.LocalPath);UseWaitCursor=false;UpdateTransferView();RefreshTransferList();bool tempSaved=false;
   try{
    var engine=new TransferEngine(new AdbTransferTransport(adb,serial));
    await engine.Execute(record,delegate(TransferProgress progress){if(!tempSaved&&!string.IsNullOrEmpty(record.TemporaryPath)){tempSaved=true;SaveHistory();}currentProgress=progress;string eta="";if(progress.Total>0&&progress.BytesPerSecond>0){double seconds=Math.Max(0,(progress.Total-progress.Bytes)/progress.BytesPerSecond);eta=" · осталось ~"+(seconds>=60?Math.Ceiling(seconds/60)+" мин":Math.Ceiling(seconds)+" с");}transferSummary=Path.GetFileName(record.LocalPath)+" · "+SizeText(progress.Bytes)+(progress.Total>0?" / "+SizeText(progress.Total):"")+" · "+SizeText((long)progress.BytesPerSecond)+"/с"+eta;UpdateTransferView();},transferCancel.Token);
    transferSummary="Готово: "+Path.GetFileName(record.LocalPath)+" · "+SizeText(record.Bytes);
   }catch(OperationCanceledException){transferSummary="Передача отменена: "+Path.GetFileName(record.LocalPath);}
   catch(Exception){transferSummary="Ошибка передачи: "+record.Error;throw;}
   finally{transferActive=false;transferCancel.Dispose();transferCancel=null;SaveHistory();UpdateTransferView();RefreshTransferList();transferCompletion.TrySetResult(true);}
  }
  async Task<string> FreeRemoteName(string directory,string filename){
   var transport=new AdbTransferTransport(adb,adb.Serial);for(int i=0;i<1000;i++){string name=i==0?filename:Path.GetFileNameWithoutExtension(filename)+" ("+(i+1)+")"+Path.GetExtension(filename);string candidate=Core.PhonePath(directory+"/"+name);if(!await transport.Exists(candidate,CancellationToken.None))return candidate;}throw new IOException("Не удалось подобрать свободное имя на телефоне.");
  }
  static string FreeLocalName(string directory,string filename){for(int i=0;i<1000;i++){string name=i==0?filename:Path.GetFileNameWithoutExtension(filename)+" ("+(i+1)+")"+Path.GetExtension(filename);string candidate=Path.Combine(directory,name);if(!File.Exists(candidate)&&!Directory.Exists(candidate))return candidate;}throw new IOException("Не удалось подобрать свободное имя файла.");}
  void Files(FlowLayoutPanel p){
   Title(p,"Файлы без пересылок себе","Отправка и скачивание с прогрессом. Новые файлы с совпадающим именем сохраняются отдельно.");path=new TextBox{Width=605,Text=remote,Margin=new Padding(0,8,10,0)};var r=Row(p);r.Controls.Add(path);r.Controls.Add(Btn("Открыть",LoadFiles,true,150));
   r=Row(p);r.Controls.Add(Btn("↑ На уровень выше",async delegate{remote=Core.PhonePath(remote+"/..");path.Text=remote;await LoadFiles();},false,185));r.Controls.Add(Btn("Отправить на телефон",Upload,true,230));r.Controls.Add(Btn("Скачать выбранные",Download,false,220));ProgressUI(p);
   files=new ListView{Width=820,Height=290,View=View.Details,FullRowSelect=true,MultiSelect=true,BorderStyle=BorderStyle.FixedSingle,HideSelection=false,BackColor=Color.White};files.Columns.Add("Имя",650);files.Columns.Add("Тип",130);
   files.DoubleClick+=async delegate{if(files.SelectedItems.Count==1&&(bool)files.SelectedItems[0].Tag){path.Text=Core.PhonePath(remote+"/"+files.SelectedItems[0].Text);await Use(LoadFiles);}};p.Controls.Add(files);
   files.AllowDrop=true;files.DragEnter+=delegate(object sender,DragEventArgs e){if(!busy&&e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};files.DragDrop+=async delegate(object sender,DragEventArgs e){var local=(string[])e.Data.GetData(DataFormats.FileDrop);await Use(async delegate{await Push(local);if(files!=null&&!files.IsDisposed)await LoadFiles();});};p.Controls.Add(LabelText("Перетащите файлы из Проводника в список. Ctrl/Shift позволяют выбрать несколько файлов для скачивания. Фото: /sdcard/DCIM/Camera.",10,false,820));
   if(adb.Serial!=null)BeginInvoke(new Action(async delegate{if(page=="Файлы"&&!closing)await Use(LoadFiles);}));
  }
  async Task Push(string[] local){Need();string targetDirectory=Core.PhonePath(remote);foreach(string file in local){if(!File.Exists(file))throw new Exception("Выберите файлы, а не папки.");string target=await FreeRemoteName(targetDirectory,Path.GetFileName(file));await PerformTransfer(new TransferRecord{Direction="Upload",LocalPath=file,RemotePath=target});if(transfers[0].Status=="Отменено")break;}}
  async Task Upload(){Need();using(var d=new OpenFileDialog{Multiselect=true,Title="Выберите файлы для Xiaomi"}){if(d.ShowDialog(this)!=DialogResult.OK)return;await Push(d.FileNames);if(files!=null&&!files.IsDisposed)await LoadFiles();}}
  async Task Download(){
   Need();if(files==null||files.SelectedItems.Count==0)throw new Exception("Выберите файлы в списке.");var selected=files.SelectedItems.Cast<ListViewItem>().ToArray();if(selected.Any(item=>(bool)item.Tag))throw new Exception("Откройте папку и выберите файлы внутри неё.");string directory=remote;
   if(selected.Length==1){using(var d=new SaveFileDialog{FileName=SafeLocalName(selected[0].Text),Title="Сохранить файл с Xiaomi",OverwritePrompt=true}){if(d.ShowDialog(this)!=DialogResult.OK)return;await PerformTransfer(new TransferRecord{Direction="Download",LocalPath=d.FileName,RemotePath=Core.PhonePath(directory+"/"+selected[0].Text),OverwriteApproved=File.Exists(d.FileName)});}}
   else using(var d=new FolderBrowserDialog{Description="Куда сохранить выбранные файлы?"}){if(d.ShowDialog(this)!=DialogResult.OK)return;foreach(var item in selected){await PerformTransfer(new TransferRecord{Direction="Download",LocalPath=FreeLocalName(d.SelectedPath,SafeLocalName(item.Text)),RemotePath=Core.PhonePath(directory+"/"+item.Text)});if(transfers[0].Status=="Отменено")break;}}
  }
  void TransfersPage(FlowLayoutPanel p){
   Title(p,"Передачи","Последние 100 передач на этом компьютере. Повтор запускает копирование заново, с проверкой исходного телефона.");ProgressUI(p);
   var row=Row(p);row.Controls.Add(Btn("Повторить выбранную",RetryTransfer,true,245));row.Controls.Add(Btn("Показать папку",delegate{if(transferList.SelectedItems.Count==0)throw new Exception("Выберите передачу.");var r=(TransferRecord)transferList.SelectedItems[0].Tag;string directory=Path.GetDirectoryName(r.LocalPath);if(Directory.Exists(directory))System.Diagnostics.Process.Start("explorer.exe",Core.Arg(directory));return Task.FromResult(0);},false,200));
   row.Controls.Add(Btn("Очистить историю",delegate{if(transferActive)throw new Exception("Сначала завершите передачу.");transfers.Clear();SaveHistory();RefreshTransferList();return Task.FromResult(0);},false,210));
   transferList=new ListView{Width=820,Height=360,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false};transferList.Columns.Add("Когда",110);transferList.Columns.Add("Файл",300);transferList.Columns.Add("Направление",160);transferList.Columns.Add("Состояние",180);p.Controls.Add(transferList);RefreshTransferList();
   transferList.DoubleClick+=delegate{if(transferList.SelectedItems.Count>0){var r=(TransferRecord)transferList.SelectedItems[0].Tag;MessageBox.Show(this,"Телефон: "+r.RemotePath+"\nКомпьютер: "+r.LocalPath+"\nСостояние: "+r.Status+(string.IsNullOrEmpty(r.TemporaryPath)?"":"\nВременный файл: "+r.TemporaryPath)+(string.IsNullOrEmpty(r.Error)?"":"\n\n"+r.Error),"Передача");}};
  }
  void RefreshTransferList(){if(transferList==null||transferList.IsDisposed||closing)return;transferList.Items.Clear();foreach(var r in transfers){DateTime time;string date=DateTime.TryParse(r.StartedUtc,out time)?time.ToLocalTime().ToString("dd.MM HH:mm"):"";var item=new ListViewItem(date){Tag=r};item.SubItems.Add(Path.GetFileName(r.LocalPath??""));item.SubItems.Add(r.Direction=="Upload"?"На телефон":"На ноутбук");item.SubItems.Add(r.Status??"");transferList.Items.Add(item);}}
  async Task RetryTransfer(){
   Need();if(transferList==null||transferList.SelectedItems.Count==0)throw new Exception("Выберите неудачную или отменённую передачу.");var old=(TransferRecord)transferList.SelectedItems[0].Tag;if(old.Status=="Успешно"||old.Status=="Выполняется")throw new Exception("Повтор доступен для неудачных, отменённых и прерванных передач.");
   var record=new TransferRecord{Direction=old.Direction,LocalPath=old.LocalPath,RemotePath=old.RemotePath,DeviceIdentity=old.DeviceIdentity};string identity=await PreparePhoneSession();if(identity!=old.DeviceIdentity)throw new Exception("Выберите телефон, для которого выполнялась эта передача.");
   if(record.Direction=="Upload"){int slash=record.RemotePath.LastIndexOf('/');record.RemotePath=await FreeRemoteName(record.RemotePath.Substring(0,slash),record.RemotePath.Substring(slash+1));}
   else using(var d=new SaveFileDialog{FileName=Path.GetFileName(record.LocalPath),InitialDirectory=Path.GetDirectoryName(record.LocalPath),OverwritePrompt=true}){if(d.ShowDialog(this)!=DialogResult.OK)return;record.LocalPath=d.FileName;record.OverwriteApproved=File.Exists(d.FileName);}
   await PerformTransfer(record);
  }
 }
}
