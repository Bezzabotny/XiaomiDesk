using System;
using System.IO;
using System.IO.Compression;
using XiaomiDesk;
class ArchiveTests {
 static void Entry(ZipArchive zip,string name,string data){using(var w=new StreamWriter(zip.CreateEntry(name).Open()))w.Write(data);}
 static void Main(){string root=Path.Combine(Path.GetTempPath(),"XiaomiDesk-archive-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);try{
  string zip=Path.Combine(root,"safe.zip");using(var z=ZipFile.Open(zip,ZipArchiveMode.Create))Entry(z,"index.json","{\"items\":[{\"name\":\"Фото.jpg\",\"path\":\"/sdcard/DCIM/Фото.jpg\",\"album\":\"Camera\",\"thumb\":\"\"}],\"partial\":true,\"hasMore\":true}");var page=GalleryArchive.Read(zip,Path.Combine(root,"safe"));if(page.items.Length!=1||!page.partial||!page.hasMore)throw new Exception("Metadata lost");
  int n=0;foreach(string entry in new[]{"../outside.jpg","C:/outside.jpg","60.jpg","sub/0.jpg"}){string bad=Path.Combine(root,"bad"+(n++)+".zip");using(var z=ZipFile.Open(bad,ZipArchiveMode.Create)){Entry(z,"index.json","{\"items\":[]}");Entry(z,entry,"bad");}bool rejected=false;try{GalleryArchive.Read(bad,Path.Combine(root,"target"));}catch(IOException){rejected=true;}if(!rejected)throw new Exception("Unsafe archive accepted: "+entry);}
  string error=Path.Combine(root,"error.zip");using(var z=ZipFile.Open(error,ZipArchiveMode.Create))Entry(z,"index.json","{\"items\":[],\"error\":\"permission denied\"}");try{GalleryArchive.Read(error,Path.Combine(root,"error"));throw new Exception("Permission ignored");}catch(IOException e){if(e.Message!="permission denied")throw;}
  string forged=Path.Combine(root,"forged.zip");using(var z=ZipFile.Open(forged,ZipArchiveMode.Create)){Entry(z,"index.json","{\"items\":[]}");Entry(z,"0.jpg",new string('x',3*1024*1024));}var bytes=File.ReadAllBytes(forged);for(int i=0;i<bytes.Length-46;i++)if(bytes[i]==0x50&&bytes[i+1]==0x4b&&bytes[i+2]==1&&bytes[i+3]==2&&bytes[i+46]=='0'){Array.Copy(BitConverter.GetBytes(128),0,bytes,i+24,4);break;}File.WriteAllBytes(forged,bytes);bool bounded=false;try{GalleryArchive.Read(forged,Path.Combine(root,"forged"));}catch(IOException){bounded=true;}if(!bounded)throw new Exception("Forged ZIP size bypassed decompression bound");
  Console.WriteLine("PASS gallery metadata, partial access, pagination, archive confinement, permission error");
 }finally{Directory.Delete(root,true);}}
}
