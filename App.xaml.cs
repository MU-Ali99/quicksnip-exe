using System.Windows;

namespace RightSnip;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            JumpListService.Register();

            if (HasArgument(e.Args, "--register-jump-list"))
            {
                Shutdown();
                return;
            }

            if (HasArgument(e.Args, "--open-folder"))
            {
                SnipFolderService.Open();
                Shutdown();
                return;
            }

            if (HasArgument(e.Args, "--menu"))
            {
                var optionsWindow = new OptionsWindow();
                MainWindow = optionsWindow;
                optionsWindow.Closed += (_, _) => Shutdown();
                optionsWindow.Show();
                return;
            }

            await ScreenCaptureService.CaptureCurrentDisplayAsync();
            Shutdown();
        }
        catch
        {
            Shutdown(1);
        }
    }

    private static bool HasArgument(string[] arguments, string expected) =>
        arguments.Contains(expected, StringComparer.OrdinalIgnoreCase);
}
