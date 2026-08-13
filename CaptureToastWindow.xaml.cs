using System.Windows;
using System.Windows.Media.Animation;

namespace QuickSnip;

public partial class CaptureToastWindow : Window
{
    public CaptureToastWindow(string title, string detail)
    {
        InitializeComponent();
        TitleText.Text = title;
        DetailText.Text = detail;
        Loaded += Window_Loaded;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Right - ActualWidth - 20;
        Top = SystemParameters.WorkArea.Bottom - ActualHeight - 20;
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
        await Task.Delay(1500);
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }
}
