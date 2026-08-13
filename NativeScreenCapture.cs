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
    private const uint GetRoot = 2;
    private const int MouseWheel = 0x020A;

    public static LockedCaptureTarget GetLockedTarget(int x, int y, bool captureWindow)
    {
        var point = new NativePoint { X = x, Y = y };
        var scrollWindow = WindowFromPoint(point);
        var window = GetAncestor(scrollWindow, GetRoot);
        if (!IsCapturableWindow(window))
        {
            window = IntPtr.Zero;
        }

        NativeRectangle bounds;
        string name;
        if (captureWindow && window != IntPtr.Zero && TryGetWindowBounds(window, out bounds))
        {
            var title = new StringBuilder(512);
            GetWindowText(window, title, title.Capacity);
            name = title.ToString();
        }
        else
        {
            var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            bounds = info.Monitor;
            name = $"Display {DisplayNumber(monitor)}";
        }

        return new LockedCaptureTarget(
            window,
            scrollWindow,
            new PhysicalCaptureRectangle(bounds.Left, bounds.Top,
                bounds.Right - bounds.Left, bounds.Bottom - bounds.Top),
            name,
            captureWindow && window != IntPtr.Zero);
    }

    public static Bitmap CaptureLockedTarget(LockedCaptureTarget target)
    {
        var bounds = target.Bounds;
        if (target.IsWindow && target.Window != IntPtr.Zero &&
            TryGetWindowBounds(target.Window, out var current))
        {
            bounds = new PhysicalCaptureRectangle(current.Left, current.Top,
                current.Right - current.Left, current.Bottom - current.Top);
        }
        return CaptureRegion(bounds);
    }

    public static void ScrollLockedTarget(LockedCaptureTarget target, bool next)
    {
        if (target.ScrollWindow == IntPtr.Zero) return;
        var centerX = target.Bounds.Left + target.Bounds.Width / 2;
        var centerY = target.Bounds.Top + target.Bounds.Height / 2;
        var delta = next ? -720 : 720;

        if (IsFileExplorerWindow(target.Window))
        {
            ScrollWithInputFallback(target.Window, centerX, centerY, delta);
            return;
        }

        var wParam = new IntPtr(delta << 16);
        var lParam = new IntPtr((centerY << 16) | (centerX & 0xFFFF));
        PostMessage(target.ScrollWindow, MouseWheel, wParam, lParam);
    }

    private static bool IsFileExplorerWindow(IntPtr window)
    {
        var className = new StringBuilder(128);
        GetClassName(window, className, className.Capacity);
        return className.ToString() is "CabinetWClass" or "ExploreWClass";
    }

    private static void ScrollWithInputFallback(
        IntPtr targetWindow,
        int targetX,
        int targetY,
        int delta)
    {
        var previousWindow = GetForegroundWindow();
        GetCursorPos(out var previousPointer);

        try
        {
            SetForegroundWindow(targetWindow);
            SetCursorPos(targetX, targetY);
            Thread.Sleep(35);

            var input = new Input
            {
                Type = 0,
                Mouse = new MouseInput
                {
                    MouseData = unchecked((uint)delta),
                    Flags = 0x0800
                }
            };
            SendInput(1, [input], Marshal.SizeOf<Input>());
            Thread.Sleep(35);
        }
        finally
        {
            SetCursorPos(previousPointer.X, previousPointer.Y);
            if (previousWindow != IntPtr.Zero && previousWindow != targetWindow)
                SetForegroundWindow(previousWindow);
        }
    }

    private static int DisplayNumber(IntPtr selected)
    {
        var number = 0;
        var result = 1;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            number++;
            if (monitor == selected) result = number;
            return true;
        }, IntPtr.Zero);
        return result;
    }

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

    public static Bitmap CaptureRegion(PhysicalCaptureRectangle region) =>
        CaptureRectangle(new NativeRectangle
        {
            Left = region.Left,
            Top = region.Top,
            Right = region.Left + region.Width,
            Bottom = region.Top + region.Height
        });

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
    private delegate bool MonitorCallback(IntPtr monitor, IntPtr dc, IntPtr rectangle, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
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

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);

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

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr dc, IntPtr clip, MonitorCallback callback, IntPtr parameter);
}

internal sealed record LockedCaptureTarget(
    IntPtr Window,
    IntPtr ScrollWindow,
    PhysicalCaptureRectangle Bounds,
    string Name,
    bool IsWindow);
