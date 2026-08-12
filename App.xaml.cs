using System.Windows;

namespace RightSnip;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await ScreenCaptureService.CaptureCurrentDisplayAsync();
            Shutdown();
        }
        catch
        {
            Shutdown(1);
        }
    }
}
