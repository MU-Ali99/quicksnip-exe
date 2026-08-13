using System.IO;
using System.Text.Json;

namespace QuickSnip;

internal static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static QuickSnipSettings Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile))
            {
                return new QuickSnipSettings();
            }

            var json = File.ReadAllText(AppPaths.SettingsFile);
            var settings =
                JsonSerializer.Deserialize<QuickSnipSettings>(json) ??
                new QuickSnipSettings();

            settings.Normalize();
            return settings;
        }
        catch (Exception exception)
        {
            AppLogger.Error("Load settings", exception);
            return new QuickSnipSettings();
        }
    }

    public static void Save(QuickSnipSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllText(
            AppPaths.SettingsFile,
            JsonSerializer.Serialize(settings, JsonOptions));
    }
}
