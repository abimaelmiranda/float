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

    private AppSettings _cached;

    public SettingsService()
    {
        (_cached, LoadWarning) = Load();
    }

    public string? LoadWarning { get; private set; }

    public AppSettings Get() => _cached;

    public void Save(AppSettings settings)
    {
        _cached = settings;
        LoadWarning = null;
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
        File.WriteAllText(SettingsPath, json);
    }

    private static (AppSettings Settings, string? Warning) Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return (new AppSettings(), null);
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings);
            return settings is null
                ? (new AppSettings(), "Settings file was empty.")
                : (settings, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return (new AppSettings(), ex.Message);
        }
    }
}
