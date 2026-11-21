# DroidForge

**Android-приложения на компьютере с Windows.** DroidForge — удобная оболочка для управления виртуальными Android-телефонами: создавайте профили, настраивайте ресурсы, запускайте эмулятор и устанавливайте APK.

> DroidForge не содержит отдельного движка Android. Он управляет официальным Android Emulator из Android SDK. Android Emulator устанавливается отдельно через Android Studio / SDK Manager.

## Возможности

- Создание виртуальных устройств из установленных Android-образов, включая образы с **Google Play Store**.
- Настройка RAM, количества ядер CPU, разрешения и плотности экрана для каждого телефона.
- Запуск эмулятора с автоматической графикой, GPU компьютера или программным рендерингом.
- Управление мышью: нажатия, свайпы и перетаскивания; ввод текста с клавиатуры компьютера.
- Установка APK, просмотр установленных приложений и их запуск.
- Сохранение пути к SDK и режима графики на этом компьютере.

## Что понадобится

- Windows 10 или Windows 11, 64-разрядная версия.
- Android Studio с установленными Android SDK, Android Emulator и Android SDK Platform-Tools.
- Образ Android. Для магазина выбирайте образ с пометкой **Google Play**.
- Включённая аппаратная виртуализация для Android Emulator.

## Настройка Play Store

1. Установите [Android Studio](https://developer.android.com/studio).
2. Откройте **Tools → SDK Manager**.
3. На вкладке **SDK Tools** установите **Android Emulator** и **Android SDK Platform-Tools**.
4. На вкладке **SDK Platforms** выберите версию Android. Включите **Show Package Details** и установите для неё **Google Play x86_64 System Image**.
5. Откройте DroidForge → **Настройки** и укажите папку Android SDK. Обычно это `%LOCALAPPDATA%\Android\Sdk`.
6. Перейдите в **Мои устройства**, выберите версию с **Google Play Store**, укажите имя и создайте телефон.
7. Запустите устройство и войдите в Google Play внутри Android, чтобы устанавливать приложения из магазина.

Образы системы занимают много места и скачиваются через Android SDK; DroidForge их не включает. Образы Google Play доступны только для поддерживаемых версий Android и конфигураций устройства.

## Сборка из исходников

Установите [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), затем выполните:

```powershell
dotnet build DroidForge.csproj -c Release
dotnet run --project DroidForge.csproj
```

Чтобы собрать автономное приложение для Windows:

```powershell
dotnet publish DroidForge.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Файл приложения появится в `bin/Release/net10.0-windows/win-x64/publish/`.

## Статус проекта

DroidForge — ранний open-source проект. Сейчас Android Emulator открывается в собственном окне, отдельно от оболочки. Игровые раскладки «клавиша → касание» пока не поддерживаются. Android Emulator сам передаёт виртуальному устройству ввод с мыши и клавиатуры компьютера.

## Лицензия

Исходный код DroidForge распространяется по лицензии MIT. Android, Android Studio, Google Play и связанные с ними названия и логотипы принадлежат Google. DroidForge не содержит SDK-компоненты Android и сервисы Google Play и не распространяет их.

Официальная документация: [Android Emulator](https://developer.android.com/studio/run/emulator), [виртуальные устройства](https://developer.android.com/studio/run/managing-avds), [ускорение графики](https://developer.android.com/studio/run/emulator-acceleration).
