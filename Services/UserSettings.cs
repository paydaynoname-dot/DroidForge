using System.Text.Json;
using System.IO;

namespace DroidForge.Services;

public sealed class UserSettings
{
    public string? SdkPath { get; set; }
    public string GpuMode { get; set; } = "auto";

    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DroidForge", "settings.json");

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsFile)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
