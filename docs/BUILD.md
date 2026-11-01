# Сборка

Windows11 x64, Windows PowerShell5.1 или PowerShell7, .NET Framework4.x, JDK17+ и Android SDK с API35 / build-tools35.0.0. Дополнительные NuGet-зависимости не нужны.

Из корня репозитория:

```powershell
$env:JAVA_HOME = 'C:\path\to\jdk'
./scripts/test.ps1
./source/build-desktop.ps1
./source/build-android.ps1 -BuildTools "$env:ANDROID_HOME/build-tools/35.0.0" -AndroidJar "$env:ANDROID_HOME/platforms/android-35/android.jar"
./scripts/fetch-tools.ps1
./scripts/package.ps1 -Development
```

Desktop компилируется штатным C#5 компилятором .NET Framework. Результат: `dist/`; архив и контрольная сумма: `.build/`. Автоматические тесты не требуют телефона; проверки реального устройства запускаются отдельно скриптами `source/test-phone-*.ps1` и могут запрашивать разрешения на Android.

Без параметров подписи создаётся ключ разработки в `.build/development-only.p12`. Такой APK предназначен для локальной разработки и не обновит приложение с другим ключом. Не устанавливайте его поверх пользовательского релиза. Для релиза используйте [инструкцию выпуска](RELEASING.md).

Подписывающий ключ, APK, EXE, зависимости, настройки и временные данные исключены из Git. Не добавляйте их через `git add -f`.
