using System.Text.Json;
using Float.Core.Abstractions.Services;
using Float.Core.Models;
using Float.Infrastructure.Json.Context;

namespace Float.Infrastructure.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".float", "settings.json");

    private AppSettings _cached = Load();

    public AppSettings Get() => _cached;

    public void Save(AppSettings settings)
    {
        _cached = settings;
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
        File.WriteAllText(SettingsPath, json);
    }

    private static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings)
                ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
