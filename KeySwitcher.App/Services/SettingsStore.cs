using System.IO;
using System.Text.Json;
using KeySwitcher.Models;

namespace KeySwitcher.Services;

public sealed class SettingsStore
{
    public string DirectoryPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "by koda", "KeySwitcher");
    public string SettingsPath => Path.Combine(DirectoryPath, "settings.json");
    public string ModelsPath => Path.Combine(DirectoryPath, "Models");

    public AppSettings Load()
    {
        Directory.CreateDirectory(DirectoryPath);
        Directory.CreateDirectory(ModelsPath);
        if (!File.Exists(SettingsPath)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions()) ?? new AppSettings(); }
        catch { return new AppSettings(); }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions()));
    }

    private static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
}
