package app.xiaomidesk;
import android.content.Context;
import android.content.SharedPreferences;
import org.json.JSONObject;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;

final class Wire {
 static final ThreadPoolExecutor pool=new ThreadPoolExecutor(1,1,10,TimeUnit.SECONDS,new ArrayBlockingQueue<Runnable>(32),new ThreadPoolExecutor.DiscardPolicy());
 static volatile long success=0;
 static SharedPreferences prefs(Context c){return c.getSharedPreferences("link",Context.MODE_PRIVATE);}
 static String trim(String s,int n){return s==null?"":s.length()>n?s.substring(0,n):s;}
 static void cancelPending(){pool.getQueue().clear();success=0;}
 static boolean allowed(Context c,String token,int port){SharedPreferences p=prefs(c);return SessionPolicy.allows(p.getBoolean("enabled",false),p.getString("token",""),p.getInt("port",0),token,port);}
 static void send(Context c,String kind,String app,String title,String text){
  SharedPreferences p=prefs(c);if(!p.getBoolean("enabled",false))return;
  final String token=p.getString("token","");final int port=p.getInt("port",0);if(token.length()!=64||port<1||port>65535)return;
  final byte[] body;try{body=new JSONObject().put("kind",kind).put("app",trim(app,200)).put("title",trim(title,1000)).put("text",trim(text,16000)).toString().getBytes(StandardCharsets.UTF_8);}catch(Exception e){return;}
  if(body.length>65536)return;
  final Context context=c.getApplicationContext();
  pool.execute(new Runnable(){public void run(){if(!allowed(context,token,port))return;HttpURLConnection conn=null;try{conn=(HttpURLConnection)new URL("http://127.0.0.1:"+port+"/event").openConnection(java.net.Proxy.NO_PROXY);conn.setConnectTimeout(1500);conn.setReadTimeout(1500);conn.setRequestMethod("POST");conn.setRequestProperty("X-XiaomiDesk-Token",token);conn.setRequestProperty("Content-Type","application/json; charset=utf-8");conn.setDoOutput(true);conn.setFixedLengthStreamingMode(body.length);try(java.io.OutputStream out=conn.getOutputStream()){if(!allowed(context,token,port))return;out.write(body);}if(conn.getResponseCode()==200&&allowed(context,token,port))success=System.currentTimeMillis();}catch(Exception ignored){}finally{if(conn!=null)conn.disconnect();}}});
 }
 interface TextResult {void received(String text);}
 static void fetchClipboard(Context c,final TextResult callback){
  final Context context=c.getApplicationContext();SharedPreferences p=prefs(context);final String token=p.getString("token","");final int port=p.getInt("port",0);
  pool.execute(()->{if(!allowed(context,token,port))return;HttpURLConnection conn=null;try{conn=(HttpURLConnection)new URL("http://127.0.0.1:"+port+"/clipboard").openConnection(java.net.Proxy.NO_PROXY);conn.setConnectTimeout(1500);conn.setReadTimeout(1500);conn.setRequestProperty("X-XiaomiDesk-Token",token);if(conn.getResponseCode()!=200)return;java.io.ByteArrayOutputStream out=new java.io.ByteArrayOutputStream();try(java.io.InputStream in=conn.getInputStream()){byte[] buffer=new byte[4096];int n;while((n=in.read(buffer))!=-1){if(out.size()+n>65536)return;out.write(buffer,0,n);}}final String text=new String(out.toByteArray(),StandardCharsets.UTF_8);if(text.length()<=16000&&allowed(context,token,port))callback.received(text);}catch(Exception ignored){}finally{if(conn!=null)conn.disconnect();}});
 }
}
