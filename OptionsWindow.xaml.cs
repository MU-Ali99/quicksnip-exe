using System.Windows;

namespace RightSnip;

public partial class OptionsWindow : Window
{
    public OptionsWindow()
    {
        InitializeComponent();
    }

    private async void RightSnipButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Hide();
            await Task.Delay(150);
            await ScreenCaptureService.CaptureCurrentDisplayAsync();
            Close();
        }
        catch
        {
            Show();
            StatusText.Text = "Right Snip could not complete.";
        }
    }

    private void OpenFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SnipFolderService.Open();
        Close();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e) =>
        Close();
}
