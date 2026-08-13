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

        if (settings.ShowSnipModesInTaskbar && !settings.QuickSnipEnabled)
        {
            jumpList.JumpItems.Add(CreateTask(
                executablePath,
                "Screen Snip",
                "Capture the display containing the mouse pointer",
                "--display"));
        }

        if (settings.ShowSnipModesInTaskbar && !settings.ActiveWindowSnipEnabled)
        {
            jumpList.JumpItems.Add(CreateTask(
                executablePath,
                "Window Snip",
                "Capture only the focused application window",
                "--active-window"));
        }

        if (settings.ShowSnipModesInTaskbar && !settings.DragSnipEnabled)
        {
            jumpList.JumpItems.Add(CreateTask(
                executablePath,
                "Drag Snip",
                "Drag to capture a selected area",
                "--drag"));
        }

        if (settings.LockSnipEnabled)
        {
            jumpList.JumpItems.Add(CreateTask(
                executablePath,
                "Lock Snip",
                "Build a scrolling screenshot from a locked display or window",
                "--lock-snip"));
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
