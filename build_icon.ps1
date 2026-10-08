param(
  [string]$Source = "$PSScriptRoot\assets\ikona_spolszczenia.png",
  [string]$Destination = "$PSScriptRoot\build\ikona_spolszczenia.ico"
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$sourceImage = [Drawing.Image]::FromFile($Source)
$frames = @()
try {
  foreach ($size in @(16,24,32,48,64,128,256)) {
    $bitmap = [Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $buffer = [IO.MemoryStream]::new()
    try {
      $graphics.Clear([Drawing.Color]::Transparent)
      $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
      $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
      $ratio = [Math]::Min($size / $sourceImage.Width,$size / $sourceImage.Height)
      $width = [int][Math]::Round($sourceImage.Width * $ratio)
      $height = [int][Math]::Round($sourceImage.Height * $ratio)
      $x = [int][Math]::Floor(($size - $width) / 2)
      $y = [int][Math]::Floor(($size - $height) / 2)
      $graphics.DrawImage($sourceImage,[Drawing.Rectangle]::new($x,$y,$width,$height))
      $bitmap.Save($buffer,[Drawing.Imaging.ImageFormat]::Png)
      $frames += [pscustomobject]@{Size=$size;Bytes=$buffer.ToArray()}
    } finally { $buffer.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
  }
} finally { $sourceImage.Dispose() }
$parent = Split-Path -Parent $Destination
New-Item -ItemType Directory -Path $parent -Force | Out-Null
$stream = [IO.File]::Create($Destination)
$writer = [IO.BinaryWriter]::new($stream)
try {
  $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
  $offset = 6 + 16 * $frames.Count
  foreach ($frame in $frames) {
    $dimension = if ($frame.Size -eq 256) {0} else {$frame.Size}
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
    $offset += $frame.Bytes.Length
  }
  foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
} finally { $writer.Dispose() }
