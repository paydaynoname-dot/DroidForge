# DroidForge

**Android-приложения на компьютере с Windows.** DroidForge создаёт полноэкранный рабочий стол Windows для Android: запускает приложения в отдельных перетаскиваемых окнах, устанавливает APK и даёт создавать файлы.

> DroidForge не содержит отдельного движка Android. Он управляет официальным Android Emulator из Android SDK. Android Emulator устанавливается отдельно через Android Studio / SDK Manager.

## Возможности

- Создание виртуальных устройств из установленных Android-образов, включая образы с **Google Play Store**. DroidForge автоматически находит установленные версии, в том числе Android 17 (API 37).
- Настройка RAM, количества ядер CPU, разрешения и плотности экрана для каждого телефона.
- Запуск эмулятора с автоматической графикой, GPU компьютера или программным рендерингом.
- Индикатор загрузки Android с процентами по этапам и таймером.
- Понятное сообщение, если запуск остановился, например из-за нехватки места на диске.
- Кнопка запуска открывает DroidForgeDesktop во весь экран Windows. Экран телефона не используется как рабочий стол.
- Приложения Android запускаются в отдельных окнах. Окна можно перемещать, разворачивать и менять размер; управление идёт мышью и клавиатурой компьютера.
- Google Play, Android Settings и установленные приложения доступны с рабочего стола.
- Установка APK через выбор файла на ПК; установленное приложение появляется среди значков рабочего стола.
- Встроенное окно для создания и сохранения текстовых файлов в `Документы\DroidForgeDesktop`.
- Сохранение пути к SDK и режима графики на этом компьютере.

## Что понадобится

- Windows 10 или Windows 11, 64-разрядная версия.
- Android Studio с установленными Android SDK, Android Emulator и Android SDK Platform-Tools.
- Образ Android. Для магазина выбирайте образ с пометкой **Google Play**.
- Включённая аппаратная виртуализация для Android Emulator.

## Настройка Play Store

1. Установите [Android Studio](https://developer.android.com/studio).
2. Откройте **Tools → SDK Manager**.
3. На вкладке **SDK Tools** установите **Android Emulator**, **Android SDK Platform-Tools** и **Android SDK Command-line Tools (latest)**.
4. На вкладке **SDK Platforms** выберите версию Android. Включите **Show Package Details** и установите для неё **Google Play x86_64 System Image**.
5. Откройте DroidForge → **Настройки** и укажите папку Android SDK. Обычно это `%LOCALAPPDATA%\Android\Sdk`.
6. Перейдите в **Мои устройства**, выберите версию с **Google Play Store**, укажите имя и создайте телефон.
7. Один раз запустите созданное устройство, завершите первоначальную настройку Android и войдите в Google Play.
8. После этого кнопка запуска открывает DroidForgeDesktop. Установка APK и запуск приложений выполняются из его рабочего стола.

Образы системы занимают много места и скачиваются через Android SDK; DroidForge их не включает. Образы Google Play доступны только для поддерживаемых версий Android и конфигураций устройства.
Для отдельных окон DroidForge использует официальный [scrcpy](https://github.com/Genymobile/scrcpy) и виртуальные дисплеи Android. Приложения и системный образ Android не заменяются; APK устанавливаются обычным способом через ADB.

## Сборка из исходников

Установите [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), затем выполните:

```powershell
./scripts/download-scrcpy.ps1
dotnet build DroidForge.csproj -c Release
dotnet run --project DroidForge.csproj
```

Чтобы собрать автономное приложение для Windows:

```powershell
./scripts/download-scrcpy.ps1
dotnet publish DroidForge.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Файл приложения появится в `bin/Release/net10.0-windows/win-x64/publish/`.

## Статус проекта

DroidForge — ранний open-source проект. Индикатор запуска показывает этапы загрузки и время ожидания; точный линейный процент Android не сообщает. Некоторые игры могут не поддерживать виртуальные дисплеи Android. Игровые раскладки «клавиша → касание» пока не поддерживаются.

## Лицензия

Исходный код DroidForge распространяется по лицензии MIT. Android, Android Studio, Google Play и связанные с ними названия и логотипы принадлежат Google. DroidForge не содержит SDK-компоненты Android и сервисы Google Play и не распространяет их. Архив Windows включает официальный scrcpy с его лицензией; [исходный код и лицензия scrcpy](https://github.com/Genymobile/scrcpy) принадлежат Genymobile.

Официальная документация: [Android Emulator](https://developer.android.com/studio/run/emulator), [виртуальные устройства](https://developer.android.com/studio/run/managing-avds), [ускорение графики](https://developer.android.com/studio/run/emulator-acceleration).
