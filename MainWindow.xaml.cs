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

    private void InstallContextMenuButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ContextMenuService.Install(Environment.ProcessPath!);
            StatusText.Text =
                "Right Snip was added to the desktop and folder-background right-click menus. On Windows 11, choose Show more options.";
        }
        catch (Exception exception)
        {
            StatusText.Text =
                $"Could not add the right-click entry: {exception.Message}";
        }
    }

    private void RemoveContextMenuButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ContextMenuService.Uninstall();
            StatusText.Text =
                "The Right Snip right-click entry was removed.";
        }
        catch (Exception exception)
        {
            StatusText.Text =
                $"Could not remove the right-click entry: {exception.Message}";
        }
    }
}
