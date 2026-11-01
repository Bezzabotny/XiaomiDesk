using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
namespace XiaomiDesk {
 public class GalleryItem {public string name,path,album,thumb;public bool video;public long modified;}
 public class GalleryPage {public GalleryItem[] items;public bool partial,hasMore;public string error;}
 public static class GalleryArchive {
  static void CopyBounded(ZipArchiveEntry entry,Stream destination,long limit){using(var source=entry.Open()){byte[] buffer=new byte[16384];long count=0;int n;while((n=source.Read(buffer,0,buffer.Length))!=0){count+=n;if(count>limit||count>entry.Length)throw new IOException("Миниатюра превышает допустимый размер.");destination.Write(buffer,0,n);}if(count!=entry.Length)throw new IOException("Повреждённый архив миниатюр.");}}
  public static GalleryPage Read(string file,string directory){
   Directory.CreateDirectory(directory);using(var zip=ZipFile.OpenRead(file)){
    if(zip.Entries.Count>61)throw new IOException("Слишком большая страница галереи.");long total=0;
    var names=new HashSet<string>(StringComparer.Ordinal);foreach(var entry in zip.Entries){total+=entry.Length;if(!names.Add(entry.FullName)||entry.Length>2097152||total>33554432||!(entry.FullName=="index.json"||Regex.IsMatch(entry.FullName,@"^([0-9]|[1-5][0-9])\.jpg$")))throw new IOException("Некорректные миниатюры галереи.");}
    var index=zip.GetEntry("index.json");if(index==null||index.Length>262144)throw new IOException("Нет списка галереи. Обновите помощник на Xiaomi.");GalleryPage page;
    using(var data=new MemoryStream()){CopyBounded(index,data,262144);page=new JavaScriptSerializer{MaxJsonLength=262144}.Deserialize<GalleryPage>(System.Text.Encoding.UTF8.GetString(data.ToArray()));}
    if(page==null||page.items==null||page.items.Length>60)throw new IOException("Некорректный список галереи.");if(!string.IsNullOrEmpty(page.error))throw new IOException(page.error);
    foreach(var item in page.items){if(item==null)throw new IOException("Некорректное фото.");if(!string.IsNullOrEmpty(item.thumb)&&!Regex.IsMatch(item.thumb,@"^([0-9]|[1-5][0-9])\.jpg$"))throw new IOException("Некорректная миниатюра.");}
    foreach(var entry in zip.Entries)if(entry.FullName!="index.json"){string output=Path.Combine(directory,entry.FullName);try{using(var fileStream=new FileStream(output,FileMode.Create,FileAccess.Write))CopyBounded(entry,fileStream,2097152);}catch{if(File.Exists(output))File.Delete(output);throw;}}return page;
   }
  }
 }
}
