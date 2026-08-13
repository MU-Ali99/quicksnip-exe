using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace QuickSnip;

public partial class LockTargetPickerWindow : Window
{
    private const uint NoZOrder = 0x0004;
    public Point? SelectedScreenPoint { get; private set; }

    public LockTargetPickerWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => CoverVirtualDesktop();
        Loaded += (_, _) => { Activate(); Focus(); };
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
