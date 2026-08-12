using System.Diagnostics;
using System.IO;

namespace RightSnip;

internal static class SnipFolderService
{
    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "RightSnip");

    public static void EnsureExists() =>
        Directory.CreateDirectory(Path);

    public static void Open()
    {
        EnsureExists();

        Process.Start(new ProcessStartInfo
        {
            FileName = Path,
            UseShellExecute = true
        });
    }
}
