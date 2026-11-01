using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace XiaomiDesk {
 public partial class Hub {
  void Connection(FlowLayoutPanel p){
   Title(p,"Подключение по Wi-Fi","Ноутбук остаётся в своей Wi-Fi-сети. Точку доступа телефона включать не нужно.");
   savedInfo=LabelText("",11,true,820);savedInfo.Height=48;p.Controls.Add(savedInfo);UpdateSavedInfo();
   autoCheck=new CheckBox{Text="Подключаться к сохранённому телефону при запуске приложения",Checked=settings.AutoConnect,AutoSize=false,Width=820,Height=32,ForeColor=ink,Margin=new Padding(0,0,0,10)};
   autoCheck.CheckedChanged+=delegate{reconnectVersion++;settings.AutoConnect=autoCheck.Checked;try{settingsStore.Save(settings);lastAutoAttempt=DateTime.MinValue;SetWifiFeedback(settings.AutoConnect?"Автоподключение включено. Нужны общая сеть и беспроводная отладка на телефоне.":"Автоподключение выключено. Можно подключиться кнопкой вручную.");}catch(Exception e){MessageBox.Show(this,"Не удалось сохранить настройку: "+e.Message,"XiaomiDesk");}};p.Controls.Add(autoCheck);
   var r=Row(p);r.Controls.Add(Btn("Подключить сохранённый",async delegate{await ReconnectSaved(true);},true,265));r.Controls.Add(Btn("Запомнить выбранный",async delegate{await RememberCurrent(true);},false,235));r.Controls.Add(Btn("Забыть телефон",ForgetSaved,false,180));
   wifiFeedback=LabelText("Первое подключение: прокрутите вниз и выполните шаги 1–3. Если телефон уже сохранён, нажмите кнопку выше.",10,false,820);wifiFeedback.Height=48;p.Controls.Add(wifiFeedback);
   Card(p,"1. Подключите оба устройства к одному роутеру","Например, ноутбук и Xiaomi подключены к домашней сети Home. Нельзя оставить телефон только на мобильном интернете. В гостевой/общественной сети устройства могут быть изолированы друг от друга.");
   Card(p,"2. Один раз выполните сопряжение","На Xiaomi: Настройки → Расширенные настройки → Для разработчиков → Беспроводная отладка. Включите её и нажмите на название пункта. Выберите «Сопряжение с помощью кода» (название может немного отличаться).");
   p.Controls.Add(LabelText("В появившемся на Xiaomi окне есть шестизначный код и IP-адрес с портом. Оставьте это окно открытым и перенесите значения сюда. Пример адреса: 192.168.1.10:37000.",10,false,820));
   var pairAddress=new TextBox{Width=310,Margin=new Padding(0,6,12,0)};var pairCode=new TextBox{Width=120,MaxLength=6,UseSystemPasswordChar=true,Margin=new Padding(0,6,12,0)};
   r=Row(p);r.Height=28;r.Controls.Add(LabelText("Адрес из окна с кодом (IP:порт)",10,true,310));r.Controls.Add(LabelText("Код: 6 цифр",10,true,130));
   r=Row(p);r.Controls.Add(pairAddress);r.Controls.Add(pairCode);r.Controls.Add(Btn("Сопрячь устройства",async delegate{try{await PairWifi(pairAddress.Text.Trim(),pairCode.Text.Trim());}finally{pairCode.Clear();}},true,250));
   Card(p,"3. Подключите и сохраните телефон","На Xiaomi закройте маленькое окно с кодом. На главном экране «Беспроводная отладка» найдите строку «IP-адрес и порт». Введите её ниже. Порт обычно другой — код из шага 2 здесь уже не нужен.");
   p.Controls.Add(LabelText("Адрес с главного экрана «Беспроводная отладка» (IP:порт):",10,true,820));var connectAddress=new TextBox{Width=430,Text=settings.LastEndpoint,Margin=new Padding(0,6,12,0)};
   r=Row(p);r.Controls.Add(connectAddress);r.Controls.Add(Btn("Подключить и запомнить",async delegate{await ConnectAndRemember(connectAddress.Text.Trim());},true,285));
   Card(p,"При следующих запусках","Оставьте Wi-Fi и беспроводную отладку включёнными на Xiaomi. Программа найдёт сохранённый телефон и проверит его идентификатор. Если Android выключил отладку после перезагрузки или смены сети, включите её на телефоне снова. Повторять шестизначный код обычно не нужно.");
   p.Controls.Add(LabelText("Помощник для уведомлений и буфера обмена",17,true,820));p.Controls.Add(LabelText("Если помощник уже установлен, переустанавливать его не нужно. Один раз откройте его кнопкой ниже и подтвердите подключение на Xiaomi. Настройки доверия будут сохранены для следующих запусков.",10,false,820));
   r=Row(p);r.Controls.Add(Btn("Открыть помощник",async delegate{await Configure();},true,245));r.Controls.Add(Btn("Установить помощник",Install,false,245));
   p.Controls.Add(LabelText("Если Wi-Fi не подключается",17,true,820));Card(p,"Проверьте адрес и сеть","Оба устройства должны быть в одной обычной Wi-Fi-сети. Если «Сопрячь» прошло, а подключение нет — проверьте адрес именно из шага 3. При смене порта программа попробует найти новый адрес автоматически; если роутер блокирует обнаружение, введите текущий адрес вручную.");
   p.Controls.Add(LabelText("Запасной вариант: USB",17,true,820));Card(p,"Подключение кабелем","На Xiaomi включите «Отладка по USB», подключите кабель с передачей данных и подтвердите доверие к компьютеру. Выберите телефон сверху и нажмите «Запомнить выбранный». Для установки помощника на Xiaomi может понадобиться «Установка через USB».");
   p.Controls.Add(LabelText("Если экран виден, но мышь не работает, проверьте «Отладка по USB (Настройки безопасности)», если такой пункт есть в вашей HyperOS.",10,false,820));
  }
 }
}
