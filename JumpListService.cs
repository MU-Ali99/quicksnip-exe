using System.IO;
using System.Windows.Shell;

namespace QuickSnip;

internal static class JumpListService
{
    public static void Register(QuickSnipSettings settings)
    {
        var executablePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        var jumpList = new JumpList
        {
            ShowFrequentCategory = false,
            ShowRecentCategory = false
        };

        if (!settings.QuickSnipEnabled)
        {
            jumpList.JumpItems.Add(CreateTask(
                executablePath,
                "QuickSnip",
                "Capture the display containing the mouse pointer",
                "--display"));
        }

        if (!settings.ActiveWindowSnipEnabled)
        {
            jumpList.JumpItems.Add(CreateTask(
                executablePath,
                "Window Snip",
                "Capture only the focused application window",
                "--active-window"));
        }

        jumpList.JumpItems.Add(CreateTask(
            executablePath,
            "Open Snips Folder",
            "Open the configured QuickSnip save folder",
            "--open-folder"));

        jumpList.JumpItems.Add(CreateTask(
            executablePath,
            "QuickSnip Settings",
            "Open QuickSnip settings",
            "--menu"));

        JumpList.SetJumpList(System.Windows.Application.Current, jumpList);
        jumpList.Apply();
    }

    private static JumpTask CreateTask(
        string executablePath,
        string title,
        string description,
        string arguments) =>
        new()
        {
            ApplicationPath = executablePath,
            Arguments = arguments,
            Title = title,
            Description = description,
            IconResourcePath = executablePath,
            IconResourceIndex = 0,
            WorkingDirectory = Path.GetDirectoryName(executablePath)
        };
}
