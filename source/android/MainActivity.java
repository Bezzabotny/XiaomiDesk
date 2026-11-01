package app.xiaomidesk;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.os.Bundle;
import android.os.Handler;
import android.provider.Settings;
import android.util.Base64;
import android.view.View;
import android.widget.*;
import java.nio.charset.StandardCharsets;

public final class MainActivity extends Activity {
 private TextView status;private EditText text;private Handler handler=new Handler();private String pending="";private String pendingValue="";
 private final Runnable ping=new Runnable(){public void run(){Wire.send(MainActivity.this,"hello","XiaomiDesk","","");showStatus();handler.postDelayed(this,5000);}};
 @Override public void onCreate(Bundle b){super.onCreate(b);build();accept(getIntent());}
 @Override protected void onNewIntent(Intent i){super.onNewIntent(i);setIntent(i);accept(i);}
 @Override protected void onResume(){super.onResume();handler.removeCallbacks(ping);handler.post(ping);}
 @Override protected void onPause(){handler.removeCallbacks(ping);super.onPause();}
 private int dp(int n){return (int)(n*getResources().getDisplayMetrics().density);}
 private void build(){
  ScrollView scroll=new ScrollView(this);scroll.setBackgroundColor(Color.rgb(244,246,242));LinearLayout root=new LinearLayout(this);root.setOrientation(1);root.setPadding(dp(24),dp(56),dp(24),dp(32));scroll.addView(root);setContentView(scroll);
  label(root,"xiaomi desk.",32,true);label(root,"Ваш телефон рядом с Windows",17,false);status=label(root,"Ожидание подключения",15,true);
  label(root,"1. Подключите Xiaomi к ноутбуку по Wi-Fi или USB.\n2. В Windows откройте XiaomiDesk → Подключение → Открыть помощник.\n3. Подтвердите подключение здесь.",15,false);
  button(root,"Разрешить доступ к уведомлениям",new View.OnClickListener(){public void onClick(View v){startActivity(new Intent(Settings.ACTION_NOTIFICATION_LISTENER_SETTINGS));}});
  label(root,"Это разрешение позволяет передавать содержание уведомлений на ваш ноутбук. Его можно отозвать в настройках Android в любой момент.",13,false);
  label(root,"Текст между устройствами",21,true);text=new EditText(this);text.setMinLines(3);text.setMaxLines(8);text.setHint("Текст для ноутбука");root.addView(text,new LinearLayout.LayoutParams(-1,-2));
  button(root,"Взять текст из буфера телефона",new View.OnClickListener(){public void onClick(View v){readClipboard(false);}});
  button(root,"Отправить текст на ноутбук",new View.OnClickListener(){public void onClick(View v){if(!paired())return;Wire.send(MainActivity.this,"clipboard","XiaomiDesk","",text.getText().toString());toast("Отправка текста…");}});
  button(root,"Скопировать текст в Xiaomi",new View.OnClickListener(){public void onClick(View v){writeClipboard(text.getText().toString());}});
  button(root,"Приостановить / возобновить связь",new View.OnClickListener(){public void onClick(View v){SharedPreferences p=Wire.prefs(MainActivity.this);if(p.getString("token","").isEmpty()){toast("Сначала подключите ноутбук");return;}p.edit().putBoolean("enabled",!p.getBoolean("enabled",false)).apply();Wire.cancelPending();showStatus();}});
  button(root,"Забыть подключение",new View.OnClickListener(){public void onClick(View v){new AlertDialog.Builder(MainActivity.this).setTitle("Отключить ноутбук?").setMessage("Передача уведомлений и текста будет остановлена.").setNegativeButton("Отмена",null).setPositiveButton("Отключить",(d,w)->{Wire.prefs(MainActivity.this).edit().clear().apply();Wire.cancelPending();showStatus();}).show();}});
  label(root,"Локально, без облака и аккаунта. Камера и экран включаются из Windows через подтверждённое подключение отладки. Для галереи требуется отдельное разрешение на фото и видео. Доступ к SMS, контактам, микрофону и камере помощник не запрашивает.",13,false);
 }
 private TextView label(LinearLayout root,String value,int size,boolean bold){TextView t=new TextView(this);t.setText(value);t.setTextSize(size);t.setTextColor(Color.rgb(24,48,49));if(bold)t.setTypeface(null,1);t.setPadding(0,dp(10),0,dp(12));root.addView(t);return t;}
 private void button(LinearLayout root,String value,View.OnClickListener click){Button b=new Button(this);b.setText(value);b.setAllCaps(false);b.setTextColor(Color.rgb(24,48,49));b.setOnClickListener(click);LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(-1,-2);p.topMargin=dp(8);root.addView(b,p);}
 private void accept(Intent i){
  if(i==null)return;final String token=i.getStringExtra("token");final int port=i.getIntExtra("port",0);
  if(token==null){showStatus();return;}if(!token.matches("[a-f0-9]{64}")||port<1||port>65535){toast("Некорректное подключение");return;}
  final String action=i.getStringExtra("action");final String value=i.getStringExtra("value");SharedPreferences p=Wire.prefs(this);
  if(!token.equals(p.getString("token",""))||port!=p.getInt("port",0)){
   new AlertDialog.Builder(this).setTitle("Подключить XiaomiDesk?").setMessage("Подтвердите, только если вы сейчас открыли помощник из XiaomiDesk на своём ноутбуке. После подтверждения разрешённые уведомления будут передаваться через подключение отладки.").setNegativeButton("Отмена",null).setPositiveButton("Подключить",(d,w)->{Wire.prefs(this).edit().putString("token",token).putInt("port",port).putBoolean("enabled",true).apply();Wire.success=0;runAction(action,value);Wire.send(this,"hello","XiaomiDesk","","");showStatus();}).show();
  }else{runAction(action,value);Wire.send(this,"hello","XiaomiDesk","","");showStatus();}
 }
 private void runAction(String action,String value){if(action==null)return;if(!paired())return;if(action.startsWith("gallery:")){GalleryExport.start(this,action);return;}if("notifications".equals(action)){startActivity(new Intent(Settings.ACTION_NOTIFICATION_LISTENER_SETTINGS));return;}pending=action;pendingValue=value==null?"":value;handler.postDelayed(()->performPending(),500);}
 @Override public void onRequestPermissionsResult(int request,String[] permissions,int[] results){super.onRequestPermissionsResult(request,permissions,results);if(request==41)GalleryExport.resume(this);}
 @Override public void onWindowFocusChanged(boolean focus){super.onWindowFocusChanged(focus);if(focus)handler.postDelayed(()->performPending(),250);}
 private void performPending(){if(!hasWindowFocus()||pending.isEmpty())return;String a=pending;pending="";pendingValue="";if(!paired())return;if("read".equals(a))readClipboard(true);if("write".equals(a)){SharedPreferences p=Wire.prefs(this);final String token=p.getString("token","");final int port=p.getInt("port",0);Wire.fetchClipboard(this,s->runOnUiThread(()->{if(isFinishing()||!hasWindowFocus()||!Wire.allowed(this,token,port))return;text.setText(s);writeClipboard(s);}));}}
 private void readClipboard(boolean send){ClipboardManager c=(ClipboardManager)getSystemService(Context.CLIPBOARD_SERVICE);ClipData data=c.getPrimaryClip();if(data==null||data.getItemCount()==0){text.setText("");toast("Буфер телефона пуст");if(send)Wire.send(this,"clipboard","XiaomiDesk","","");return;}CharSequence value=data.getItemAt(0).coerceToText(this);String s=Wire.trim(value==null?"":value.toString(),16000);text.setText(s);if(send)Wire.send(this,"clipboard","XiaomiDesk","",s);}
 private void writeClipboard(String s){((ClipboardManager)getSystemService(Context.CLIPBOARD_SERVICE)).setPrimaryClip(ClipData.newPlainText("XiaomiDesk",Wire.trim(s,16000)));toast("Текст скопирован в Xiaomi");}
 private boolean paired(){boolean ok=Wire.prefs(this).getBoolean("enabled",false);if(!ok)toast("Подключите ноутбук или возобновите связь");return ok;}
 private void showStatus(){if(status==null)return;SharedPreferences p=Wire.prefs(this);status.setText(p.getString("token","").isEmpty()?"Ноутбук ещё не подключён":!p.getBoolean("enabled",false)?"Связь приостановлена":System.currentTimeMillis()-Wire.success<20000?"● Ноутбук на связи":"Ожидание ответа ноутбука…");}
 private void toast(String s){Toast.makeText(this,s,Toast.LENGTH_SHORT).show();}
}
