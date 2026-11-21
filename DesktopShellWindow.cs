using DroidForge.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DroidForge;

/// <summary>A Windows desktop for Android apps. Android apps open in their own movable scrcpy windows.</summary>
public sealed class DesktopShellWindow : Window
{
    private readonly AndroidSdk _sdk;
    private readonly string _deviceName;
    private readonly WrapPanel _icons = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _clock = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(15) };
    private DesktopFilesWindow? _filesWindow;

    private static readonly string[] AccentColors = ["#70F0C0", "#80B6FF", "#F5BC68", "#D99AFF", "#FE8B96", "#66D7DC"];

    public DesktopShellWindow(AndroidSdk sdk, string deviceName)
    {
        _sdk = sdk;
        _deviceName = deviceName;
        Title = "DroidForgeDesktop";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Normal;
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Background = new LinearGradientBrush(ColorFrom("#132A30"), ColorFrom("#101625"), 45);
        Foreground = ColorBrush("#F2F5FA");
        FontFamily = new FontFamily("Segoe UI");
        KeyDown += HandleShortcut;
        BuildInterface();
        Loaded += async (_, _) => await ReloadAppsAsync();
        Closed += (_, _) => _timer.Stop();
        _timer.Tick += (_, _) => _clock.Text = DateTime.Now.ToString("HH:mm");
        _timer.Start();
    }

    private void BuildInterface()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(66) });

        var top = new Border
        {
            Background = ColorBrush("#CC0A101A"),
            BorderBrush = ColorBrush("#263447"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(24, 0, 22, 0)
        };
        var topContent = new Grid();
        topContent.ColumnDefinitions.Add(new ColumnDefinition());
        topContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var mark = new Border
        {
            Width = 34, Height = 34, CornerRadius = new CornerRadius(11), Background = ColorBrush("#70F0C0"),
            Child = new TextBlock { Text = "D", Foreground = ColorBrush("#07140F"), FontSize = 21, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        brand.Children.Add(mark);
        var brandText = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(11, 0, 0, 0) };
        brandText.Children.Add(new TextBlock { Text = "DROIDFORGE DESKTOP", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = ColorBrush("#F2F5FA") });
        brandText.Children.Add(new TextBlock { Text = _deviceName + "  ·  Android на ПК", FontSize = 10, Foreground = ColorBrush("#91A0B5"), Margin = new Thickness(0, 3, 0, 0) });
        brand.Children.Add(brandText);
        topContent.Children.Add(brand);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(new TextBlock { Text = "Esc — выйти   ·   F11 — окно", FontSize = 10, Foreground = ColorBrush("#90A0B4"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 21, 0) });
        _clock.Text = DateTime.Now.ToString("HH:mm");
        right.Children.Add(new TextBlock { Text = _clock.Text, FontSize = 17, FontWeight = FontWeights.SemiBold, Foreground = ColorBrush("#F2F5FA"), VerticalAlignment = VerticalAlignment.Center });
        var close = SmallButton("×", "Закрыть DroidForgeDesktop", (_, _) => Close());
        close.Margin = new Thickness(22, 0, 0, 0);
        right.Children.Add(close);
        Grid.SetColumn(right, 1);
        topContent.Children.Add(right);
        top.Child = topContent;
        root.Children.Add(top);

        var content = new Grid { Margin = new Thickness(30, 27, 30, 20) };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        var desktop = new StackPanel();
        var heading = new Grid();
        heading.ColumnDefinitions.Add(new ColumnDefinition());
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headingCopy = new StackPanel();
        headingCopy.Children.Add(new TextBlock { Text = "Рабочий стол", FontSize = 24, FontWeight = FontWeights.SemiBold, Foreground = ColorBrush("#F2F5FA") });
        headingCopy.Children.Add(new TextBlock { Text = "Приложения открываются в отдельных окнах. Их можно двигать и менять размер.", FontSize = 12, Foreground = ColorBrush("#A5B3C5"), Margin = new Thickness(1, 5, 0, 0) });
        heading.Children.Add(headingCopy);
        var refreshApps = ActionButton("↻  Обновить значки", async (_, _) => await ReloadAppsAsync());
        refreshApps.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(refreshApps, 1);
        heading.Children.Add(refreshApps);
        desktop.Children.Add(heading);
        var appScroller = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 18, 0, 0) };
        appScroller.Content = _icons;
        desktop.Children.Add(appScroller);
        Grid.SetColumn(desktop, 0);
        content.Children.Add(desktop);

        var side = new Border
        {
            Background = ColorBrush("#B8101723"), BorderBrush = ColorBrush("#2B394A"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20), Padding = new Thickness(20), Margin = new Thickness(25, 2, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };
        var sideContent = new StackPanel();
        sideContent.Children.Add(new TextBlock { Text = "Быстрый доступ", FontSize = 17, FontWeight = FontWeights.SemiBold, Foreground = ColorBrush("#F2F5FA"), Margin = new Thickness(0, 0, 0, 12) });
        sideContent.Children.Add(QuickAction("▶", "Google Play", "Открыть магазин приложений", "com.android.vending", "#70F0C0"));
        sideContent.Children.Add(QuickAction("⚙", "Настройки Android", "Параметры виртуального устройства", "com.android.settings", "#80B6FF"));
        sideContent.Children.Add(QuickAction("▤", "Файлы", "Создавать и открывать файлы на ПК", null, "#F5BC68"));
        sideContent.Children.Add(new Border { Height = 1, Background = ColorBrush("#293546"), Margin = new Thickness(0, 8, 0, 15) });
        sideContent.Children.Add(new TextBlock { Text = "УСТАНОВКА APK", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = ColorBrush("#70F0C0") });
        sideContent.Children.Add(new TextBlock { Text = "Выбери APK-файл на компьютере. После установки его значок появится на рабочем столе.", FontSize = 11, Foreground = ColorBrush("#A5B3C5"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 12) });
        sideContent.Children.Add(ActionButton("＋  Установить APK с компьютера", InstallApk_Click, true));
        sideContent.Children.Add(new TextBlock { Text = "ФАЙЛЫ ПК", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = ColorBrush("#70F0C0"), Margin = new Thickness(0, 19, 0, 4) });
        sideContent.Children.Add(new TextBlock { Text = "Файлы DroidForgeDesktop хранятся отдельно от Android и не меняют его прошивку.", FontSize = 11, Foreground = ColorBrush("#A5B3C5"), TextWrapping = TextWrapping.Wrap });
        side.Child = sideContent;
        Grid.SetColumn(side, 1);
        content.Children.Add(side);
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        var taskbar = new Border
        {
            Background = ColorBrush("#E80B111C"), BorderBrush = ColorBrush("#2A3646"), BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(23, 0, 24, 0)
        };
        var bar = new Grid();
        bar.ColumnDefinitions.Add(new ColumnDefinition());
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(TaskButton("◆  Пуск", (_, _) => SetStatus("DroidForgeDesktop готов.")));
        actions.Children.Add(TaskButton("Приложения", (_, _) => SetStatus("Выбери значок приложения на рабочем столе.")));
        actions.Children.Add(TaskButton("Файлы", (_, _) => OpenFiles()));
        actions.Children.Add(TaskButton("＋ Установить APK", InstallApk_Click));
        actions.Children.Add(TaskButton("⚙ Настройки", async (_, _) => await OpenAndroidAppAsync("com.android.settings", "Настройки Android")));
        bar.Children.Add(actions);
        _status.Text = "Android готов · приложения открываются в окнах";
        _status.VerticalAlignment = VerticalAlignment.Center;
        _status.FontSize = 10;
        _status.Foreground = ColorBrush("#95A3B7");
        Grid.SetColumn(_status, 1);
        bar.Children.Add(_status);
        taskbar.Child = bar;
        Grid.SetRow(taskbar, 2);
        root.Children.Add(taskbar);

        Content = root;
    }

    private UIElement QuickAction(string glyph, string name, string description, string? packageName, string color)
    {
        var button = new Button
        {
            Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(8),
            HorizontalContentAlignment = HorizontalAlignment.Stretch, Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 0, 5),
            Content = MakeQuickActionContent(glyph, name, description, color)
        };
        button.Click += async (_, _) =>
        {
            if (packageName is null) OpenFiles();
            else await OpenAndroidAppAsync(packageName, name);
        };
        return button;
    }

    private static UIElement MakeQuickActionContent(string glyph, string name, string description, string color)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(new Border { Width = 38, Height = 38, CornerRadius = new CornerRadius(12), Background = ColorBrush("#263345"), Child = new TextBlock { Text = glyph, FontSize = 19, Foreground = ColorBrush(color), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        text.Children.Add(new TextBlock { Text = name, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = ColorBrush("#EEF2F8") });
        text.Children.Add(new TextBlock { Text = description, FontSize = 9, Foreground = ColorBrush("#8E9CAF"), Margin = new Thickness(0, 2, 0, 0) });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }

    private static Button ActionButton(string title, RoutedEventHandler click, bool primary = false)
    {
        var button = new Button
        {
            Content = title, Padding = new Thickness(13, 10, 13, 10), FontSize = 11, FontWeight = FontWeights.SemiBold,
            Foreground = primary ? ColorBrush("#07140F") : ColorBrush("#E8EDF5"),
            Background = ColorBrush(primary ? "#70F0C0" : "#192332"), BorderBrush = ColorBrush(primary ? "#70F0C0" : "#2A3749"),
            BorderThickness = new Thickness(1), Cursor = Cursors.Hand, HorizontalContentAlignment = HorizontalAlignment.Center
        };
        button.Click += click;
        return button;
    }

    private Button TaskButton(string title, RoutedEventHandler click)
    {
        var button = ActionButton(title, click);
        button.Margin = new Thickness(0, 0, 9, 0);
        return button;
    }

    private Button SmallButton(string text, string tooltip, RoutedEventHandler click)
    {
        var button = ActionButton(text, click);
        button.ToolTip = tooltip;
        button.Padding = new Thickness(12, 4, 12, 4);
        button.FontSize = 17;
        return button;
    }

    private Button AppIcon(string title, string packageName, string glyph, string color, string description)
    {
        var tile = new StackPanel { Width = 88, Height = 91, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        tile.Children.Add(new Border
        {
            Width = 54, Height = 54, CornerRadius = new CornerRadius(18), Background = ColorBrush("#B9202C3D"),
            BorderBrush = ColorBrush("#485767"), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock { Text = glyph, FontSize = glyph.Length > 2 ? 12 : 25, FontWeight = FontWeights.Bold, Foreground = ColorBrush(color), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        });
        tile.Children.Add(new TextBlock { Text = title, FontSize = 10, Foreground = ColorBrush("#EEF2F8"), TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 7, 0, 0) });
        var button = new Button
        {
            Width = 100, Height = 106, Margin = new Thickness(4), Padding = new Thickness(3),
            Background = ColorBrush("#38202B3B"), BorderBrush = ColorBrush("#472E3B4E"), BorderThickness = new Thickness(1),
            ToolTip = description + "\nНажми, чтобы открыть в окне Android.", Cursor = Cursors.Hand, Content = tile
        };
        button.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(packageName)) OpenFiles();
            else await OpenAndroidAppAsync(packageName, title);
        };
        return button;
    }

    private async Task ReloadAppsAsync()
    {
        _icons.Children.Clear();
        var installedPackages = (await _sdk.ListAllPackagesAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _icons.Children.Add(AppIcon("Google Play", "com.android.vending", "▶", "#70F0C0", "Магазин приложений Android"));
        _icons.Children.Add(AppIcon("Настройки", "com.android.settings", "⚙", "#80B6FF", "Настройки Android"));
        var builtInApps = new (string Title, string Package, string Glyph, string Color, string Hint)[]
        {
            ("Камера", "com.android.camera2", "◉", "#FE8B96", "Камера Android"),
            ("Chrome", "com.android.chrome", "C", "#F5BC68", "Браузер Android"),
            ("Контакты", "com.android.contacts", "●", "#80B6FF", "Контакты Android"),
            ("Часы", "com.android.deskclock", "◷", "#D99AFF", "Часы Android"),
            ("Календарь", "com.android.calendar", "▦", "#66D7DC", "Календарь Android"),
            ("Фото", "com.google.android.apps.photos", "✿", "#FE8B96", "Фото Android")
        };
        foreach (var app in builtInApps)
            if (installedPackages.Contains(app.Package))
                _icons.Children.Add(AppIcon(app.Title, app.Package, app.Glyph, app.Color, app.Hint));
        _icons.Children.Add(AppIcon("Файлы ПК", "", "▤", "#F5BC68", "Создавать и открывать файлы на компьютере"));
        var packages = await _sdk.ListPackagesAsync();
        foreach (var package in packages)
        {
            if (package is "com.android.vending" or "com.android.settings" || package.StartsWith("com.droidforge.", StringComparison.OrdinalIgnoreCase)) continue;
            var title = FriendlyName(package);
            var glyph = title.FirstOrDefault().ToString().ToUpperInvariant();
            var index = (int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(package) % (uint)AccentColors.Length);
            _icons.Children.Add(AppIcon(title, package, glyph, AccentColors[index], package));
        }
        SetStatus(packages.Count == 0
            ? "Android готов · установи APK или открой Google Play"
            : $"Android готов · приложений: {packages.Count}");
    }

    private static string FriendlyName(string packageName)
    {
        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["com.android.chrome"] = "Chrome",
            ["com.google.android.youtube"] = "YouTube",
            ["com.whatsapp"] = "WhatsApp",
            ["com.instagram.android"] = "Instagram",
            ["com.facebook.katana"] = "Facebook",
            ["com.roblox.client"] = "Roblox",
            ["com.spotify.music"] = "Spotify",
            ["com.mojang.minecraftpe"] = "Minecraft",
            ["org.telegram.messenger"] = "Telegram",
            ["com.discord"] = "Discord"
        };
        if (known.TryGetValue(packageName, out var name)) return name;
        var part = packageName.Split('.').LastOrDefault() ?? packageName;
        return string.Concat(part.Replace('_', ' ').Replace('-', ' ').Select((character, index) => index == 0 ? char.ToUpperInvariant(character) : character));
    }

    private async Task OpenAndroidAppAsync(string packageName, string title)
    {
        SetStatus($"Открываю {title} в отдельном окне Android…");
        var result = await _sdk.StartAppWindowAsync(packageName, title);
        if (result.ExitCode == 0) SetStatus($"{title} открыт в отдельном окне. Его можно перетаскивать.");
        else SetStatus("Не удалось открыть " + title + ": " + (result.Error + " " + result.Output).Trim());
    }

    private async void InstallApk_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Title = "Выбери APK-файл для установки", Filter = "Android-приложение (*.apk)|*.apk", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        SetStatus("Устанавливаю " + Path.GetFileName(picker.FileName) + " в Android…");
        var result = await _sdk.InstallApkAsync(picker.FileName);
        if (result.ExitCode != 0)
        {
            SetStatus("Не удалось установить APK: " + (result.Error + " " + result.Output).Trim());
            return;
        }
        SetStatus("APK установлен. Обновляю значки рабочего стола…");
        await ReloadAppsAsync();
        SetStatus("Приложение установлено и добавлено на рабочий стол.");
    }

    private void OpenFiles()
    {
        if (_filesWindow is { IsVisible: true }) { _filesWindow.Activate(); return; }
        _filesWindow = new DesktopFilesWindow();
        _filesWindow.Owner = this;
        _filesWindow.Closed += (_, _) => _filesWindow = null;
        _filesWindow.Show();
    }

    private void SetStatus(string message) => _status.Text = message;

    private void HandleShortcut(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        else if (e.Key == Key.F11)
        {
            if (WindowStyle == WindowStyle.None)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = WindowState.Normal;
                Width = 1150;
                Height = 760;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Left = 0;
                Top = 0;
                Width = SystemParameters.PrimaryScreenWidth;
                Height = SystemParameters.PrimaryScreenHeight;
            }
            e.Handled = true;
        }
    }

    private static SolidColorBrush ColorBrush(string value) => new(ColorFrom(value));
    private static Color ColorFrom(string value) => (Color)ColorConverter.ConvertFromString(value)!;
}

