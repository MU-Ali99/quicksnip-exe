using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;

namespace QuickSnip;

public partial class LockTargetPickerWindow : Window
{
    private const uint NoZOrder = 0x0004;
    public Point? SelectedScreenPoint { get; private set; }

    public LockTargetPickerWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => CoverVirtualDesktop();
        Loaded += (_, _) =>
        {
            PositionHintOnPrimaryDisplay();
            Activate();
            Focus();
        };
        ContentRendered += (_, _) => PositionHintOnPrimaryDisplay();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(PositionHintOnPrimaryDisplay);
    }

    private void PositionHintOnPrimaryDisplay()
    {
        Hint.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        const int primaryScreenWidth = 0;
        const int topGapInPhysicalPixels = 24;
        var anchor = PointFromScreen(new Point(
            GetSystemMetrics(primaryScreenWidth) / 2.0,
            topGapInPhysicalPixels));
        Canvas.SetLeft(Hint, anchor.X - Hint.DesiredSize.Width / 2);
        Canvas.SetTop(Hint, anchor.Y);
    }

    private void CoverVirtualDesktop()
    {
        SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero,
            GetSystemMetrics(76), GetSystemMetrics(77),
            GetSystemMetrics(78), GetSystemMetrics(79), NoZOrder);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SelectedScreenPoint = PointToScreen(e.GetPosition(this));
        DialogResult = true;
    }

    private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e) => DialogResult = false;
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);
}
