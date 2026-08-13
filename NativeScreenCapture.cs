using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace QuickSnip;

internal static class NativeScreenCapture
{
    private const uint MonitorDefaultToNearest = 2;
    private const int SourceCopy = 0x00CC0020;
    private const int CaptureLayeredWindows = 0x40000000;
    private const int ExtendedFrameBounds = 9;
    private const int DwmCloaked = 14;

    public static Bitmap CaptureDisplayContainingPointer()
    {
        if (!GetCursorPos(out var pointer))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var monitor = MonitorFromPoint(pointer, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return CaptureRectangle(monitorInfo.Monitor);
    }

    public static Bitmap CaptureActiveWindow()
    {
        var window = GetForegroundWindow();

        if (!IsCapturableWindow(window))
        {
            window = FindTopCapturableWindow();
        }

        if (window == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "QuickSnip could not find an active application window.");
        }

        if (!TryGetWindowBounds(window, out var bounds))
        {
            throw new InvalidOperationException(
                "QuickSnip could not determine the active window bounds.");
        }

        LogSelectedWindow(window, bounds);

        if (IsZoomed(window))
        {
            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            var monitorInfo = new MonitorInfo
            {
                Size = Marshal.SizeOf<MonitorInfo>()
            };

            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                bounds = Intersect(bounds, monitorInfo.WorkArea);
            }
        }

        return CaptureRectangle(bounds);
    }

    private static Bitmap CaptureRectangle(NativeRectangle bounds)
    {
        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("The capture area is empty.");
        }

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            var destinationDc = graphics.GetHdc();
            var screenDc = GetDC(IntPtr.Zero);

            try
            {
                if (screenDc == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!BitBlt(
                    destinationDc,
                    0,
                    0,
                    width,
                    height,
                    screenDc,
                    bounds.Left,
                    bounds.Top,
                    SourceCopy | CaptureLayeredWindows))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                if (screenDc != IntPtr.Zero)
                {
                    ReleaseDC(IntPtr.Zero, screenDc);
                }

                graphics.ReleaseHdc(destinationDc);
            }

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static NativeRectangle Intersect(
        NativeRectangle first,
        NativeRectangle second) =>
        new()
        {
            Left = Math.Max(first.Left, second.Left),
            Top = Math.Max(first.Top, second.Top),
            Right = Math.Min(first.Right, second.Right),
            Bottom = Math.Min(first.Bottom, second.Bottom)
        };

    private static IntPtr FindTopCapturableWindow()
    {
        var result = IntPtr.Zero;

        EnumWindows((window, _) =>
        {
            if (!IsCapturableWindow(window))
            {
                return true;
            }

            result = window;
            return false;
        }, IntPtr.Zero);

        return result;
    }

    private static bool IsCapturableWindow(IntPtr window)
    {
        if (window == IntPtr.Zero || !IsWindowVisible(window) || IsIconic(window))
        {
            return false;
        }

        GetWindowThreadProcessId(window, out var processId);

        if (processId == (uint)Environment.ProcessId)
        {
            return false;
        }

        var className = new StringBuilder(128);
        GetClassName(window, className, className.Capacity);

        if (className.ToString() is
            ("Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or
             "Progman" or "WorkerW" or "Windows.UI.Core.CoreWindow"))
        {
            return false;
        }

        var title = new StringBuilder(512);
        GetWindowText(window, title, title.Capacity);

        if (string.IsNullOrWhiteSpace(title.ToString()))
        {
            return false;
        }

        if (DwmGetWindowAttribute(
            window,
            DwmCloaked,
            out int cloaked,
            sizeof(int)) == 0 && cloaked != 0)
        {
            return false;
        }

        if (!TryGetWindowBounds(window, out var bounds))
        {
            return false;
        }

        return bounds.Right - bounds.Left >= 100 &&
               bounds.Bottom - bounds.Top >= 100;
    }

    private static bool TryGetWindowBounds(
        IntPtr window,
        out NativeRectangle bounds)
    {
        var result = DwmGetWindowAttribute(
            window,
            ExtendedFrameBounds,
            out bounds,
            Marshal.SizeOf<NativeRectangle>());

        if (result != 0 && !GetWindowRect(window, out bounds))
        {
            return false;
        }

        return bounds.Right > bounds.Left && bounds.Bottom > bounds.Top;
    }

    private static void LogSelectedWindow(
        IntPtr window,
        NativeRectangle bounds)
    {
        try
        {
            GetWindowThreadProcessId(window, out var processId);
            var title = new StringBuilder(512);
            var className = new StringBuilder(128);
            GetWindowText(window, title, title.Capacity);
            GetClassName(window, className, className.Capacity);

            AppLogger.Information(
                "Active window",
                $"Title={title}; Class={className}; PID={processId}; " +
                $"Bounds={bounds.Left},{bounds.Top}," +
                $"{bounds.Right - bounds.Left}x{bounds.Bottom - bounds.Top}");
        }
        catch
        {
            // Diagnostics must not interrupt capture.
        }
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRectangle Monitor;
        public NativeRectangle WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetMonitorInfo(
        IntPtr monitor,
        ref MonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(
        IntPtr window,
        out NativeRectangle rectangle);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr window,
        int attribute,
        out NativeRectangle value,
        int valueSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr window,
        int attribute,
        out int value,
        int valueSize);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(
        EnumWindowsCallback callback,
        IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(
        IntPtr window,
        uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr window,
        out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(
        IntPtr window,
        StringBuilder className,
        int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(
        IntPtr window,
        StringBuilder text,
        int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(
        IntPtr destination,
        int destinationX,
        int destinationY,
        int width,
        int height,
        IntPtr source,
        int sourceX,
        int sourceY,
        int rasterOperation);
}
