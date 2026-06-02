$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class IconNativeMethods {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool DestroyIcon(IntPtr handle);
}
"@

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Path $assets -Force | Out-Null

function New-ProxyZapretBitmap {
    param([int] $Size)

    $bitmap = New-Object Drawing.Bitmap $Size, $Size
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([Drawing.Color]::Transparent)

    $scale = $Size / 64.0
    $background = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 14, 18, 27))
    $accent = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 67, 211, 164))
    $darkPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 14, 18, 27)), (4 * $scale)
    $darkPen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $darkPen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $darkPen.EndCap = [Drawing.Drawing2D.LineCap]::Round

    $graphics.FillEllipse($background, 1 * $scale, 1 * $scale, 62 * $scale, 62 * $scale)
    $graphics.FillEllipse($accent, 5 * $scale, 5 * $scale, 54 * $scale, 54 * $scale)

    $shield = @(
        (New-Object Drawing.PointF (32 * $scale), (16 * $scale)),
        (New-Object Drawing.PointF (45 * $scale), (21 * $scale)),
        (New-Object Drawing.PointF (43 * $scale), (38 * $scale)),
        (New-Object Drawing.PointF (32 * $scale), (48 * $scale)),
        (New-Object Drawing.PointF (21 * $scale), (38 * $scale)),
        (New-Object Drawing.PointF (19 * $scale), (21 * $scale))
    )
    $graphics.DrawPolygon($darkPen, $shield)
    $graphics.DrawLine($darkPen, 25 * $scale, 31 * $scale, 30 * $scale, 36 * $scale)
    $graphics.DrawLine($darkPen, 30 * $scale, 36 * $scale, 39 * $scale, 27 * $scale)

    $darkPen.Dispose()
    $accent.Dispose()
    $background.Dispose()
    $graphics.Dispose()
    return $bitmap
}

$png = New-ProxyZapretBitmap -Size 256
$png.Save((Join-Path $assets 'ProxyZapret.png'), [Drawing.Imaging.ImageFormat]::Png)
$png.Dispose()

$iconBitmap = New-ProxyZapretBitmap -Size 64
$handle = $iconBitmap.GetHicon()
try {
    $icon = [Drawing.Icon]::FromHandle($handle)
    $stream = [IO.File]::Open((Join-Path $assets 'ProxyZapret.ico'), [IO.FileMode]::Create)
    try {
        $icon.Save($stream)
    }
    finally {
        $stream.Dispose()
        $icon.Dispose()
    }
}
finally {
    [IconNativeMethods]::DestroyIcon($handle) | Out-Null
    $iconBitmap.Dispose()
}

Write-Host "Generated: $assets\ProxyZapret.ico"
Write-Host "Generated: $assets\ProxyZapret.png"

