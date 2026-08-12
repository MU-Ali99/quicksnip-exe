using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace RightSnip;

internal static class ScreenCaptureService
{
    private const int ClipboardAttempts = 6;
    private static readonly TimeSpan ClipboardRetryDelay =
        TimeSpan.FromMilliseconds(80);

    public static async Task<string> CaptureCurrentDisplayAsync()
    {
        var pointerPosition = Forms.Cursor.Position;
        var displayBounds =
            Forms.Screen.FromPoint(pointerPosition).Bounds;

        using var bitmap = new Bitmap(
            displayBounds.Width,
            displayBounds.Height,
            PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                displayBounds.Location,
                System.Drawing.Point.Empty,
                displayBounds.Size,
                CopyPixelOperation.SourceCopy);
        }

        var screenshotDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "RightSnip");

        Directory.CreateDirectory(screenshotDirectory);

        var filename =
            $"rightsnip-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png";

        var savedPath =
            Path.Combine(screenshotDirectory, filename);

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
