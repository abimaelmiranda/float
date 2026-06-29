using System.Globalization;
using System.Text.Json;

namespace Float.UI.Localization;

internal static class CultureBootstrap
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".float", "settings.json");

    public static void ApplySavedCulture()
    {
        var language = ReadLanguage();
        if (language is not ("en" or "pt-BR")) language = "en";

        var culture = CultureInfo.GetCultureInfo(language);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static string ReadLanguage()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return "en";
            using var doc = JsonDocument.Parse(File.ReadAllText(SettingsPath));
            return doc.RootElement.TryGetProperty("Language", out var value)
                ? value.GetString() ?? "en"
                : "en";
        }
        catch
        {
            return "en";
        }
    }
}
