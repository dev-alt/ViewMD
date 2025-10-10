Param(
    [string]$OutputDir = "Assets",
    [string]$Glyph = "MV",
    [string]$Bg = "#2D2D2D",
    [string]$Fg = "#FFFFFF"
)

$ErrorActionPreference = 'Stop'

$icons = @(
    @{ Name = 'StoreLogo.png';        W = 50;  H = 50  },
    @{ Name = 'Square44x44Logo.png';  W = 44;  H = 44  },
    @{ Name = 'Square71x71Logo.png';  W = 71;  H = 71  },
    @{ Name = 'Square150x150Logo.png';W = 150; H = 150 },
    @{ Name = 'Square310x310Logo.png';W = 310; H = 310 },
    @{ Name = 'Wide310x150Logo.png';  W = 310; H = 150 }
)

if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

# Use System.Drawing for simple PNGs (works on Windows)
Add-Type -AssemblyName System.Drawing

function New-IconPng($path, $w, $h, $bg, $fg, $glyph) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.Clear([System.Drawing.ColorTranslator]::FromHtml($bg))
        $fontSize = [Math]::Max([int]($w * 0.36), 10)
        $sizeSingle = [single]$fontSize
        # Prefer Segoe UI; fallback to Arial if unavailable
        try {
            $font = [System.Drawing.Font]::new('Segoe UI', $sizeSingle, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        } catch {
            $font = [System.Drawing.Font]::new('Arial', $sizeSingle, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        }
        $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($fg))
        $sf = New-Object System.Drawing.StringFormat
        $sf.Alignment = 'Center'
        $sf.LineAlignment = 'Center'
        $rect = New-Object System.Drawing.RectangleF 0,0,$w,$h
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
        $g.DrawString($glyph, $font, $brush, $rect, $sf)
    } finally {
        $g.Dispose()
    }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

foreach ($i in $icons) {
    $path = Join-Path $OutputDir $i.Name
    New-IconPng -path $path -w $i.W -h $i.H -bg $Bg -fg $Fg -glyph $Glyph
    Write-Host "Created $path"
}
