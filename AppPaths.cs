using System.IO;

namespace RightSnip;

internal static class AppPaths
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightSnip");

    public static string OnboardingMarker => Path.Combine(
        DataDirectory,
        "onboarding-complete");

    public static string LogDirectory => Path.Combine(
        DataDirectory,
        "Logs");

    public static string LogFile => Path.Combine(
        LogDirectory,
        "rightsnip.log");
}
