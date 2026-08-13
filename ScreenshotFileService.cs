using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SkiaSharp;

namespace QuickSnip;

internal static class ScreenshotFileService
{
    public static string Save(
        Bitmap bitmap,
        CaptureTarget target,
        QuickSnipSettings settings)
    {
        var now = DateTime.Now;
        var folder = settings.SaveFolder;
        Directory.CreateDirectory(folder);

        var mode = target switch
        {
            CaptureTarget.ActiveWindow => "window",
            CaptureTarget.Drag => "drag",
            CaptureTarget.Lock => "lock",
            _ => "display"
        };

        var extension = settings.ImageFormat switch
        {
            "JPEG" => ".jpg",
            "WebP" => ".webp",
            _ => ".png"
        };
        var filename = $"quicksnip-{mode}-{now:yyyy-MM-dd-HH-mm-ss-fff}{extension}";
        var path = GetUniquePath(folder, filename);

        using var source = new MemoryStream();
        bitmap.Save(source, ImageFormat.Png);
        source.Position = 0;
        using var skBitmap = SKBitmap.Decode(source) ??
            throw new InvalidOperationException("QuickSnip could not prepare the captured image for saving.");
        using var image = SKImage.FromBitmap(skBitmap);
        var format = settings.ImageFormat switch
        {
            "JPEG" => SKEncodedImageFormat.Jpeg,
            "WebP" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
        using var encoded = image.Encode(format, QualityValue(settings.ImageQuality)) ??
            throw new InvalidOperationException($"QuickSnip could not encode {settings.ImageFormat}.");
        using var output = File.Create(path);
        encoded.SaveTo(output);

        return path;
    }

    private static string GetUniquePath(string folder, string filename)
    {
        var path = Path.Combine(folder, filename);
        if (!File.Exists(path)) return path;

        var stem = Path.GetFileNameWithoutExtension(filename);
        var extension = Path.GetExtension(filename);
        for (var index = 2; ; index++)
        {
            path = Path.Combine(folder, $"{stem}-{index}{extension}");
            if (!File.Exists(path)) return path;
        }
    }

    private static int QualityValue(string quality) => quality switch
    {
        "Low" => 45,
        "Medium" => 70,
        _ => 90
    };
}
