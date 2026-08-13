using System.IO;

namespace QuickSnip;

internal static class AppLogger
{
    public static void Information(string operation, string message) =>
        Write(operation, message);

    public static void Error(string operation, Exception exception)
        => Write(operation, exception.ToString());

    private static void Write(string operation, string message)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);

            var entry =
                $"[{DateTimeOffset.Now:O}] {operation}{Environment.NewLine}" +
                $"{message}{Environment.NewLine}{Environment.NewLine}";

            File.AppendAllText(AppPaths.LogFile, entry);
        }
        catch
        {
            // Logging must never interrupt capture or application shutdown.
        }
    }
}