internal sealed class DesktopFilesWindow : Window
{
    private readonly string _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DroidForgeDesktop");
    private readonly ListBox _files = new();
    private readonly TextBox _name = new();
    private readonly TextBox _contents = new();

    public DesktopFilesWindow()
    {
        Title = "Файлы · DroidForgeDesktop";
        Width = 760;
        Height = 560;
        MinWidth = 650;
        MinHeight = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#101520")!);
        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F4FA")!);
        FontFamily = new FontFamily("Segoe UI");
        Directory.CreateDirectory(_folder);

        var root = new DockPanel { Margin = new Thickness(20) };
        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
        header.Children.Add(new TextBlock { Text = "Файлы DroidForgeDesktop", FontSize = 19, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) });
        var openFolder = FileButton("Открыть папку", (_, _) => Process.Start(new ProcessStartInfo("explorer.exe", _folder) { UseShellExecute = true }));
        header.Children.Add(openFolder);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var editor = new Grid();
        editor.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        editor.ColumnDefinitions.Add(new ColumnDefinition());
        var left = new DockPanel { Margin = new Thickness(0, 0, 16, 0) };
        var refresh = FileButton("Обновить список", (_, _) => RefreshFiles());
        DockPanel.SetDock(refresh, Dock.Bottom);
        left.Children.Add(refresh);
        _files.SelectionChanged += (_, _) => LoadSelectedFile();
        _files.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1019")!);
        _files.Foreground = Foreground;
        _files.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#293548")!);
        left.Children.Add(_files);
        Grid.SetColumn(left, 0);
        editor.Children.Add(left);

