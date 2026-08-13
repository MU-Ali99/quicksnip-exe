using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace QuickSnip;

internal static class SnipCleanupService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };

    public static string[] FindSnips(string folder)
    {
        if (!Directory.Exists(folder)) return [];

        return Directory.EnumerateFiles(folder, "*", System.IO.SearchOption.AllDirectories)
            .Where(path =>
                SupportedExtensions.Contains(Path.GetExtension(path)) &&
                Path.GetFileName(path).StartsWith("quicksnip-", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static int MoveToRecycleBin(IEnumerable<string> paths)
    {
        var count = 0;
        foreach (var path in paths)
        {
            FileSystem.DeleteFile(
                path,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.ThrowException);
            count++;
        }

        return count;
    }
}
