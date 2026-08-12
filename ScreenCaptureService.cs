using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RightSnip;

internal static class ScreenCaptureService
{
    private const int ClipboardAttempts = 6;
    private static readonly TimeSpan ClipboardRetryDelay =
        TimeSpan.FromMilliseconds(80);

    public static async Task<string> CaptureCurrentDisplayAsync()
    {
        using var captureGate = CaptureGate.TryEnter();

        if (captureGate is null)
        {
            throw new CaptureAlreadyRunningException();
        }

        using var bitmap =
            NativeScreenCapture.CaptureDisplayContainingPointer();

        SnipFolderService.EnsureExists();

        var filename =
            $"rightsnip-{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png";

        var savedPath =
            Path.Combine(SnipFolderService.Path, filename);

        bitmap.Save(savedPath, ImageFormat.Png);

        var clipboardImage = CreateClipboardImage(bitmap);
        await CopyImageToClipboardAsync(clipboardImage);

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

    private static async Task CopyImageToClipboardAsync(
        BitmapSource image)
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
            "The screenshot was saved, but the Windows clipboard was busy.",
            lastError);
    }
}

internal sealed class CaptureAlreadyRunningException : Exception
{
    public CaptureAlreadyRunningException()
        : base("A RightSnip capture is already running.")
    {
    }
}
