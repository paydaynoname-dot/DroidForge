using DroidForge.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DroidForge;

public partial class MainWindow : Window
{
    private readonly UserSettings _settings = UserSettings.Load();
    private AndroidSdk _sdk;
    private IReadOnlyList<DeviceInfo> _devices = [];
    private string? _selectedDevice;
    private string _page = "home";
    private TextBlock? _notice;

    private static readonly Brush Mint = Brush("#70F0C0");
    private static readonly Brush Muted = Brush("#8B97A9");
    private static readonly Brush White = Brush("#F1F4FA");
    private static readonly Brush Line = Brush("#222B3A");

    public MainWindow()
    {
        InitializeComponent();
        _sdk = new AndroidSdk(_settings.SdkPath);
        Loaded += async (_, _) => { await ReloadAsync(); ShowDashboard(); };
    }

    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color)!);
    private static TextBlock Text(string value, int size = 13, string color = "#F1F4FA", FontWeight? weight = null) =>
        new() { Text = value, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal, TextWrapping = TextWrapping.Wrap, LineHeight = size * 1.48 };

    private static Border Card(UIElement child, int padding = 20, string background = "#101520") => new()
    {
        Background = Brush(background), BorderBrush = Brush("#222B3A"), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(16), Padding = new Thickness(padding), Child = child
    };

    private static StackPanel Vertical(params UIElement[] children)
    {
        var panel = new StackPanel();
        foreach (var child in children) { if (child is FrameworkElement element) element.Margin = new Thickness(0, 0, 0, 10); panel.Children.Add(child); }
        if (panel.Children.Count > 0 && panel.Children[^1] is FrameworkElement last) last.Margin = new Thickness(0);
        return panel;
    }

    private static Button Action(string label, RoutedEventHandler click, bool accent = false)
    {
        var button = new Button { Content = label, Style = (Style)Application.Current.MainWindow.FindResource(accent ? "AccentButton" : typeof(Button)) };
        button.Click += click;
        return button;
    }

    private static Button NavButton(string label, RoutedEventHandler click)
    {
        var button = new Button { Content = label, Style = (Style)Application.Current.MainWindow.FindResource("NavButton") };
        button.Click += click;
        return button;
    }

    private static Border Pill(string label, string fill = "#17251F", string fg = "#70F0C0") => new()
    {
        Background = Brush(fill), CornerRadius = new CornerRadius(20), Padding = new Thickness(10, 5, 10, 5),
        Child = new TextBlock { Text = label, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Brush(fg) }
    };

    private void Header(string eyebrow, string title)
    {
        PageEyebrow.Text = eyebrow;
        PageTitle.Text = title;
    }

    private void ClearPage(string eyebrow, string title)
    {
        Header(eyebrow, title);
        PageHost.Children.Clear();
        _notice = null;
    }

    private void AddNotice(string message, bool error = false)
    {
        if (_notice is null)
        {
            _notice = TextBlockForNotice();
            var border = new Border { Child = _notice, Background = Brush("#111C1A"), CornerRadius = new CornerRadius(10), BorderBrush = Brush(error ? "#70414A" : "#2A5948"), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 14) };
            PageHost.Children.Insert(0, border);
        }
        _notice.Text = message;
        _notice.Foreground = Brush(error ? "#FFA3A3" : "#BDECDD");
        if (_notice.Parent is Border b) b.BorderBrush = Brush(error ? "#70414A" : "#2A5948");
        static TextBlock TextBlockForNotice() => new() { Padding = new Thickness(13), FontSize = 12, TextWrapping = TextWrapping.Wrap };
    }

    private async Task ReloadAsync()
    {
        _sdk.SetRoot(_settings.SdkPath);
        if (_sdk.IsAvailable)
        {
            _devices = await _sdk.ListAvdsAsync();
            SdkBadgeText.Text = "Android SDK готов";
            SdkDot.Fill = Brush("#70F0C0");
            SidebarStatus.Text = "SDK подключён";
            SidebarDevice.Text = _devices.Count == 0 ? "Создайте первый телефон" : $"Устройств: {_devices.Count}";
        }
        else
        {
            _devices = [];
            SdkBadgeText.Text = "SDK не найден";
            SdkDot.Fill = Brush("#F7BD67");
            SidebarStatus.Text = "Нужен Android SDK";
            SidebarDevice.Text = "Откройте настройки для подключения";
        }
        if (_selectedDevice is null || !_devices.Any(d => d.Name == _selectedDevice))
            _selectedDevice = _devices.FirstOrDefault()?.Name;
        if (_page == "devices") ShowDevices();
        else if (_page == "apps") ShowApps();
        else if (_page == "settings") ShowSettings();
        else ShowDashboard();
    }

    private void ShowDashboard()
    {
        _page = "home";
        ClearPage("ANDROID НА КОМПЬЮТЕРЕ", "Твоя игровая платформа");

        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) });
        var hero = new Border { CornerRadius = new CornerRadius(20), Padding = new Thickness(28), Margin = new Thickness(0, 0, 0, 16), MinHeight = 294,
            Background = new LinearGradientBrush(Brush("#172B31").Color, Brush("#171927").Color, 20), BorderBrush = Brush("#2B4144"), BorderThickness = new Thickness(1) };
        var heroContent = new Grid();
        heroContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        heroContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var label = Text("ТВОЙ ANDROID. ТВОИ ПРАВИЛА.", 10, "#70F0C0", FontWeights.SemiBold); label.Margin = new Thickness(0, 0, 0, 14);
        var title = Text("Мобильный мир\nна большом экране", 30, "#F4F7FB", FontWeights.SemiBold); title.LineHeight = 39; title.Margin = new Thickness(0, 0, 0, 10);
        var desc = Text("Запускай Android‑приложения в окне, управляй мышью и клавиатурой, подбирай память и разрешение для каждого телефона.", 13, "#B4C1CD"); desc.MaxWidth = 500; desc.Margin = new Thickness(0, 0, 0, 20);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(Action(_devices.Count == 0 ? "＋  Создать телефон" : "▶  Запустить устройство", async (_, _) => await LaunchSelectedAsync(), true));
        var devicesButton = Action("Мои устройства", (_, _) => { _page = "devices"; ShowDevices(); }); devicesButton.Margin = new Thickness(10, 0, 0, 0); actions.Children.Add(devicesButton);
        copy.Children.Add(label); copy.Children.Add(title); copy.Children.Add(desc); copy.Children.Add(actions);
        Grid.SetColumn(copy, 0); heroContent.Children.Add(copy);

        var preview = CreatePhonePreview(); Grid.SetColumn(preview, 1); heroContent.Children.Add(preview);
        hero.Child = heroContent; PageHost.Children.Add(hero);

        var metrics = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        for (var i = 0; i < 3; i++) metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metrics.Children.Add(MetricCard("УСТРОЙСТВА", _devices.Count.ToString(), _devices.Count == 1 ? "виртуальный телефон" : "виртуальных телефонов", "▣", 0));
        metrics.Children.Add(MetricCard("ПОДКЛЮЧЕНИЕ", _sdk.IsAvailable ? "SDK готов" : "Нужна настройка", _sdk.IsAvailable ? "эмулятор и ADB обнаружены" : "займёт пару минут", "⌁", 1));
        metrics.Children.Add(MetricCard("PLAY STORE", "Google Play", "доступен с образом Google Play", "▷", 2));
        PageHost.Children.Add(metrics);

        var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
        var devicesContent = new StackPanel(); devicesContent.Children.Add(Text("Твои устройства", 17, "#F1F4FA", FontWeights.SemiBold));
        devicesContent.Children.Add(Text(_devices.Count == 0 ? "Создай первый виртуальный телефон и выбери образ с Google Play." : "Устройства на базе Android Emulator.", 12, "#8B97A9"));
        if (_devices.Count == 0) devicesContent.Children.Add(Action("Создать виртуальный телефон →", (_, _) => ShowDevices(), true));
        else
        {
            var picker = new ComboBox { ItemsSource = _devices.Select(d => d.Name).ToList(), SelectedItem = _selectedDevice, Margin = new Thickness(0, 8, 0, 12), Height = 36 };
            picker.SelectionChanged += (_, _) => _selectedDevice = picker.SelectedItem as string;
            devicesContent.Children.Add(picker);
            devicesContent.Children.Add(Text(_devices.FirstOrDefault(d => d.Name == _selectedDevice)?.Resolution ?? "", 11, "#8794A8"));
            devicesContent.Children.Add(Action("▶  Запустить выбранный телефон", async (_, _) => await LaunchSelectedAsync(), true));
        }
        var devicesCard = Card(devicesContent); Grid.SetColumn(devicesCard, 0); row.Children.Add(devicesCard);
        var tips = new StackPanel(); tips.Children.Add(Text("Мышь + клавиатура", 17, "#F1F4FA", FontWeights.SemiBold));
        tips.Children.Add(Text("Android Emulator передаёт щелчки и перетаскивание как касания экрана. Ввод текста идёт с клавиатуры ПК; Ctrl и прокрутка мыши поддерживают жесты.", 12, "#A5B0C2"));
        var playLine = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        playLine.Children.Add(Pill("GOOGLE PLAY")); var playCopy = Text("  Выбери образ Android с Google Play", 11, "#B7C2D0"); playCopy.VerticalAlignment = VerticalAlignment.Center; playLine.Children.Add(playCopy); tips.Children.Add(playLine);
        var tipsCard = Card(tips); Grid.SetColumn(tipsCard, 1); row.Children.Add(tipsCard); PageHost.Children.Add(row);
    }

    private Border CreatePhonePreview()
    {
        var outer = new Border { Width = 178, Height = 252, CornerRadius = new CornerRadius(29), Background = Brush("#080D13"), BorderBrush = Brush("#60737D"), BorderThickness = new Thickness(2), Padding = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, RenderTransform = new RotateTransform(-5) };
        var screen = new Grid { ClipToBounds = true };
        screen.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        screen.Background = new LinearGradientBrush(Brush("#1B5551").Color, Brush("#263054").Color, 45);
        var islands = new Ellipse { Width = 145, Height = 145, Fill = Brush("#527E76"), Opacity = .28, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 32, -35, 0) }; screen.Children.Add(islands);
        var widgets = new StackPanel { Margin = new Thickness(13, 22, 13, 8), VerticalAlignment = VerticalAlignment.Top };
        var status = Text("9:41                                      ▴  ▪", 8, "#E2F1EC"); status.Margin = new Thickness(2, 0, 0, 26);
        var clock = Text(DateTime.Now.ToString("HH:mm"), 30, "#FFFFFF", FontWeights.Light); clock.Margin = new Thickness(2, 0, 0, 0);
        var date = Text(DateTime.Now.ToString("ddd, MMM d"), 8, "#D0DFE0"); date.Margin = new Thickness(3, 0, 0, 20);
        widgets.Children.Add(status); widgets.Children.Add(clock); widgets.Children.Add(date);
        var pills = new UniformGrid { Columns = 4, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        foreach (var (icon, color) in new[] { ("▶", "#69C9A3"), ("◎", "#5979DB"), ("▦", "#E6AD5A"), ("✉", "#538CDA") })
            pills.Children.Add(new Border { Width = 26, Height = 26, CornerRadius = new CornerRadius(8), Background = Brush(color), Margin = new Thickness(4), Child = new TextBlock { Text = icon, Foreground = White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center } });
        widgets.Children.Add(pills); Grid.SetRow(widgets, 0); screen.Children.Add(widgets);
        outer.Child = screen; return outer;
    }

    private Border MetricCard(string label, string value, string caption, string icon, int column)
    {
        var content = new Grid(); content.ColumnDefinitions.Add(new ColumnDefinition()); content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var stack = new StackPanel(); stack.Children.Add(Text(label, 9, "#7F8BA0", FontWeights.SemiBold));
        var valueText = Text(value, 17, "#F0F3F8", FontWeights.SemiBold); valueText.Margin = new Thickness(0, 5, 0, 1); stack.Children.Add(valueText); stack.Children.Add(Text(caption, 10, "#8491A4")); content.Children.Add(stack);
        var glyph = new Border { Width = 31, Height = 31, CornerRadius = new CornerRadius(9), Background = Brush("#172421"), Child = new TextBlock { Text = icon, Foreground = Mint, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } }; Grid.SetColumn(glyph, 1); content.Children.Add(glyph);
        var card = Card(content, 16); card.Margin = new Thickness(column == 0 ? 0 : 6, 0, column == 2 ? 0 : 6, 0); Grid.SetColumn(card, column); return card;
    }

    private void ShowDevices()
    {
        _page = "devices";
        ClearPage("ПРОФИЛИ ANDROID", "Мои устройства");
        var intro = new Grid { Margin = new Thickness(0, 0, 0, 18) }; intro.ColumnDefinitions.Add(new ColumnDefinition()); intro.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var sub = Text("Создавай телефоны с нужным экраном, памятью и Google Play.", 12, "#8B97A9"); Grid.SetRow(sub, 1);
        var introText = new StackPanel(); introText.Children.Add(Text("Каждый профиль — отдельное виртуальное устройство.", 12, "#8B97A9")); intro.Children.Add(introText);
        var scan = Action("Обновить список", async (_, _) => await ReloadAsync()); Grid.SetColumn(scan, 1); intro.Children.Add(scan); PageHost.Children.Add(intro);

        if (_devices.Count == 0)
        {
            var empty = new StackPanel(); empty.Children.Add(Text("Пока нет виртуальных устройств", 19, "#F1F4FA", FontWeights.SemiBold));
            empty.Children.Add(Text(_sdk.IsAvailable ? "Создай виртуальный телефон. Для Google Play выбери образ с пометкой Google Play." : "Сначала установи Android Studio и укажи путь к SDK в настройках.", 12, "#99A6B8"));
            if (!_sdk.IsAvailable) empty.Children.Add(Action("Открыть настройки SDK", (_, _) => ShowSettings(), true));
            PageHost.Children.Add(Card(empty, 24));
        }
        else
        {
            foreach (var device in _devices)
            {
                var cardGrid = new Grid(); cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); cardGrid.ColumnDefinitions.Add(new ColumnDefinition()); cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var phoneGlyph = new Border { Width = 46, Height = 55, CornerRadius = new CornerRadius(10), Background = Brush("#1B2831"), BorderBrush = Brush("#31414C"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = "▯", Foreground = Mint, FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } }; phoneGlyph.Margin = new Thickness(0, 0, 16, 0); cardGrid.Children.Add(phoneGlyph);
                var details = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; details.Children.Add(Text(device.Name, 15, "#F1F4FA", FontWeights.SemiBold)); details.Children.Add(Text($"{device.Resolution}   ·   {device.CpuCores} ядра   ·   {device.RamMb / 1024d:0.#} ГБ RAM", 11, "#8693A6")); details.Margin = new Thickness(0, 2, 0, 0); Grid.SetColumn(details, 1); cardGrid.Children.Add(details);
                var controls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                controls.Children.Add(Action("Настроить", (_, _) => { _selectedDevice = device.Name; ShowDeviceEditor(device); }));
                var run = Action("▶  Запустить", async (_, _) => { _selectedDevice = device.Name; await LaunchSelectedAsync(); }, true); run.Margin = new Thickness(8, 0, 0, 0); controls.Children.Add(run);
                Grid.SetColumn(controls, 2); cardGrid.Children.Add(controls);
                var card = Card(cardGrid, 18); card.Margin = new Thickness(0, 0, 0, 10); PageHost.Children.Add(card);
            }
        }

        PageHost.Children.Add(new Border { Height = 1, Background = Line, Margin = new Thickness(0, 22, 0, 18) });
        PageHost.Children.Add(Text("СОЗДАТЬ УСТРОЙСТВО", 10, "#70F0C0", FontWeights.SemiBold));
        PageHost.Children.Add(Text("Выбери Android образ. Чтобы пользоваться магазином, нужен образ Google Play.", 12, "#8B97A9"));
        var form = new Grid(); form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
        var nameBox = new TextBox { Text = "Pixel_Play", Margin = new Thickness(0, 9, 10, 0), Height = 39 };
        var images = new ComboBox { Margin = new Thickness(0, 9, 0, 0), Height = 39 };
        var installedImages = _sdk.ListSystemImages();
        foreach (var image in installedImages) images.Items.Add(image);
        if (installedImages.Count == 0)
        {
            images.Items.Add("Сначала установи образ Android в SDK Manager");
            images.SelectedIndex = 0;
            images.IsEnabled = false;
        }
        else
        {
            var playIndex = installedImages.ToList().FindIndex(image => image.IncludesPlayStore);
            images.SelectedIndex = playIndex >= 0 ? playIndex : 0;
        }
        Grid.SetColumn(nameBox, 0); Grid.SetColumn(images, 1); form.Children.Add(nameBox); form.Children.Add(images); PageHost.Children.Add(form);
        var info = Text("Показываются образы, уже установленные в SDK. Google Play Store будет внутри телефона, если выбрать образ с такой пометкой.", 11, "#818EA2"); info.Margin = new Thickness(0, 8, 0, 0); PageHost.Children.Add(info);
        var create = Action("＋  Создать телефон", async (_, _) =>
        {
            if (!_sdk.IsAvailable) { AddNotice("Подключи Android SDK в настройках перед созданием устройства.", true); return; }
            var option = images.SelectedItem as SystemImageInfo;
            var name = nameBox.Text.Trim().Replace(' ', '_');
            if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z0-9_.-]+$")) { AddNotice("Имя устройства: латинские буквы, цифры, точка, дефис или подчёркивание.", true); return; }
            if (_devices.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) { AddNotice("Устройство с таким именем уже есть.", true); return; }
            if (option is null) { AddNotice("В SDK пока нет системного образа. В Android Studio открой SDK Manager и установи образ Google Play.", true); return; }
            var result = await _sdk.CreateAvdAsync(name, option.PackageId);
            if (result.ExitCode != 0) { AddNotice("Не удалось создать устройство: " + (result.Error + " " + result.Output).Trim(), true); return; }
            _selectedDevice = name; await ReloadAsync(); AddNotice($"Устройство {name} создано. Нажми «Запустить», чтобы открыть Android.");
        }, true);
        create.Margin = new Thickness(0, 14, 0, 0); PageHost.Children.Add(create);
    }

    private void ShowDeviceEditor(DeviceInfo device)
    {
        _page = "devices";
        ClearPage("НАСТРОЙКА ПРОФИЛЯ", device.Name);
        PageHost.Children.Add(Text("Изменения применятся при следующем запуске устройства.", 12, "#8B97A9"));
        var fields = new Grid { Margin = new Thickness(0, 16, 0, 14) };
        fields.ColumnDefinitions.Add(new ColumnDefinition()); fields.ColumnDefinitions.Add(new ColumnDefinition());
        var ram = new TextBox { Text = device.RamMb.ToString(), Margin = new Thickness(0, 7, 12, 13), Height = 38 };
        var cores = new TextBox { Text = device.CpuCores.ToString(), Margin = new Thickness(0, 7, 0, 13), Height = 38 };
        var width = new TextBox { Text = device.Resolution.Contains('×') ? device.Resolution.Split('×')[0].Trim() : "1080", Margin = new Thickness(0, 7, 12, 13), Height = 38 };
        var height = new TextBox { Text = device.Resolution.Contains('×') ? device.Resolution.Split('×')[1].Trim() : "1920", Margin = new Thickness(0, 7, 0, 13), Height = 38 };
        var density = new TextBox { Text = "420", Margin = new Thickness(0, 7, 12, 13), Height = 38 };
        AddField(fields, "Память RAM, МБ", ram, 0, 0); AddField(fields, "Количество ядер", cores, 1, 0);
        AddField(fields, "Ширина экрана, px", width, 0, 1); AddField(fields, "Высота экрана, px", height, 1, 1); AddField(fields, "Плотность экрана, dpi", density, 0, 2);
        PageHost.Children.Add(fields);
        var play = Text("Google Play определяется Android образом устройства. Чтобы добавить его в существующее устройство, создай отдельный профиль с образом Google Play.", 11, "#9AA7B8"); PageHost.Children.Add(Card(play, 14));
        var save = Action("Сохранить параметры", async (_, _) =>
        {
            if (!int.TryParse(ram.Text, out var r) || r is < 512 or > 16384 || !int.TryParse(cores.Text, out var c) || c is < 1 or > 16 ||
                !int.TryParse(width.Text, out var w) || w is < 320 or > 7680 || !int.TryParse(height.Text, out var h) || h is < 480 or > 7680 || !int.TryParse(density.Text, out var d) || d is < 72 or > 1000)
            { AddNotice("Проверь значения: RAM 512–16384 МБ, ядра 1–16, ширина 320–7680, высота 480–7680, dpi 72–1000.", true); return; }
            var current = _sdk.ReadDevice(device.Name);
            var result = await _sdk.ApplyDeviceConfigAsync(current, r, c, w, h, d);
            if (result.ExitCode != 0) { AddNotice("Не удалось сохранить настройки: " + result.Error, true); return; }
            await ReloadAsync(); AddNotice("Параметры сохранены. Перезапусти устройство, чтобы применить изменения.");
        }, true);
        save.Margin = new Thickness(0, 16, 0, 0); PageHost.Children.Add(save);
        PageHost.Children.Add(Action("←  К устройствам", (_, _) => ShowDevices()));
    }

    private static void AddField(Grid grid, string label, TextBox box, int column, int row)
    {
        while (grid.RowDefinitions.Count <= row) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var stack = new StackPanel { Margin = new Thickness(column == 0 ? 0 : 12, 0, column == 0 ? 12 : 0, 0) };
        stack.Children.Add(Text(label, 11, "#8B97A9")); stack.Children.Add(box); Grid.SetRow(stack, row); Grid.SetColumn(stack, column); grid.Children.Add(stack);
    }

    private void ShowApps()
    {
        _page = "apps";
        ClearPage("ИГРЫ И ПРИЛОЖЕНИЯ", "Твоя библиотека");
        var intro = new StackPanel(); intro.Children.Add(Text("Установи APK и запускай приложения на виртуальном Android.", 12, "#8B97A9"));
        intro.Children.Add(Text("Для Google Play создай устройство с образом Google Play Store, войди в аккаунт прямо на нём и устанавливай приложения из магазина.", 12, "#A9B5C4"));
        PageHost.Children.Add(Card(intro, 17));

        var installStack = new StackPanel(); installStack.Children.Add(Text("Установка APK", 17, "#F1F4FA", FontWeights.SemiBold));
        installStack.Children.Add(Text("APK будет установлен в первое запущенное Android‑устройство.", 11, "#8B97A9"));
        installStack.Children.Add(Action("Выбрать APK и установить", async (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = "Android-приложение (*.apk)|*.apk", Title = "Выбери APK для установки" };
            if (picker.ShowDialog(this) != true) return;
            if (!_sdk.IsAvailable) { AddNotice("Сначала подключи Android SDK в настройках.", true); return; }
            AddNotice("Устанавливаю приложение…");
            var result = await _sdk.AdbAsync("install", "-r", picker.FileName);
            if (result.ExitCode != 0) AddNotice("Не удалось установить APK. Убедись, что виртуальное устройство запущено. " + result.Error, true);
            else { AddNotice("APK успешно установлен на устройство."); await RefreshPackagesAsync(); }
        }, true));
        PageHost.Children.Add(Card(installStack, 19));

        var library = new StackPanel();
        var heading = new Grid(); heading.ColumnDefinitions.Add(new ColumnDefinition()); heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headingText = Text("Установленные приложения", 17, "#F1F4FA", FontWeights.SemiBold); heading.Children.Add(headingText);
        var refresh = Action("Обновить", async (_, _) => await RefreshPackagesAsync()); Grid.SetColumn(refresh, 1); heading.Children.Add(refresh); library.Children.Add(heading);
        library.Children.Add(Text("Выбери приложение, чтобы открыть его в эмуляторе.", 11, "#8B97A9"));
        var list = new StackPanel { Name = "PackageList" };
        list.Children.Add(Text("Список появится после запуска устройства.", 11, "#768296")); library.Children.Add(list);
        PageHost.Children.Add(Card(library, 19));
    }

    private async Task RefreshPackagesAsync()
    {
        var packages = await _sdk.ListPackagesAsync();
        if (PageHost.Children.Count == 0) return;
        var host = FindPackageList(PageHost);
        if (host is null) return;
        host.Children.Clear();
        if (packages.Count == 0) { host.Children.Add(Text("Нет запущенного Android‑устройства или приложений.", 11, "#768296")); return; }
        foreach (var packageName in packages)
        {
            var row = new Grid { Margin = new Thickness(0, 6, 0, 0) }; row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(Text(packageName, 12, "#DDE4EE"));
            var launch = Action("Открыть", async (_, _) =>
            {
                var result = await _sdk.OpenPackageAsync(packageName);
                if (result.ExitCode != 0) AddNotice("Не удалось открыть приложение: " + result.Error, true);
            }); Grid.SetColumn(launch, 1); row.Children.Add(launch); host.Children.Add(row);
        }
    }

    private static StackPanel? FindPackageList(DependencyObject parent)
    {
        if (parent is StackPanel sp && sp.Name == "PackageList") return sp;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var found = FindPackageList(VisualTreeHelper.GetChild(parent, i)); if (found is not null) return found;
        }
        return null;
    }

    private void ShowSettings()
    {
        _page = "settings";
        ClearPage("ПОДКЛЮЧЕНИЕ И ПРОИЗВОДИТЕЛЬНОСТЬ", "Настройки");
        var sdkCard = new StackPanel(); sdkCard.Children.Add(Text("Android SDK", 17, "#F1F4FA", FontWeights.SemiBold));
        sdkCard.Children.Add(Text("DroidForge использует Android Emulator, ADB и Android SDK, установленные на компьютере.", 12, "#94A1B3"));
        var pathRow = new Grid { Margin = new Thickness(0, 13, 0, 8) }; pathRow.ColumnDefinitions.Add(new ColumnDefinition()); pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var path = new TextBox { Text = _settings.SdkPath ?? _sdk.Root ?? "", Height = 40, VerticalContentAlignment = VerticalAlignment.Center };
        pathRow.Children.Add(path);
        var browse = Action("Обзор…", (_, _) =>
        {
            var dialog = new OpenFolderDialog { Title = "Выбери корневую папку Android SDK", Multiselect = false };
            if (dialog.ShowDialog(this) == true) path.Text = dialog.FolderName;
        }); browse.Margin = new Thickness(8, 0, 0, 0); Grid.SetColumn(browse, 1); pathRow.Children.Add(browse); sdkCard.Children.Add(pathRow);
        sdkCard.Children.Add(Action("Сохранить и проверить SDK", async (_, _) =>
        {
            _settings.SdkPath = string.IsNullOrWhiteSpace(path.Text) ? null : path.Text.Trim(); _settings.Save();
            await ReloadAsync(); AddNotice(_sdk.IsAvailable ? "Android SDK подключён. Устройства и ADB готовы." : "Не нашёл emulator/emulator.exe и platform-tools/adb.exe. Проверь корневую папку SDK.", !_sdk.IsAvailable);
        }, true));
        sdkCard.Children.Add(Text("Стандартное расположение Windows: %LOCALAPPDATA%\\Android\\Sdk", 10, "#78869A"));
        PageHost.Children.Add(Card(sdkCard, 20));

        var performance = new StackPanel(); performance.Children.Add(Text("Графика", 17, "#F1F4FA", FontWeights.SemiBold));
        performance.Children.Add(Text("Режим ускорения Android Emulator. «Auto» подходит большинству компьютеров.", 11, "#8B97A9"));
        var gpu = new ComboBox { Margin = new Thickness(0, 10, 0, 12), Height = 38, Width = 270, HorizontalAlignment = HorizontalAlignment.Left };
        gpu.Items.Add(new GpuOption("Auto · рекомендуется", "auto")); gpu.Items.Add(new GpuOption("GPU компьютера · Host", "host")); gpu.Items.Add(new GpuOption("Программная графика", "software"));
        gpu.SelectedItem = gpu.Items.Cast<GpuOption>().FirstOrDefault(o => o.Mode == _settings.GpuMode) ?? gpu.Items[0];
        performance.Children.Add(gpu);
        performance.Children.Add(Action("Сохранить режим графики", (_, _) =>
        {
            if (gpu.SelectedItem is GpuOption option) { _settings.GpuMode = option.Mode; _settings.Save(); AddNotice("Режим графики сохранён."); }
        }, true));
        PageHost.Children.Add(Card(performance, 20));

        var setup = new StackPanel(); setup.Children.Add(Text("Нужен Android Studio?", 17, "#F1F4FA", FontWeights.SemiBold));
        setup.Children.Add(Text("SDK Manager устанавливает Android Emulator, Platform Tools и образы системы. Включи Show Package Details, чтобы выбрать образ Google Play Store.", 12, "#A1ADBE"));
        setup.Children.Add(Action("Открыть страницу Android Studio →", (_, _) => OpenUrl("https://developer.android.com/studio")));
        PageHost.Children.Add(Card(setup, 20));
    }

    private sealed record GpuOption(string Label, string Mode) { public override string ToString() => Label; }

    private async Task LaunchSelectedAsync()
    {
        if (!_sdk.IsAvailable) { _page = "settings"; ShowSettings(); AddNotice("Сначала подключи Android SDK в настройках.", true); return; }
        if (_devices.Count == 0) { _page = "devices"; ShowDevices(); AddNotice("Создай виртуальное устройство перед запуском.", true); return; }
        var name = _selectedDevice ?? _devices[0].Name;
        var result = await _sdk.StartAsync(name, _settings.GpuMode);
        if (result.ExitCode != 0) AddNotice("Не удалось запустить устройство: " + result.Error, true);
        else AddNotice($"{name} запускается. Первое включение Android может занять несколько минут.");
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private void NavHome_Click(object sender, RoutedEventArgs e) => ShowDashboard();
    private void NavDevices_Click(object sender, RoutedEventArgs e) => ShowDevices();
    private void NavApps_Click(object sender, RoutedEventArgs e) => ShowApps();
    private void NavSettings_Click(object sender, RoutedEventArgs e) => ShowSettings();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ReloadAsync();
}
