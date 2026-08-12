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
                ShowOptionsWindow();
                return;
            }

            if (!OnboardingService.IsComplete)
            {
                ShowWelcomeWindow(isFirstRun: true);
                return;
            }

            await ScreenCaptureService.CaptureCurrentDisplayAsync();
            Shutdown();
        }
        catch (CaptureAlreadyRunningException)
        {
            Shutdown();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Application startup", exception);
            Shutdown(1);
        }
    }

    public void ShowOptionsWindow()
    {
        var optionsWindow = new OptionsWindow();
        MainWindow = optionsWindow;
        optionsWindow.Closed += (_, _) => Shutdown();
        optionsWindow.Show();
    }

    public void ShowWelcomeWindow(bool isFirstRun)
    {
        var welcomeWindow = new WelcomeWindow(isFirstRun);
        MainWindow = welcomeWindow;

        welcomeWindow.ContinueRequested += (_, _) =>
        {
            welcomeWindow.Closed -= WelcomeWindowClosed;
            welcomeWindow.Close();
            ShowOptionsWindow();
        };

        welcomeWindow.Closed += WelcomeWindowClosed;
        welcomeWindow.Show();
    }

    private void WelcomeWindowClosed(object? sender, EventArgs e) =>
        Shutdown();

    private static bool HasArgument(string[] arguments, string expected) =>
        arguments.Contains(expected, StringComparer.OrdinalIgnoreCase);
}
