using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickSnip;

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
            WelcomeSubtitle.Text = "How QuickSnip works";
            UnderstandCheckBox.IsChecked = true;
            ContinueButton.Content = "Back to QuickSnip Settings";
        }
    }

    private void UnderstandCheckBox_Changed(object sender, RoutedEventArgs e) =>
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

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && e.OriginalSource is not Button)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
