Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"

$srcDir = "D:\Download\角色"
$dstRoot = "C:\Users\19810\Mother&Daughter\Assets\ProPlatformer\_Arts\Textures\Player"
$fmt = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb

foreach ($mom in @(2, 3, 4)) {
    $pairs = @( @{gif = "站立"; name = "Idle"}, @{gif = "跑"; name = "Run"}, @{gif = "跳"; name = "Jump"} )
    foreach ($pair in $pairs) {
        $af = $pair.gif
        $an = $pair.name
        $p = Join-Path $srcDir "母亲$mom$af.gif"
        $img = [System.Drawing.Image]::FromFile($p)
        $fd = New-Object System.Drawing.Imaging.FrameDimension($img.FrameDimensionsList[0])
        $n = $img.GetFrameCount($fd)
        $frameW = $img.Width
        $frameH = $img.Height

        $stripW = $frameW * $n
        $strip = New-Object System.Drawing.Bitmap -ArgumentList $stripW, $frameH, $fmt
        $g = [System.Drawing.Graphics]::FromImage($strip)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $g.Clear([System.Drawing.Color]::Transparent)
        for ($i = 0; $i -lt $n; $i++) {
            $img.SelectActiveFrame($fd, $i)
            $g.DrawImage($img, (New-Object System.Drawing.Rectangle -ArgumentList ($i * $frameW), 0, $frameW, $frameH))
        }
        $g.Dispose()

        $outDir = Join-Path $dstRoot "Mother$mom"
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null
        $outPath = Join-Path $outDir "Mother$mom$an.png"
        $strip.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $strip.Dispose()
        $img.Dispose()
        Write-Host "Mother$mom$an : ${frameW}x${frameH} x ${n} frames -> $outPath"
    }
}
Write-Host "DONE"
