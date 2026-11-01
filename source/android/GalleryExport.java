package app.xiaomidesk;
import android.app.Activity;
import android.Manifest;
import android.content.ContentUris;
import android.content.pm.PackageManager;
import android.database.Cursor;
import android.graphics.Bitmap;
import android.media.ThumbnailUtils;
import android.net.Uri;
import android.os.Build;
import android.provider.MediaStore;
import android.util.Size;
import org.json.*;
import java.io.*;
import java.util.zip.*;

final class GalleryExport {
 static String waiting,waitingToken;static int waitingPort;
 static volatile boolean running;
 static void start(Activity a,String request){
  if(!request.matches("gallery:[0-9]{1,8}:[a-f0-9]{32}"))return;
  waiting=request;waitingToken=Wire.prefs(a).getString("token","");waitingPort=Wire.prefs(a).getInt("port",0);
  boolean images=a.checkSelfPermission(Build.VERSION.SDK_INT>=33?Manifest.permission.READ_MEDIA_IMAGES:Manifest.permission.READ_EXTERNAL_STORAGE)==PackageManager.PERMISSION_GRANTED;
  boolean videos=Build.VERSION.SDK_INT<33?images:a.checkSelfPermission(Manifest.permission.READ_MEDIA_VIDEO)==PackageManager.PERMISSION_GRANTED;
  boolean partial=Build.VERSION.SDK_INT>=34&&a.checkSelfPermission(Manifest.permission.READ_MEDIA_VISUAL_USER_SELECTED)==PackageManager.PERMISSION_GRANTED;
  if(!images&&!videos&&!partial){
   if(Build.VERSION.SDK_INT>=34)a.requestPermissions(new String[]{Manifest.permission.READ_MEDIA_IMAGES,Manifest.permission.READ_MEDIA_VIDEO,Manifest.permission.READ_MEDIA_VISUAL_USER_SELECTED},41);
   else if(Build.VERSION.SDK_INT>=33)a.requestPermissions(new String[]{Manifest.permission.READ_MEDIA_IMAGES,Manifest.permission.READ_MEDIA_VIDEO},41);
   else a.requestPermissions(new String[]{Manifest.permission.READ_EXTERNAL_STORAGE},41);
  }else resume(a);
 }
 static void resume(Activity a){final String request=waiting;if(request==null||running)return;final String token=waitingToken;final int port=waitingPort;waiting=null;running=true;new Thread(()->{try{export(a,request,token,port);}finally{running=false;a.runOnUiThread(()->resume(a));}},"gallery-export").start();}
 static void export(Activity a,String request,String token,int port){
  String[] args=request.split(":");int offset=Integer.parseInt(args[1]);File dir=new File(a.getExternalCacheDir(),"gallery");dir.mkdirs();
  // Only our old exports; never delete user media.
  File[] old=dir.listFiles();if(old!=null)for(File f:old)if(f.getName().matches("[a-f0-9]{32}\\.(zip|part)")&&System.currentTimeMillis()-f.lastModified()>600000)f.delete();
  File part=new File(dir,args[2]+".part"),result=new File(dir,args[2]+".zip");JSONObject index=new JSONObject();JSONArray items=new JSONArray();
  try(ZipOutputStream zip=new ZipOutputStream(new FileOutputStream(part))){
   try{
    if(!Wire.allowed(a,token,port))throw new SecurityException("Связь с ноутбуком приостановлена. Откройте помощник и повторите.");
    boolean images=a.checkSelfPermission(Build.VERSION.SDK_INT>=33?Manifest.permission.READ_MEDIA_IMAGES:Manifest.permission.READ_EXTERNAL_STORAGE)==PackageManager.PERMISSION_GRANTED;
    boolean videos=Build.VERSION.SDK_INT<33?images:a.checkSelfPermission(Manifest.permission.READ_MEDIA_VIDEO)==PackageManager.PERMISSION_GRANTED;
    boolean partial=Build.VERSION.SDK_INT>=34&&a.checkSelfPermission(Manifest.permission.READ_MEDIA_VISUAL_USER_SELECTED)==PackageManager.PERMISSION_GRANTED;
    if(!images&&!videos&&!partial)throw new SecurityException("Разрешите помощнику доступ к фото и видео в настройках Xiaomi, затем обновите галерею.");
    index.put("partial",!(images&&videos));
    Uri collection=MediaStore.Files.getContentUri("external");
    String[] columns={"_id","_display_name","_data","media_type","bucket_display_name","date_modified"};
    try(Cursor c=a.getContentResolver().query(collection,columns,"media_type IN (1,3)",null,"date_modified DESC, _id DESC")){
     if(c!=null){index.put("hasMore",c.getCount()>offset+60);if(offset==0||c.moveToPosition(offset-1)){while(items.length()<60&&c.moveToNext()){
      if(!Wire.allowed(a,token,port))throw new SecurityException("Подключение изменилось. Обновите галерею.");
      int n=items.length();boolean video=c.getInt(3)==3;long id=c.getLong(0);JSONObject item=new JSONObject();item.put("name",c.getString(1));item.put("path",c.getString(2));item.put("album",c.getString(4));item.put("video",video);item.put("modified",c.getLong(5));item.put("thumb",n+".jpg");
      try{Uri uri=ContentUris.withAppendedId(video?MediaStore.Video.Media.EXTERNAL_CONTENT_URI:MediaStore.Images.Media.EXTERNAL_CONTENT_URI,id);Bitmap b;
       if(Build.VERSION.SDK_INT>=29)b=a.getContentResolver().loadThumbnail(uri,new Size(480,480),null);
       else b=video?ThumbnailUtils.createVideoThumbnail(c.getString(2),MediaStore.Video.Thumbnails.MINI_KIND):MediaStore.Images.Thumbnails.getThumbnail(a.getContentResolver(),id,MediaStore.Images.Thumbnails.MINI_KIND,null);
       if(b!=null){ByteArrayOutputStream data=new ByteArrayOutputStream();b.compress(Bitmap.CompressFormat.JPEG,82,data);b.recycle();zip.putNextEntry(new ZipEntry(n+".jpg"));data.writeTo(zip);zip.closeEntry();}
      }catch(Exception ignored){item.put("thumb","");}items.put(item);
     }}}
    }
   }catch(Exception e){index.put("error",e instanceof SecurityException?e.getMessage():"Не удалось прочитать галерею: "+e.getClass().getSimpleName());}
   index.put("items",items);zip.putNextEntry(new ZipEntry("index.json"));zip.write(index.toString().getBytes("UTF-8"));zip.closeEntry();
  }catch(Exception e){part.delete();return;}
  part.renameTo(result);
 }
}
