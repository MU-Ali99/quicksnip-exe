using System.Windows;

namespace RightSnip;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void RightSnipButton_Click(object sender, RoutedEventArgs e)
    {
        RightSnipButton.IsEnabled = false;
        StatusText.Text = "Capturing...";

        try
        {
            Hide();

            // Let Windows repaint the area previously occupied by this window.
            await Task.Delay(180);

            var savedPath =
                await ScreenCaptureService.CaptureCurrentDisplayAsync();

            Show();
            Activate();
            StatusText.Text =
                $"Saved and copied to clipboard.\n{savedPath}";
        }
        catch (Exception exception)
        {
            Show();
            Activate();
            StatusText.Text = $"Capture failed: {exception.Message}";
        }
        finally
        {
            RightSnipButton.IsEnabled = true;
        }
    }
}
