using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public partial class Hub {
  CancellationTokenSource galleryCancel;string galleryDirectory,gallerySerial,galleryIdentity;GalleryPage galleryPage;int galleryOffset;FlowLayoutPanel galleryGrid;ComboBox galleryAlbums,galleryKinds;Label galleryInfo;Button galleryNext,galleryPrevious;
  void ClearGallery(){if(galleryCancel!=null){galleryCancel.Cancel();galleryCancel=null;}galleryPage=null;if(galleryGrid!=null&&!galleryGrid.IsDisposed){foreach(Control c in galleryGrid.Controls.Cast<Control>().ToArray())c.Dispose();galleryGrid.Controls.Clear();}galleryGrid=null;if(galleryInfo!=null&&!galleryInfo.IsDisposed)galleryInfo.Text="Нажмите «Обновить», чтобы загрузить галерею выбранного телефона.";string dir=galleryDirectory;galleryDirectory=null;if(dir!=null)try{Directory.Delete(dir,true);}catch{} }
  Button GalleryButton(string text,Func<Task> action,int width){var b=new Button{Text=text,Width=width,Height=38,FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=ink,Margin=new Padding(0,4,8,4)};b.Click+=async delegate{if(!b.Enabled||busy)return;b.Enabled=false;try{await action();}catch(OperationCanceledException){}catch(Exception e){if(!closing&&page=="Галерея")MessageBox.Show(this,e.Message,"Галерея",MessageBoxButtons.OK,MessageBoxIcon.Information);}finally{if(!b.IsDisposed)b.Enabled=b==galleryNext?galleryPage!=null&&galleryPage.hasMore:b==galleryPrevious?galleryPage!=null&&galleryOffset>0:true;}};return b;}
  void Gallery(FlowLayoutPanel p){
   Title(p,"Галерея","Фото и видео с Xiaomi. Миниатюры загружаются отдельно от оригиналов.");
   var row=Row(p);var refresh=GalleryButton("Обновить",async delegate{await LoadGallery(0);},135);row.Controls.Add(refresh);row.Controls.Add(Btn("Обновить помощник",Install,false,220));row.Controls.Add(GalleryButton("Доступ к фото",async delegate{Need();await adb.Activity("-a android.settings.APPLICATION_DETAILS_SETTINGS -d package:app.xiaomidesk");},175));
   galleryAlbums=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=250,Margin=new Padding(0,8,12,0)};galleryAlbums.Items.Add("Все альбомы на странице");galleryAlbums.SelectedIndex=0;
   galleryKinds=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=160,Margin=new Padding(0,8,12,0)};galleryKinds.Items.AddRange(new object[]{"Фото и видео","Только фото","Только видео"});galleryKinds.SelectedIndex=0;
   row=Row(p);row.Controls.Add(galleryAlbums);row.Controls.Add(galleryKinds);row.Controls.Add(Btn("Скачать выбранные",DownloadGallery,true,235));
   galleryInfo=LabelText("Нажмите «Обновить». При первом входе разрешите доступ на экране Xiaomi.",10,false,820);p.Controls.Add(galleryInfo);
   galleryGrid=new FlowLayoutPanel{Width=840,Height=410,AutoScroll=true,BackColor=Color.White,Padding=new Padding(12),Margin=new Padding(0,0,0,10)};p.Controls.Add(galleryGrid);
   row=Row(p);galleryPrevious=GalleryButton("← Предыдущие",async delegate{await LoadGallery(Math.Max(0,galleryOffset-60));},180);galleryNext=GalleryButton("Следующие →",async delegate{await LoadGallery(galleryOffset+60);},180);row.Controls.Add(galleryPrevious);row.Controls.Add(galleryNext);galleryPrevious.Enabled=false;galleryNext.Enabled=false;
   galleryAlbums.SelectedIndexChanged+=delegate{RenderGallery();};galleryKinds.SelectedIndexChanged+=delegate{RenderGallery();};
   p.Controls.Add(LabelText("Нажмите на фото для просмотра. Галочки под обложками — выбор для скачивания. Фильтры относятся к текущей странице из 60 элементов.",10,false,820));
   if(adb.Serial!=null)BeginInvoke(new Action(delegate{if(page=="Галерея"&&!closing&&!busy&&!refresh.IsDisposed)refresh.PerformClick();}));
  }
  async Task LoadGallery(int offset){
   Need();if(transferActive)throw new Exception("Сначала дождитесь передачи файлов.");if(galleryCancel!=null)throw new Exception("Дождитесь загрузки миниатюр.");var grid=galleryGrid;ClearGallery();galleryGrid=grid;galleryOffset=offset;gallerySerial=adb.Serial;string serial=gallerySerial;
   if(galleryGrid==null)return;foreach(Control c in galleryGrid.Controls.Cast<Control>().ToArray())c.Dispose();galleryGrid.Controls.Clear();
   galleryInfo.Text="Подготовка миниатюр на Xiaomi… Разрешите доступ на телефоне, если появится запрос.";
   var cancel=new CancellationTokenSource();galleryCancel=cancel;var token=cancel.Token;string id=Guid.NewGuid().ToString("N");string remoteZip="/sdcard/Android/data/app.xiaomidesk/cache/gallery/"+id+".zip";
   string directory=Path.Combine(Path.GetTempPath(),"XiaomiDesk-gallery-"+id);Directory.CreateDirectory(directory);galleryDirectory=directory;
   Exception failure=null;try{
    galleryIdentity=await PreparePhoneSession();token.ThrowIfCancellationRequested();if(adb.Serial!=serial)throw new OperationCanceledException();
    string package=await adb.RunCancellable(serial,token,5000,"shell","dumpsys package app.xiaomidesk");var version=System.Text.RegularExpressions.Regex.Match(package,@"versionCode=(\d+)");if(!version.Success||int.Parse(version.Groups[1].Value)<4)throw new Exception("Сначала нажмите «Обновить помощник» в галерее. Для миниатюр нужна версия 0.4.");
    await HelperAction("gallery:"+offset+":"+id,null);token.ThrowIfCancellationRequested();
    bool ready=false;for(int i=0;i<90;i++){token.ThrowIfCancellationRequested();if(adb.Serial!=serial)throw new OperationCanceledException();string exists=await adb.RunCancellable(serial,token,5000,"shell","test -f "+Core.ShellQuote(remoteZip)+" && echo ready || true");if(exists.Trim()=="ready"){ready=true;break;}await Task.Delay(1000,token);}
    if(!ready)throw new Exception("Помощник не подготовил галерею. Обновите помощник кнопкой выше, разрешите доступ к фото и видео на Xiaomi и повторите.");
    long archiveSize;string size=(await adb.RunCancellable(serial,token,5000,"shell","stat -c%s "+Core.ShellQuote(remoteZip))).Trim();if(!long.TryParse(size,out archiveSize)||archiveSize<1||archiveSize>33554432)throw new IOException("Слишком большой архив миниатюр.");
    string archive=Path.Combine(directory,"page.zip");await adb.RunCancellable(serial,token,30000,"pull",remoteZip,archive);token.ThrowIfCancellationRequested();if(adb.Serial!=serial)throw new OperationCanceledException();
    var loaded=GalleryArchive.Read(archive,directory);token.ThrowIfCancellationRequested();galleryPage=loaded;
    galleryAlbums.Items.Clear();galleryAlbums.Items.Add("Все альбомы на странице");foreach(string album in loaded.items.Select(x=>x.album??"Без альбома").Distinct().OrderBy(x=>x))galleryAlbums.Items.Add(album);galleryAlbums.SelectedIndex=0;
    galleryInfo.Text=(loaded.partial?"Доступ ограничен выбранными фото/видео. ":"")+"Элементы "+(loaded.items.Length==0?"0":(offset+1)+"–"+(offset+loaded.items.Length))+" · отметьте нужные снимки";
    galleryPrevious.Enabled=offset>0;galleryNext.Enabled=loaded.hasMore;RenderGallery();
   }catch(Exception e){failure=e;}
   try{await adb.RunForSerial(serial,3000,"shell","rm -f -- "+Core.ShellQuote(remoteZip));}catch{}
   if(failure!=null){if(!galleryInfo.IsDisposed&&galleryCancel==cancel)galleryInfo.Text="Галерея не загружена. Проверьте доступ к фото и обновление помощника.";try{Directory.Delete(directory,true);}catch{}}
   cancel.Dispose();if(galleryCancel==cancel)galleryCancel=null;
   if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
  }
  void RenderGallery(){
   if(galleryPage==null||galleryGrid==null||galleryGrid.IsDisposed)return;foreach(Control c in galleryGrid.Controls.Cast<Control>().ToArray())c.Dispose();galleryGrid.Controls.Clear();
   foreach(var item in galleryPage.items){if(galleryKinds.SelectedIndex==1&&item.video||galleryKinds.SelectedIndex==2&&!item.video)continue;if(galleryAlbums.SelectedIndex>0&&(item.album??"Без альбома")!=(string)galleryAlbums.SelectedItem)continue;
    var card=new Panel{Width=180,Height=200,BackColor=paper,Margin=new Padding(7),Tag=item};var image=new PictureBox{Width=180,Height=146,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(232,235,240),Cursor=Cursors.Hand};
    string thumb=string.IsNullOrEmpty(item.thumb)?null:Path.Combine(galleryDirectory,item.thumb);if(thumb!=null&&File.Exists(thumb))try{using(var original=Image.FromFile(thumb))image.Image=new Bitmap(original);}catch{}
    image.Disposed+=delegate{if(image.Image!=null)image.Image.Dispose();};image.Click+=delegate{PreviewGallery(item,image.Image);};card.Controls.Add(image);
    if(image.Image==null)image.Controls.Add(new Label{Text=item.video?"▶ Видео":"Фото",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=muted});
    var check=new CheckBox{Text=(item.video?"▶ ":"")+(item.name??"Фото"),Location=new Point(5,151),Size=new Size(170,43),AutoEllipsis=true,Tag=item};card.Controls.Add(check);galleryGrid.Controls.Add(card);
   }
   if(galleryGrid.Controls.Count==0)galleryGrid.Controls.Add(new Label{Text="Нет фото или видео в выбранном фильтре.\nВыберите другой альбом/тип или откройте следующую страницу. Если галерея пуста — проверьте доступ к фото на телефоне.",Width=760,Height=100,ForeColor=muted,Padding=new Padding(12),Font=new Font("Segoe UI",11)});
  }
  void PreviewGallery(GalleryItem item,Image cover){string sourceIdentity=galleryIdentity;using(var dialog=new Form{Text=item.name??"Просмотр",Size=new Size(900,750),StartPosition=FormStartPosition.CenterParent,BackColor=nav}){
   Image owned=cover==null?null:new Bitmap(cover);var pic=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Image=owned};dialog.Controls.Add(pic);var label=new Label{Dock=DockStyle.Bottom,Height=58,ForeColor=Color.White,TextAlign=ContentAlignment.MiddleCenter,Text=(item.video?"Обложка видео":"Предпросмотр фото")+" · "+(item.album??"")+"\nДля полного качества загрузите оригинал"};dialog.Controls.Add(label);
   var download=new Button{Text="Скачать оригинал",Dock=DockStyle.Bottom,Height=44,BackColor=accent,FlatStyle=FlatStyle.Flat};dialog.Controls.Add(download);
   download.Click+=async delegate{
    download.Enabled=false;
    try{await Use(async delegate{
     Need();if(adb.Serial!=gallerySerial)throw new Exception("Телефон изменился. Обновите галерею.");
     using(var save=new SaveFileDialog{FileName=SafeLocalName(item.name??"Фото"),OverwritePrompt=true}){
      if(save.ShowDialog(dialog)!=DialogResult.OK)return;
      await PerformTransfer(GalleryDownloadRecord(item,save.FileName,sourceIdentity,File.Exists(save.FileName)));
      if(dialog.IsDisposed||transfers[0].Status!="Успешно")return;
      if(!item.video)try{using(var original=Image.FromFile(save.FileName)){var full=new Bitmap(original);pic.Image=full;if(owned!=null)owned.Dispose();owned=full;}label.Text="Оригинал · "+(item.album??"");}
      catch{label.Text="Оригинал сохранён. Для этого формата используйте приложение «Фотографии» Windows.";}
      else label.Text="Видео сохранено: "+save.FileName;
     }
    });}finally{if(!download.IsDisposed)download.Enabled=true;}
   };
   dialog.ShowDialog(this);pic.Image=null;if(owned!=null)owned.Dispose();}}
  async Task DownloadGallery(){
   Need();if(adb.Serial!=gallerySerial)throw new Exception("Телефон изменился. Обновите галерею.");if(galleryGrid==null)throw new Exception("Сначала загрузите галерею.");var selected=galleryGrid.Controls.Cast<Control>().SelectMany(c=>c.Controls.OfType<CheckBox>()).Where(c=>c.Checked).Select(c=>(GalleryItem)c.Tag).ToArray();if(selected.Length==0)throw new Exception("Отметьте снимки или видео под обложками.");
   string sourceIdentity=galleryIdentity;using(var d=new FolderBrowserDialog{Description="Куда скачать оригиналы фото и видео?"}){if(d.ShowDialog(this)!=DialogResult.OK)return;foreach(var item in selected){await PerformTransfer(GalleryDownloadRecord(item,FreeLocalName(d.SelectedPath,SafeLocalName(item.name??"Фото")),sourceIdentity,false));if(transfers[0].Status=="Отменено")break;}}
  }
  static TransferRecord GalleryDownloadRecord(GalleryItem item,string destination,string identity,bool overwrite){if(string.IsNullOrEmpty(identity))throw new InvalidOperationException("Обновите галерею перед скачиванием.");return new TransferRecord{Direction="Download",DeviceIdentity=identity,RemotePath=MediaPolicy.PhoneMediaPath(item.path),LocalPath=destination,OverwriteApproved=overwrite};}
 }
}
