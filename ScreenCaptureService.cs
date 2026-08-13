using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace QuickSnip;

internal enum CaptureTarget
{
    Display,
    ActiveWindow,
    Drag
}

internal static class ScreenCaptureService
{
    private const int ClipboardAttempts = 10;
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

        Bitmap bitmap;

        if (target == CaptureTarget.Drag)
        {
            var overlay = new DragSnipWindow();
            var accepted = overlay.ShowDialog() == true;

            if (!accepted || overlay.Selection is not { } selection)
            {
                return null;
            }

            // Let Desktop Window Manager remove the overlay before copying pixels.
            await Task.Delay(80);
            bitmap = NativeScreenCapture.CaptureRegion(selection);
        }
        else
        {
            bitmap = target == CaptureTarget.ActiveWindow
                ? NativeScreenCapture.CaptureActiveWindow()
                : NativeScreenCapture.CaptureDisplayContainingPointer();
        }

        using (bitmap)
        {

        string? savedPath = null;

        if (settings.SavePng)
        {
            savedPath = ScreenshotFileService.Save(bitmap, target, settings);
        }

        var clipboardCopied = false;
        if (settings.CopyToClipboard)
        {
            var clipboardImage = CreateClipboardImage(bitmap);
            try
            {
                await CopyImageToClipboardAsync(clipboardImage);
                clipboardCopied = true;
            }
            catch (Exception exception)
            {
                AppLogger.Error("Copy screenshot to clipboard", exception);
                ShowToast("Clipboard was busy", savedPath is null
                    ? "The snip could not be copied."
                    : "The image was still saved successfully.");
            }
        }

            CaptureCooldownService.MarkCompleted();

            if (settings.ShowCaptureToast && (clipboardCopied || savedPath is not null))
            {
                var detail = settings.SavePng && settings.CopyToClipboard
                    ? "Copied and saved"
                    : settings.CopyToClipboard
                        ? "Copied to clipboard"
                        : "Saved";
                ShowToast("Snip taken", detail);
            }

            return savedPath;
        }
    }

    private static void ShowToast(string title, string detail)
    {
        var toast = new CaptureToastWindow(title, detail);
        toast.ShowDialog();
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
