using System.IO;
using System.Windows.Shell;

namespace RightSnip;

internal static class JumpListService
{
    public static void Register()
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

        jumpList.JumpItems.Add(CreateTask(
            executablePath,
            "Open Snips Folder",
            "Open Pictures\\RightSnip",
            "--open-folder"));

        jumpList.JumpItems.Add(CreateTask(
            executablePath,
            "RightSnip Options",
            "Open the RightSnip command window",
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
