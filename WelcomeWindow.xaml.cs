using System.Windows;

namespace RightSnip;

public partial class WelcomeWindow : Window
{
    private readonly bool _isFirstRun;

    public event EventHandler? ContinueRequested;

    public WelcomeWindow(bool isFirstRun)
    {
        _isFirstRun = isFirstRun;
        InitializeComponent();

        if (!isFirstRun)
        {
            WelcomeSubtitle.Text = "How RightSnip works";
            UnderstandCheckBox.IsChecked = true;
            UnderstandCheckBox.Content = "I understand how RightSnip works";
            ContinueButton.Content = "Back to RightSnip Options";
        }
    }

    private void UnderstandCheckBox_Changed(
        object sender,
        RoutedEventArgs e) =>
        ContinueButton.IsEnabled = UnderstandCheckBox.IsChecked == true;

    private void ContinueButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (UnderstandCheckBox.IsChecked != true)
        {
            return;
        }

        if (_isFirstRun)
        {
            OnboardingService.Complete();
        }

        ContinueRequested?.Invoke(this, EventArgs.Empty);
    }
}
