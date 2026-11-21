using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DroidForge.Services;

public sealed record CommandResult(int ExitCode, string Output, string Error);
public sealed record DeviceInfo(string Name, string? ConfigPath, string Resolution, int RamMb, int CpuCores);
public sealed record SystemImageInfo(string PackageId, int ApiLevel, string Label, bool IncludesPlayStore)
{
    public override string ToString() => Label;
}

/// <summary>Small, dependency-free bridge to the Android SDK installed by the user.</summary>
public sealed class AndroidSdk
{
    public string? Root { get; private set; }
    public string? EmulatorExe => Executable("emulator", "emulator.exe");
    public string? AdbExe => Executable("platform-tools", "adb.exe");
    public string? AvdManagerBat => FindAvdManager();
    public bool IsAvailable => EmulatorExe is not null && AdbExe is not null;

    public AndroidSdk(string? preferredRoot = null) => SetRoot(preferredRoot);

    public void SetRoot(string? preferredRoot)
    {
        var candidates = new[]
        {
            preferredRoot,
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
            @"C:\Android\Sdk"
        };
        Root = candidates.Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Path.GetFullPath(p!)).FirstOrDefault(p =>
                File.Exists(Path.Combine(p, "emulator", "emulator.exe")) &&
                File.Exists(Path.Combine(p, "platform-tools", "adb.exe")));
    }

    private string? Executable(string folder, string file) => Root is null ? null : Path.Combine(Root, folder, file);

    private string? FindAvdManager()
    {
        if (Root is null) return null;
        var tools = Path.Combine(Root, "cmdline-tools");
        if (!Directory.Exists(tools)) return null;
        return Directory.GetDirectories(tools).OrderByDescending(p => Path.GetFileName(p) == "latest")
            .ThenByDescending(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
            .Select(p => Path.Combine(p, "bin", "avdmanager.bat"))
            .FirstOrDefault(File.Exists);
    }

    public async Task<IReadOnlyList<DeviceInfo>> ListAvdsAsync()
    {
        if (EmulatorExe is null) return [];
        var result = await RunAsync(EmulatorExe, ["-list-avds"], timeoutSeconds: 15);
        var names = result.Output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith("INFO", StringComparison.OrdinalIgnoreCase));
        return names.Select(ReadDevice).ToArray();
    }

    public DeviceInfo ReadDevice(string name)
    {
        var avdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");
        if (string.IsNullOrWhiteSpace(avdHome)) avdHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "avd");
        var path = Path.Combine(avdHome, name + ".avd", "config.ini");
        if (!File.Exists(path)) return new DeviceInfo(name, null, "—", 2048, 2);
        var map = ReadIni(path);
        var width = GetInt(map, "hw.lcd.width", 1080);
        var height = GetInt(map, "hw.lcd.height", 1920);
        return new DeviceInfo(name, path, $"{width} × {height}", GetInt(map, "hw.ramSize", 2048), GetInt(map, "hw.cpu.ncore", 2));
    }

    public IReadOnlyList<SystemImageInfo> ListSystemImages()
    {
        if (Root is null) return [];
        var imageRoot = Path.Combine(Root, "system-images");
        if (!Directory.Exists(imageRoot)) return [];

        var images = new List<SystemImageInfo>();
        try
        {
            foreach (var manifest in Directory.EnumerateFiles(imageRoot, "package.xml", SearchOption.AllDirectories))
            {
                var folder = Path.GetDirectoryName(manifest)!;
                var relative = Path.GetRelativePath(Root, folder);
                var parts = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4 || !parts[0].Equals("system-images", StringComparison.OrdinalIgnoreCase)) continue;
                var apiText = parts[1].StartsWith("android-", StringComparison.OrdinalIgnoreCase) ? parts[1][8..] : "";
                if (!int.TryParse(apiText, out var api)) continue;

                var flavor = parts[2];
                var architecture = parts[3];
                var hasPlayStore = flavor.Contains("playstore", StringComparison.OrdinalIgnoreCase);
                var version = api switch { 35 => "15", 36 => "16", 37 => "17", _ => $"API {api}" };
                var flavorLabel = hasPlayStore ? "Google Play Store" : flavor.Contains("google_apis", StringComparison.OrdinalIgnoreCase) ? "Google APIs" : "Android";
                var packageId = relative.Replace(Path.DirectorySeparatorChar, ';').Replace(Path.AltDirectorySeparatorChar, ';');
                images.Add(new SystemImageInfo(packageId, api, $"Android {version} · {flavorLabel} · {architecture}", hasPlayStore));
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return images.DistinctBy(image => image.PackageId, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(image => image.ApiLevel).ThenByDescending(image => image.IncludesPlayStore).ToArray();
    }

    public async Task<CommandResult> CreateAvdAsync(string name, string packageId)
    {
        if (AvdManagerBat is null)
            return new CommandResult(-1, "", "Не найдены Android SDK Command-line Tools. В Android Studio откройте Tools → SDK Manager → SDK Tools и установите Android SDK Command-line Tools (latest).");
        var psi = NewStartInfo(AvdManagerBat, ["create", "avd", "-n", name, "-k", packageId]);
        psi.RedirectStandardInput = true;
        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.StandardInput.WriteLineAsync("no"); // Use the image's standard phone profile.
            process.StandardInput.Close();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(3));
            return new(process.ExitCode, await outputTask, await errorTask);
        }
        catch (Exception e) { return new(-1, "", e.Message); }
    }

    public async Task<CommandResult> ApplyDeviceConfigAsync(DeviceInfo device, int ramMb, int cores, int width, int height, int density)
    {
        if (device.ConfigPath is null) return new(-1, "", "Не найден config.ini этого устройства.");
        var map = ReadIni(device.ConfigPath);
        map["hw.ramSize"] = ramMb.ToString();
        map["hw.cpu.ncore"] = cores.ToString();
        map["hw.lcd.width"] = width.ToString();
        map["hw.lcd.height"] = height.ToString();
        map["hw.lcd.density"] = density.ToString();
        map["hw.keyboard"] = "yes";
        var temporary = device.ConfigPath + ".droidforge.tmp";
        try
        {
            await File.WriteAllLinesAsync(temporary, map.Select(kv => $"{kv.Key}={kv.Value}"), new UTF8Encoding(false));
            File.Move(temporary, device.ConfigPath, true);
            return new(0, "Настройки сохранены.", "");
        }
        catch (Exception e)
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
            return new(-1, "", e.Message);
        }
    }

    public async Task<CommandResult> StartAsync(string avdName, string gpu)
    {
        if (EmulatorExe is null) return new(-1, "", "Не найден Android Emulator.");
        try
        {
            var psi = NewStartInfo(EmulatorExe, ["-avd", avdName, "-gpu", gpu, "-no-boot-anim"]);
            psi.UseShellExecute = false;
            Process.Start(psi);
            await Task.Delay(350);
            return new(0, "Устройство запускается в отдельном окне.", "");
        }
        catch (Exception e) { return new(-1, "", e.Message); }
    }

    public async Task<CommandResult> AdbAsync(params string[] args)
    {
        if (AdbExe is null) return new(-1, "", "Не найден Android Platform Tools (adb).");
        return await RunAsync(AdbExe, args, 45);
    }

    public async Task<IReadOnlyList<string>> ListPackagesAsync()
    {
        var result = await AdbAsync("shell", "pm", "list", "packages", "-3");
        if (result.ExitCode != 0) return [];
        return result.Output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().StartsWith("package:") ? x.Trim()[8..] : x.Trim())
            .Where(x => x.Length > 0).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<CommandResult> OpenPackageAsync(string packageName) =>
        await AdbAsync("shell", "monkey", "-p", packageName, "-c", "android.intent.category.LAUNCHER", "1");

    private static Dictionary<string, string> ReadIni(string path)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            var split = line.IndexOf('=');
            if (split > 0) map[line[..split].Trim()] = line[(split + 1)..].Trim();
        }
        return map;
    }

    private static int GetInt(Dictionary<string, string> values, string key, int fallback) =>
        values.TryGetValue(key, out var text) && int.TryParse(text, out var n) ? n : fallback;

    private static ProcessStartInfo NewStartInfo(string file, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo(file) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        return psi;
    }

    private static async Task<CommandResult> RunAsync(string file, IEnumerable<string> args, int timeoutSeconds)
    {
        try
        {
            using var process = new Process { StartInfo = NewStartInfo(file, args) };
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
            return new(process.ExitCode, await stdout, await stderr);
        }
        catch (Exception e) { return new(-1, "", e.Message); }
    }
}
