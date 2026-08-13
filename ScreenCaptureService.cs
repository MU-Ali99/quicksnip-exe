using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace QuickSnip;

internal enum CaptureTarget
{
    Display,
    ActiveWindow
}

internal static class ScreenCaptureService
{
    private const int ClipboardAttempts = 6;
    private static readonly TimeSpan ClipboardRetryDelay =
        TimeSpan.FromMilliseconds(80);

    public static async Task<string?> CaptureAsync(
        CaptureTarget target,
        QuickSnipSettings? settings = null)
    {
        using var captureGate = CaptureGate.TryEnter();

        if (captureGate is null)
        {
            throw new CaptureAlreadyRunningException();
        }

        if (!CaptureCooldownService.IsReady())
        {
            throw new CaptureCooldownException();
        }

        settings ??= SettingsService.Load();
        settings.Normalize();

        using var bitmap = target switch
        {
            CaptureTarget.ActiveWindow => NativeScreenCapture.CaptureActiveWindow(),
            _ => NativeScreenCapture.CaptureDisplayContainingPointer()
        };

        string? savedPath = null;

        if (settings.SavePng)
        {
            SnipFolderService.EnsureExists(settings.SaveFolder);

            var mode = target == CaptureTarget.ActiveWindow
                ? "window"
                : "display";

            var filename =
                $"quicksnip-{mode}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png";

            savedPath = Path.Combine(settings.SaveFolder, filename);
            bitmap.Save(savedPath, ImageFormat.Png);
        }

        if (settings.CopyToClipboard)
        {
            var clipboardImage = CreateClipboardImage(bitmap);
            await CopyImageToClipboardAsync(clipboardImage);
        }

        CaptureCooldownService.MarkCompleted();

        return savedPath;
    }

    private static BitmapSource CreateClipboardImage(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static async Task CopyImageToClipboardAsync(BitmapSource image)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= ClipboardAttempts; attempt++)
        {
            try
            {
                System.Windows.Clipboard.SetImage(image);
                return;
            }
            catch (System.Runtime.InteropServices.COMException exception)
            {
                lastError = exception;

                if (attempt < ClipboardAttempts)
                {
                    await Task.Delay(ClipboardRetryDelay);
                }
            }
        }

        throw new InvalidOperationException(
            "The screenshot output succeeded, but the Windows clipboard was busy.",
            lastError);
    }
}

internal sealed class CaptureAlreadyRunningException : Exception
{
    public CaptureAlreadyRunningException()
        : base("A QuickSnip capture is already running.")
    {
    }
}

internal sealed class CaptureCooldownException : Exception
{
    public CaptureCooldownException()
        : base("QuickSnip ignored an accidental repeated click.")
    {
    }
}
