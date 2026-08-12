using System.Reflection;
using System.Windows;

namespace RightSnip;

public partial class OptionsWindow : Window
{
    public OptionsWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Build {version?.Major}.{version?.Minor}.{version?.Build}";
        SaveLocationText.Text = SnipFolderService.Path;
    }

    private async void RightSnipButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RightSnipButton.IsEnabled = false;

        try
        {
            Hide();
            await Task.Delay(150);
            await ScreenCaptureService.CaptureCurrentDisplayAsync();
            Close();
        }
        catch (CaptureAlreadyRunningException)
        {
            Close();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Options Right Snip", exception);
            Show();
            StatusText.Text = "Right Snip could not complete. Details were saved to the diagnostic log.";
            RightSnipButton.IsEnabled = true;
        }
    }

    private void OpenFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            SnipFolderService.Open();
            Close();
        }
        catch (Exception exception)
        {
            AppLogger.Error("Open snips folder", exception);
            StatusText.Text = "The snips folder could not be opened.";
        }
    }

    private void InformationButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Hide();

        var guide = new WelcomeWindow(isFirstRun: false)
        {
            Owner = this
        };

        guide.ContinueRequested += (_, _) => guide.Close();
        guide.Closed += (_, _) =>
        {
            Show();
            Activate();
        };
        guide.Show();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e) =>
        Close();
}
