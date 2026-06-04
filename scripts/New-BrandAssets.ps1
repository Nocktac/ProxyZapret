$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Path $assets -Force | Out-Null

function New-ProxyZapretBitmap {
    param([int] $Size)

    $bitmap = New-Object Drawing.Bitmap $Size, $Size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([Drawing.Color]::Transparent)

    $scale = $Size / 256.0
    $rect = New-Object Drawing.RectangleF (18 * $scale), (18 * $scale), (220 * $scale), (220 * $scale)
    $radius = 48 * $scale

    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    $bgBrush = New-Object Drawing.Drawing2D.LinearGradientBrush -ArgumentList @(
        $rect,
        ([Drawing.Color]::FromArgb(255, 18, 25, 38)),
        ([Drawing.Color]::FromArgb(255, 8, 13, 24)),
        ([Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    )
    $graphics.FillPath($bgBrush, $path)

    $ringPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(120, 91, 166, 255)), (7 * $scale)
    $ringPen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($ringPen, $path)

    $shield = New-Object Drawing.Drawing2D.GraphicsPath
    $shieldPoints = @(
        (New-Object Drawing.PointF (128 * $scale), (55 * $scale)),
        (New-Object Drawing.PointF (184 * $scale), (77 * $scale)),
        (New-Object Drawing.PointF (174 * $scale), (158 * $scale)),
        (New-Object Drawing.PointF (128 * $scale), (205 * $scale)),
        (New-Object Drawing.PointF (82 * $scale), (158 * $scale)),
        (New-Object Drawing.PointF (72 * $scale), (77 * $scale))
    )
    $shield.AddPolygon($shieldPoints)

    $shieldRect = New-Object Drawing.RectangleF (70 * $scale), (50 * $scale), (116 * $scale), (160 * $scale)
    $shieldBrush = New-Object Drawing.Drawing2D.LinearGradientBrush -ArgumentList @(
        $shieldRect,
        ([Drawing.Color]::FromArgb(255, 77, 211, 168)),
        ([Drawing.Color]::FromArgb(255, 55, 126, 255)),
        ([Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    )
    $graphics.FillPath($shieldBrush, $shield)

    $highlightRect = New-Object Drawing.RectangleF (86 * $scale), (64 * $scale), (54 * $scale), (120 * $scale)
    $highlightBrush = New-Object Drawing.Drawing2D.LinearGradientBrush -ArgumentList @(
        $highlightRect,
        ([Drawing.Color]::FromArgb(110, 255, 255, 255)),
        ([Drawing.Color]::FromArgb(0, 255, 255, 255)),
        ([Drawing.Drawing2D.LinearGradientMode]::BackwardDiagonal)
    )
    $graphics.FillPath($highlightBrush, $shield)

    $shieldPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(235, 238, 246, 255)), (9 * $scale)
    $shieldPen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($shieldPen, $shield)

    if ($Size -ge 32) {
        $checkPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 245, 250, 255)), (13 * $scale)
        $checkPen.StartCap = [Drawing.Drawing2D.LineCap]::Round
        $checkPen.EndCap = [Drawing.Drawing2D.LineCap]::Round
        $checkPen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
        $graphics.DrawLines($checkPen, @(
            (New-Object Drawing.PointF (101 * $scale), (133 * $scale)),
            (New-Object Drawing.PointF (121 * $scale), (153 * $scale)),
            (New-Object Drawing.PointF (158 * $scale), (109 * $scale))
        ))
        $checkPen.Dispose()
    }

    $shieldPen.Dispose()
    $highlightBrush.Dispose()
    $shieldBrush.Dispose()
    $shield.Dispose()
    $ringPen.Dispose()
    $bgBrush.Dispose()
    $path.Dispose()
    $graphics.Dispose()
    return $bitmap
}

function Convert-BitmapToPngBytes {
    param([Drawing.Bitmap] $Bitmap)

    $stream = New-Object IO.MemoryStream
    try {
        $Bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
        return $stream.ToArray()
    }
    finally {
        $stream.Dispose()
    }
}

function Write-UInt16 {
    param([IO.BinaryWriter] $Writer, [int] $Value)
    $Writer.Write([byte]($Value -band 0xff))
    $Writer.Write([byte](($Value -shr 8) -band 0xff))
}

function Write-UInt32 {
    param([IO.BinaryWriter] $Writer, [int64] $Value)
    $Writer.Write([byte]($Value -band 0xff))
    $Writer.Write([byte](($Value -shr 8) -band 0xff))
    $Writer.Write([byte](($Value -shr 16) -band 0xff))
    $Writer.Write([byte](($Value -shr 24) -band 0xff))
}

function New-MultiSizeIcon {
    param(
        [int[]] $Sizes,
        [string] $Path
    )

    $entries = @()
    foreach ($size in $Sizes) {
        $bitmap = New-ProxyZapretBitmap -Size $size
        try {
            $entries += [pscustomobject]@{
                Size = $size
                Data = Convert-BitmapToPngBytes -Bitmap $bitmap
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }

    $stream = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::Write)
    $writer = New-Object IO.BinaryWriter $stream
    try {
        Write-UInt16 $writer 0
        Write-UInt16 $writer 1
        Write-UInt16 $writer $entries.Count

        $offset = 6 + (16 * $entries.Count)
        foreach ($entry in $entries) {
            $encodedSize = $entry.Size
            if ($encodedSize -eq 256) { $encodedSize = 0 }
            $writer.Write([byte]$encodedSize)
            $writer.Write([byte]$encodedSize)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            Write-UInt16 $writer 1
            Write-UInt16 $writer 32
            Write-UInt32 $writer $entry.Data.Length
            Write-UInt32 $writer $offset
            $offset += $entry.Data.Length
        }

        foreach ($entry in $entries) {
            $writer.Write([byte[]]$entry.Data)
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

$png = New-ProxyZapretBitmap -Size 512
$png.Save((Join-Path $assets 'ProxyZapret.png'), [Drawing.Imaging.ImageFormat]::Png)
$png.Dispose()

New-MultiSizeIcon -Sizes @(16, 24, 32, 48, 64, 128, 256) -Path (Join-Path $assets 'ProxyZapret.ico')

Write-Host "Generated: $assets\ProxyZapret.ico"
Write-Host "Generated: $assets\ProxyZapret.png"
