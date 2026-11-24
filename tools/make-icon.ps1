param(
    [string]$SourcePath = (Join-Path $PSScriptRoot '..\assets\suzuha.png'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\assets\suzuha.ico')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskSource = [System.Drawing.Image]::FromFile([System.IO.Path]::GetFullPath($SourcePath))
$taskFrames = [System.Collections.Generic.List[byte[]]]::new()
$taskSizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
try {
    foreach ($taskSize in $taskSizes) {
        $taskBitmap = [System.Drawing.Bitmap]::new($taskSize, $taskSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $taskGraphics = [System.Drawing.Graphics]::FromImage($taskBitmap)
        $taskStream = [System.IO.MemoryStream]::new()
        try {
            $taskGraphics.Clear([System.Drawing.Color]::Transparent)
            $taskGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $taskGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $taskScale = [Math]::Min($taskSize / $taskSource.Width, $taskSize / $taskSource.Height)
            $taskWidth = [int][Math]::Round($taskSource.Width * $taskScale)
            $taskHeight = [int][Math]::Round($taskSource.Height * $taskScale)
            $taskRect = [System.Drawing.Rectangle]::new([int](($taskSize - $taskWidth) / 2), [int](($taskSize - $taskHeight) / 2), $taskWidth, $taskHeight)
            $taskGraphics.DrawImage($taskSource, $taskRect)
            $taskBitmap.Save($taskStream, [System.Drawing.Imaging.ImageFormat]::Png)
            $taskFrames.Add($taskStream.ToArray())
        } finally { $taskStream.Dispose(); $taskGraphics.Dispose(); $taskBitmap.Dispose() }
    }
    $taskFile = [System.IO.File]::Create([System.IO.Path]::GetFullPath($OutputPath))
    $taskWriter = [System.IO.BinaryWriter]::new($taskFile)
    try {
        $taskWriter.Write([uint16]0)
        $taskWriter.Write([uint16]1)
        $taskWriter.Write([uint16]$taskSizes.Count)
        $taskOffset = 6 + 16 * $taskSizes.Count
        for ($taskIndex = 0; $taskIndex -lt $taskSizes.Count; $taskIndex++) {
            $taskDimension = if ($taskSizes[$taskIndex] -eq 256) { 0 } else { $taskSizes[$taskIndex] }
            $taskWriter.Write([byte]$taskDimension)
            $taskWriter.Write([byte]$taskDimension)
            $taskWriter.Write([byte]0)
            $taskWriter.Write([byte]0)
            $taskWriter.Write([uint16]1)
            $taskWriter.Write([uint16]32)
            $taskWriter.Write([uint32]$taskFrames[$taskIndex].Length)
            $taskWriter.Write([uint32]$taskOffset)
            $taskOffset += $taskFrames[$taskIndex].Length
        }
        foreach ($taskFrame in $taskFrames) { $taskWriter.Write([byte[]]$taskFrame) }
    } finally { $taskWriter.Dispose(); $taskFile.Dispose() }
} finally { $taskSource.Dispose() }
Write-Output ([System.IO.Path]::GetFullPath($OutputPath))
