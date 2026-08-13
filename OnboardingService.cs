using System.IO;

namespace QuickSnip;

internal static class OnboardingService
{
    public static bool IsComplete =>
        File.Exists(AppPaths.OnboardingMarker);

    public static void Complete()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllText(
            AppPaths.OnboardingMarker,
            DateTimeOffset.UtcNow.ToString("O"));
    }
}
