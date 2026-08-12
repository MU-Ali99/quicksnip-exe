using Microsoft.Win32;

namespace RightSnip;

internal static class ContextMenuService
{
    private static readonly string[] MenuKeyPaths =
    [
        @"Software\Classes\DesktopBackground\Shell\RightSnip",
        @"Software\Classes\Directory\Background\shell\RightSnip"
    ];

    public static void Install(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException(
                "RightSnip could not determine its executable path.");
        }

        foreach (var menuKeyPath in MenuKeyPaths)
        {
            using var menuKey =
                Registry.CurrentUser.CreateSubKey(menuKeyPath);

            menuKey.SetValue(string.Empty, "Right Snip");
            menuKey.SetValue("Icon", executablePath);

            using var commandKey =
                menuKey.CreateSubKey("command");

            commandKey.SetValue(
                string.Empty,
                $"\"{executablePath}\" --right-snip");
        }
    }

    public static void Uninstall()
    {
        foreach (var menuKeyPath in MenuKeyPaths)
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                menuKeyPath,
                throwOnMissingSubKey: false);
        }
    }
}
