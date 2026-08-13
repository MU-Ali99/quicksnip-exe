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

            if (HasArgument(e.Args, "--hotkey-host"))
            {
                HotkeyService.RunHost(settings);
                Shutdown();
                return;
            }

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

            if (HasArgument(e.Args, "--drag"))
            {
                await ScreenCaptureService.CaptureAsync(
                    CaptureTarget.Drag,
                    settings);
                Shutdown();
                return;
            }

            if (HasArgument(e.Args, "--lock-snip"))
            {
                if (settings.LockSnipEnabled)
                {
                    HotkeyService.StopHost();
                    var controller = new LockSnipWindow(settings);
                    MainWindow = controller;
                    controller.Closed += (_, _) =>
                    {
                        HotkeyService.StartHostIfNeeded(SettingsService.Load());
                        Shutdown();
                    };
                    controller.Show();
                }
                else
                {
                    Shutdown();
                }
                return;
            }

            if (HasArgument(e.Args, "--menu"))
            {
                HotkeyService.StopHost();
                ShowOptionsWindow();
                return;
            }

            if (!OnboardingService.IsComplete)
            {
                ShowWelcomeWindow(isFirstRun: true);
                return;
            }

            var defaultTarget = settings.DragSnipEnabled
                ? CaptureTarget.Drag
                : settings.ActiveWindowSnipEnabled
                    ? CaptureTarget.ActiveWindow
                    : CaptureTarget.Display;

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
            var settings = SettingsService.Load();
            JumpListService.Register(settings);
            HotkeyService.StartHostIfNeeded(settings);
            Shutdown();
        };
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
