using System.Globalization;
using System.IO;

namespace QuickSnip;

internal static class CaptureCooldownService
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(1);

    public static bool IsReady()
    {
        try
        {
            if (!File.Exists(AppPaths.LastCaptureMarker))
            {
                return true;
            }

            var text = File.ReadAllText(AppPaths.LastCaptureMarker);

            if (!DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var completedAt))
            {
                return true;
            }

            return DateTimeOffset.UtcNow - completedAt >= Cooldown;
        }
        catch
        {
            return true;
        }
    }

    public static void MarkCompleted()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllText(
            AppPaths.LastCaptureMarker,
            DateTimeOffset.UtcNow.ToString("O"));
    }
}
