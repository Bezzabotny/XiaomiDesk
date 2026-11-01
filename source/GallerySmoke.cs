using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Windows.Forms;
using XiaomiDesk;
class GallerySmoke {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(Hub h,string field,object value){typeof(Hub).GetField(field,flags).SetValue(h,value);}
 static object Get(Hub h,string field){return typeof(Hub).GetField(field,flags).GetValue(h);}
 [STAThread]static int Main(string[] args){string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);try{
  Application.EnableVisualStyles();using(var hub=new Hub(Path.Combine(root,"gallery-settings.json"),new Startup())){
   hub.Show();Application.DoEvents();typeof(Hub).GetMethod("ShowPage",flags).Invoke(hub,new object[]{"Галерея"});
   string cache=Path.Combine(root,"cache");Directory.CreateDirectory(cache);using(var b=new Bitmap(480,320)){using(var g=Graphics.FromImage(b)){g.Clear(Color.LightSteelBlue);g.FillEllipse(Brushes.Gold,250,30,80,80);g.FillRectangle(Brushes.SlateBlue,0,220,480,100);}b.Save(Path.Combine(cache,"0.jpg"),System.Drawing.Imaging.ImageFormat.Jpeg);}
   Set(hub,"galleryDirectory",cache);Set(hub,"galleryPage",new GalleryPage{items=new[]{new GalleryItem{name="Фото 1.jpg",album="Camera",thumb="0.jpg"},new GalleryItem{name="Видео.mp4",album="Camera",thumb="0.jpg",video=true},new GalleryItem{name="Скриншот.png",album="Screenshots",thumb="0.jpg"}}});
   var albums=(ComboBox)Get(hub,"galleryAlbums");albums.Items.AddRange(new object[]{"Camera","Screenshots"});var kinds=(ComboBox)Get(hub,"galleryKinds");var grid=(FlowLayoutPanel)Get(hub,"galleryGrid");typeof(Hub).GetMethod("RenderGallery",flags).Invoke(hub,new object[0]);
   if(grid.Controls.Count!=3)throw new Exception("Gallery cards missing");kinds.SelectedIndex=2;if(grid.Controls.Count!=1)throw new Exception("Video filter failed");kinds.SelectedIndex=0;albums.SelectedIndex=2;if(grid.Controls.Count!=1)throw new Exception("Album filter failed");kinds.SelectedIndex=2;if(!grid.Controls.OfType<Label>().Any(c=>c.Text.Contains("Нет фото")))throw new Exception("Empty gallery filter has no explanation");kinds.SelectedIndex=0;albums.SelectedIndex=0;Application.DoEvents();
   using(var b=new Bitmap(hub.Width,hub.Height)){hub.DrawToBitmap(b,new Rectangle(Point.Empty,hub.Size));b.Save(Path.Combine(root,"gallery-fixture.png"));}
   typeof(Hub).GetMethod("SwitchSerial",flags).Invoke(hub,new object[]{"new-phone"});if(grid.Controls.Count!=0||Directory.Exists(cache))throw new Exception("Previous phone gallery or cache retained");Console.WriteLine("PASS native gallery cards, photo/video and album filters, device change clears previews and cache");
   var make=typeof(Hub).GetMethod("GalleryDownloadRecord",BindingFlags.Static|BindingFlags.NonPublic);var record=(TransferRecord)make.Invoke(null,new object[]{new GalleryItem{path="/sdcard/DCIM/a.jpg"},"local.jpg","serial:original-phone",false});if(record.DeviceIdentity!="serial:original-phone")throw new Exception("Gallery original lost its phone identity after device change");bool refused=false;try{make.Invoke(null,new object[]{new GalleryItem{path="/sdcard/DCIM/a.jpg"},"local.jpg",null,false});}catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}if(!refused)throw new Exception("Gallery original accepted without verified identity");Console.WriteLine("PASS gallery original pins source phone identity and rejects missing identity");
  }return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 class Startup:IStartupStore {public string Read(){return null;}public void Write(string v){}public void Delete(){}}
}
