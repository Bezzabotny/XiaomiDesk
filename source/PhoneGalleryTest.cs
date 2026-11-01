using System;
using System.IO;
using System.Drawing;
using XiaomiDesk;
class PhoneGalleryTest {
 static void Main(string[] args){var page=GalleryArchive.Read(args[0],args[1]);int decoded=0;foreach(var item in page.items){if(!string.IsNullOrEmpty(item.thumb)){using(var image=Image.FromFile(Path.Combine(args[1],item.thumb))){if(image.Width<1||image.Height<1)throw new Exception("Invalid cover");decoded++;}}if(!string.IsNullOrEmpty(item.path))MediaPolicy.PhoneMediaPath(item.path);}Console.WriteLine("PASS phone gallery: "+page.items.Length+" items, "+decoded+" decoded covers; partial="+page.partial+"; nextPage="+page.hasMore);}
}
