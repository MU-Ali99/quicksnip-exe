using System.Diagnostics;
using System.IO;

namespace QuickSnip;

internal static class SnipFolderService
{
    public static string DefaultPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "QuickSnip");

    public static void EnsureExists(string path) =>
        Directory.CreateDirectory(path);

    public static void Open(string path)
    {
        EnsureExists(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
