using System.Configuration;
using System.Data;
using System.Windows;

namespace RightSnip;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--right-snip", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await Task.Delay(150);
                await ScreenCaptureService.CaptureCurrentDisplayAsync();
                Shutdown();
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    exception.Message,
                    "RightSnip",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }

            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Closed += (_, _) => Shutdown();
        mainWindow.Show();
    }
}

