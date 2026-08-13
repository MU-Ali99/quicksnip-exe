using System.Windows;

namespace QuickSnip;

internal static class WindowPlacementService
{
    private const double ScreenMargin = 28;

    public static void Restore(
        Window window,
        WindowPlacementSettings placement,
        double defaultWidth,
        double defaultHeight)
    {
        var desktopLeft = SystemParameters.VirtualScreenLeft;
        var desktopTop = SystemParameters.VirtualScreenTop;
        var desktopRight = desktopLeft + SystemParameters.VirtualScreenWidth;
        var desktopBottom = desktopTop + SystemParameters.VirtualScreenHeight;

        var maxWidth = Math.Max(window.MinWidth, SystemParameters.WorkArea.Width - ScreenMargin * 2);
        var maxHeight = Math.Max(window.MinHeight, SystemParameters.WorkArea.Height - ScreenMargin * 2);
        var width = Math.Clamp(placement.Width ?? defaultWidth, window.MinWidth, maxWidth);
        var height = Math.Clamp(placement.Height ?? defaultHeight, window.MinHeight, maxHeight);

        var fallbackLeft = SystemParameters.WorkArea.Left +
            (SystemParameters.WorkArea.Width - width) / 2;
        var fallbackTop = SystemParameters.WorkArea.Top +
            (SystemParameters.WorkArea.Height - height) / 2;

        var left = placement.Left ?? fallbackLeft;
        var top = placement.Top ?? fallbackTop;

        // Reject placements that no longer overlap the connected virtual desktop.
        if (left + width < desktopLeft + ScreenMargin ||
            left > desktopRight - ScreenMargin ||
            top + height < desktopTop + ScreenMargin ||
            top > desktopBottom - ScreenMargin)
        {
            left = fallbackLeft;
            top = fallbackTop;
        }

        left = Math.Clamp(left, desktopLeft + ScreenMargin, desktopRight - width - ScreenMargin);
        top = Math.Clamp(top, desktopTop + ScreenMargin, desktopBottom - height - ScreenMargin);

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Width = width;
        window.Height = height;
        window.Left = left;
        window.Top = top;
    }

    public static void Save(Window window, WindowPlacementSettings placement)
    {
        var bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;

        placement.Left = bounds.Left;
        placement.Top = bounds.Top;
        placement.Width = bounds.Width;
        placement.Height = bounds.Height;
    }
}
