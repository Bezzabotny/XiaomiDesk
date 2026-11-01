using System;
namespace XiaomiDesk {
 public static class ScreenOptions {
  public static string Arguments(string serial,string title,bool camera,bool hardwareInput){return "--serial "+Core.Arg(serial)+" --window-title "+Core.Arg(title)+" --window-borderless --no-window-aspect-ratio-lock --max-size=1600 --max-fps="+(camera?"30":"45")+(camera?" --video-source=camera --camera-facing=back --no-audio --no-control":hardwareInput?" --keyboard=uhid --mouse=uhid":" --keyboard=sdk --mouse=sdk");}
  public static string Explain(string output){if((output??"").Contains("3221225794")||(output??"").IndexOf("0xC0000142",StringComparison.OrdinalIgnoreCase)>=0)return "Windows не смогла запустить компонент ADB (0xC0000142). Полностью распакуйте приложение, затем закройте XiaomiDesk и запустите его заново. Если ошибка повторяется, сохраните этот журнал для сообщения об ошибке.\n\n"+output;if((output??"").IndexOf("INJECT_EVENTS",StringComparison.OrdinalIgnoreCase)<0)return output;return "Xiaomi отклоняет обычные команды ввода (INJECT_EVENTS), даже если изображение работает. Включите режим физической мыши и клавиатуры в разделе «Экран телефона», остановите и снова запустите экран.\n\n"+output;}
 }
}
