using System;
using XiaomiDesk;
class InstallHintTest {
 static int Main(){string raw="Failure [INSTALL_FAILED_USER_RESTRICTED: Install canceled by user]";string hint=Core.InstallFailure(raw);if(!hint.Contains("Установка через USB")||!hint.Contains("XiaomiDesk.apk")||!hint.Contains(raw))throw new Exception("Missing actionable guidance");if(Core.InstallFailure("INSTALL_FAILED_INSUFFICIENT_STORAGE")!="INSTALL_FAILED_INSUFFICIENT_STORAGE")throw new Exception("Unrelated error changed");Console.WriteLine("PASS USB restriction guidance; raw error and unrelated errors preserved");return 0;}
}
