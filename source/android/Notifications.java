package app.xiaomidesk;
import android.app.Notification;
import android.service.notification.NotificationListenerService;
import android.service.notification.StatusBarNotification;
import android.os.Bundle;
import java.util.Timer;
import java.util.TimerTask;

public final class Notifications extends NotificationListenerService {
 private Timer timer;
 @Override public void onListenerConnected(){if(timer!=null)timer.cancel();timer=new Timer("XiaomiDesk-ping",true);timer.schedule(new TimerTask(){public void run(){Wire.send(Notifications.this,"hello","XiaomiDesk","","");}},0,10000);}
 @Override public void onListenerDisconnected(){if(timer!=null){timer.cancel();timer=null;}}
 @Override public void onDestroy(){if(timer!=null)timer.cancel();super.onDestroy();}
 @Override public void onNotificationPosted(StatusBarNotification n){
  if(n==null||getPackageName().equals(n.getPackageName()))return;
  Bundle e=n.getNotification().extras;if(e==null)return;
  CharSequence title=e.getCharSequence(Notification.EXTRA_TITLE);CharSequence body=e.getCharSequence(Notification.EXTRA_BIG_TEXT);if(body==null)body=e.getCharSequence(Notification.EXTRA_TEXT);
  if(title==null&&body==null)return;
  Wire.send(this,"notification",n.getPackageName(),title==null?"":title.toString(),body==null?"":body.toString());
 }
}
