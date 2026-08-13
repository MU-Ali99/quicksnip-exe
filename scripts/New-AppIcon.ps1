param(
    [string]$Source = "$PSScriptRoot\..\Assets\QuickSnip-source.png",
    [string]$Destination = "$PSScriptRoot\..\Assets\QuickSnip.ico"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$destinationPath = [System.IO.Path]::GetFullPath($Destination)
$destinationDirectory = [System.IO.Path]::GetDirectoryName($destinationPath)
[System.IO.Directory]::CreateDirectory($destinationDirectory) | Out-Null

$sourceImage = [System.Drawing.Image]::FromFile($Source)
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngPayloads = [System.Collections.Generic.List[byte[]]]::new()

try {
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new(
            $size,
            $size,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

                $padding = [Math]::Max(1, [Math]::Round($size * 0.03))
                $available = $size - (2 * $padding)
                $scale = [Math]::Min(
                    $available / $sourceImage.Width,
                    $available / $sourceImage.Height)
                $width = [Math]::Max(1, [Math]::Round($sourceImage.Width * $scale))
                $height = [Math]::Max(1, [Math]::Round($sourceImage.Height * $scale))
                $x = [Math]::Round(($size - $width) / 2)
                $y = [Math]::Round(($size - $height) / 2)

                $graphics.DrawImage($sourceImage, $x, $y, $width, $height)
            }
            finally {
                $graphics.Dispose()
            }

            $stream = [System.IO.MemoryStream]::new()
            try {
                $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
                $pngPayloads.Add($stream.ToArray())
            }
            finally {
                $stream.Dispose()
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }
}
finally {
    $sourceImage.Dispose()
}

$fileStream = [System.IO.File]::Create($destinationPath)
$writer = [System.IO.BinaryWriter]::new($fileStream)

try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)

    $offset = 6 + (16 * $sizes.Count)

    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $size = $sizes[$index]
        $payload = $pngPayloads[$index]
        $dimension = if ($size -eq 256) { 0 } else { $size }

        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$payload.Length)
        $writer.Write([uint32]$offset)

        $offset += $payload.Length
    }

    foreach ($payload in $pngPayloads) {
        $writer.Write($payload)
    }
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}

Write-Output $destinationPath
