using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace QuickSnip;

public readonly record struct PhysicalCaptureRectangle(
    int Left,
    int Top,
    int Width,
    int Height);

public partial class DragSnipWindow : Window
{
    private const uint NoZOrder = 0x0004;
    private Point _start;
    private bool _selecting;

    public PhysicalCaptureRectangle? Selection { get; private set; }

    public DragSnipWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => CoverVirtualDesktop();
        Loaded += (_, _) =>
        {
            UpdateShade(new Rect(0, 0, 0, 0));
            PositionHint();
            Activate();
            Focus();
        };
    }

    private void CoverVirtualDesktop()
    {
        var handle = new WindowInteropHelper(this).Handle;
        SetWindowPos(
            handle,
            IntPtr.Zero,
            GetSystemMetrics(76),
            GetSystemMetrics(77),
            GetSystemMetrics(78),
            GetSystemMetrics(79),
            NoZOrder);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(OverlayCanvas);
        _selecting = true;
        CaptureMouse();
        Hint.Visibility = Visibility.Collapsed;
        SelectionBorder.Visibility = Visibility.Visible;
        SizeBadge.Visibility = Visibility.Visible;
        UpdateSelection(_start);
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_selecting)
        {
            UpdateSelection(e.GetPosition(OverlayCanvas));
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_selecting)
        {
            return;
        }

        var end = e.GetPosition(OverlayCanvas);
        _selecting = false;
        ReleaseMouseCapture();

        var local = Normalize(_start, end);

        if (local.Width < 3 || local.Height < 3)
        {
            Cancel();
            return;
        }

        var screenStart = PointToScreen(local.TopLeft);
        var screenEnd = PointToScreen(local.BottomRight);
        Selection = new PhysicalCaptureRectangle(
            (int)Math.Round(Math.Min(screenStart.X, screenEnd.X)),
            (int)Math.Round(Math.Min(screenStart.Y, screenEnd.Y)),
            Math.Max(1, (int)Math.Round(Math.Abs(screenEnd.X - screenStart.X))),
            Math.Max(1, (int)Math.Round(Math.Abs(screenEnd.Y - screenStart.Y))));
        DialogResult = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancel();
        }
    }

    private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e) => Cancel();

    private void Cancel()
    {
        Selection = null;
        DialogResult = false;
    }

    private void UpdateSelection(Point current)
    {
        var selection = Normalize(_start, current);
        SetRectangle(SelectionBorder, selection);
        UpdateShade(selection);
        SizeText.Text = $"{Math.Max(0, (int)selection.Width)} × {Math.Max(0, (int)selection.Height)}";
        SizeBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(SizeBadge, Math.Min(selection.Right + 5,
            Math.Max(0, ActualWidth - SizeBadge.DesiredSize.Width - 4)));
        Canvas.SetTop(SizeBadge, Math.Min(selection.Bottom + 5,
            Math.Max(0, ActualHeight - SizeBadge.DesiredSize.Height - 4)));
    }

    private void UpdateShade(Rect selection)
    {
        var width = Math.Max(0, ActualWidth);
        var height = Math.Max(0, ActualHeight);
        var left = Math.Clamp(selection.Left, 0, width);
        var top = Math.Clamp(selection.Top, 0, height);
        var right = Math.Clamp(selection.Right, 0, width);
        var bottom = Math.Clamp(selection.Bottom, 0, height);

        SetRectangle(TopShade, new Rect(0, 0, width, top));
        SetRectangle(BottomShade, new Rect(0, bottom, width, Math.Max(0, height - bottom)));
        SetRectangle(LeftShade, new Rect(0, top, left, Math.Max(0, bottom - top)));
        SetRectangle(RightShade, new Rect(right, top, Math.Max(0, width - right), Math.Max(0, bottom - top)));
    }

    private void PositionHint()
    {
        Hint.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(Hint, Math.Max(12, (ActualWidth - Hint.DesiredSize.Width) / 2));
        Canvas.SetTop(Hint, 24);
    }

    private static Rect Normalize(Point first, Point second) => new(
        new Point(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y)),
        new Point(Math.Max(first.X, second.X), Math.Max(first.Y, second.Y)));

    private static void SetRectangle(FrameworkElement element, Rect bounds)
    {
        Canvas.SetLeft(element, bounds.Left);
        Canvas.SetTop(element, bounds.Top);
        element.Width = Math.Max(0, bounds.Width);
        element.Height = Math.Max(0, bounds.Height);
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
