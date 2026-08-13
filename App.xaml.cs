using System.Windows;

namespace QuickSnip;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var settings = SettingsService.Load();
            JumpListService.Register(settings);

            if (HasArgument(e.Args, "--register-jump-list"))
            {
                Shutdown();
                return;
            }

            if (HasArgument(e.Args, "--open-folder"))
            {
                SnipFolderService.Open(settings.SaveFolder);
                Shutdown();
                return;
            }

            if (HasArgument(e.Args, "--active-window"))
            {
                await ScreenCaptureService.CaptureAsync(
                    CaptureTarget.ActiveWindow,
                    settings);
                Shutdown();
                return;
            }

            if (HasArgument(e.Args, "--display"))
            {
                await ScreenCaptureService.CaptureAsync(
                    CaptureTarget.Display,
                    settings);
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

            var defaultTarget = settings.QuickSnipEnabled
                ? CaptureTarget.Display
                : CaptureTarget.ActiveWindow;

            await ScreenCaptureService.CaptureAsync(defaultTarget, settings);
            Shutdown();
        }
        catch (Exception exception) when (
            exception is CaptureAlreadyRunningException or CaptureCooldownException)
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
        optionsWindow.Closed += (_, _) =>
        {
            JumpListService.Register(SettingsService.Load());
            Shutdown();
        };
        optionsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        optionsWindow.Show();
        CenterOnPrimaryWorkArea(optionsWindow);
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
        CenterOnPrimaryWorkArea(welcomeWindow);
    }

    private void WelcomeWindowClosed(object? sender, EventArgs e) =>
        Shutdown();

    private static bool HasArgument(string[] arguments, string expected) =>
        arguments.Contains(expected, StringComparer.OrdinalIgnoreCase);

    internal static void CenterOnPrimaryWorkArea(Window window)
    {
        window.Left = SystemParameters.WorkArea.Left +
            (SystemParameters.WorkArea.Width - window.ActualWidth) / 2;
        window.Top = SystemParameters.WorkArea.Top +
            (SystemParameters.WorkArea.Height - window.ActualHeight) / 2;
    }
}
