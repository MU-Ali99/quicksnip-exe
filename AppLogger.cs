using System.IO;

namespace RightSnip;

internal static class AppLogger
{
    public static void Error(string operation, Exception exception)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);

            var entry =
                $"[{DateTimeOffset.Now:O}] {operation}{Environment.NewLine}" +
                $"{exception}{Environment.NewLine}{Environment.NewLine}";

            File.AppendAllText(AppPaths.LogFile, entry);
        }
        catch
        {
            // Logging must never interrupt capture or application shutdown.
        }
    }
}
