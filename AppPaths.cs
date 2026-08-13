using System.IO;

namespace QuickSnip;

internal static class AppPaths
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuickSnip");

    public static string OnboardingMarker => Path.Combine(
        DataDirectory,
        "onboarding-complete");

    public static string LogDirectory => Path.Combine(
        DataDirectory,
        "Logs");

    public static string SettingsFile => Path.Combine(
        DataDirectory,
        "settings.json");

    public static string LastCaptureMarker => Path.Combine(
        DataDirectory,
        "last-capture");

    public static string LogFile => Path.Combine(
        LogDirectory,
        "quicksnip.log");
}