        var form = new DockPanel();
        var save = FileButton("Сохранить файл", (_, _) => SaveFile());
        DockPanel.SetDock(save, Dock.Bottom);
        form.Children.Add(save);
        _name.Text = "Новый файл.txt";
        _name.Height = 36;
        _name.Padding = new Thickness(9, 6, 9, 6);
        _name.Margin = new Thickness(0, 0, 0, 9);
        _name.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1019")!);
        _name.Foreground = Foreground;
        _name.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#293548")!);
        DockPanel.SetDock(_name, Dock.Top);
        form.Children.Add(_name);
        _contents.AcceptsReturn = true;
        _contents.AcceptsTab = true;
        _contents.TextWrapping = TextWrapping.Wrap;
        _contents.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _contents.Padding = new Thickness(10);
        _contents.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1019")!);
        _contents.Foreground = Foreground;
        _contents.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#293548")!);
        form.Children.Add(_contents);
        Grid.SetColumn(form, 1);
        editor.Children.Add(form);
        root.Children.Add(editor);
        Content = root;
        RefreshFiles();
    }

    private void RefreshFiles()
    {
        var selected = _name.Text;
        _files.ItemsSource = Directory.EnumerateFiles(_folder).Select(Path.GetFileName).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (!string.IsNullOrWhiteSpace(selected)) _files.SelectedItem = selected;
    }

    private void LoadSelectedFile()
    {
        if (_files.SelectedItem is not string fileName) return;
        var path = Path.Combine(_folder, fileName);
        if (!File.Exists(path)) return;
        _name.Text = fileName;
        try { _contents.Text = File.ReadAllText(path); }
        catch (Exception error) { MessageBox.Show(this, "Не удалось открыть файл: " + error.Message, "DroidForgeDesktop", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void SaveFile()
    {
        var picker = new SaveFileDialog
        {
            Title = "Создать файл DroidForgeDesktop",
            Filter = "Текстовый файл (*.txt)|*.txt|Все файлы (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(_name.Text) ? "Новый файл.txt" : _name.Text,
            InitialDirectory = _folder,
            OverwritePrompt = true
        };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            File.WriteAllText(picker.FileName, _contents.Text);
            _name.Text = Path.GetFileName(picker.FileName);
            RefreshFiles();
            _files.SelectedItem = _name.Text;
        }
        catch (Exception error) { MessageBox.Show(this, "Не удалось сохранить файл: " + error.Message, "DroidForgeDesktop", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private Button FileButton(string title, RoutedEventHandler click)
    {
        var button = new Button { Content = title, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 8, 0), Foreground = Foreground, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#192332")!), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3749")!), BorderThickness = new Thickness(1), Cursor = Cursors.Hand };
        button.Click += click;
        return button;
    }
}
