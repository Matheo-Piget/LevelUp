param($src, $repo)
Add-Type -AssemblyName System.Drawing
$fonts = New-Object System.Drawing.Text.PrivateFontCollection
$fonts.AddFontFile("$repo\LevelUp\Assets\Resources\Fonts\DMSerifDisplay-Regular.ttf")
$fonts.AddFontFile("$repo\LevelUp\Assets\Resources\Fonts\Inter-SemiBold.ttf")
$serif = $fonts.Families | Where-Object { $_.Name -like 'DM Serif*' } | Select-Object -First 1
$inter = $fonts.Families | Where-Object { $_.Name -like 'Inter*' } | Select-Object -First 1
$subs = @{ fr = 'LE JEU DE CARTES MONTE-NIVEAU'; en = 'THE LEVEL-CLIMBING CARD GAME' }
foreach ($lang in 'fr','en') {
  $menu = New-Object System.Drawing.Bitmap "$src\ls_fr\01_menu.png"
  $b = New-Object System.Drawing.Bitmap 1024, 500, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
  $g = [System.Drawing.Graphics]::FromImage($b)
  $g.InterpolationMode = 'HighQualityBicubic'; $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
  # Fond : la zone des cartes du menu, d'un seul tenant (aucun raccord).
  $g.DrawImage($menu, (New-Object System.Drawing.Rectangle 0, 0, 1024, 500), (New-Object System.Drawing.Rectangle 790, 150, 1110, 542), 'Pixel')
  # Voile sombre à gauche pour le titre.
  $rect = New-Object System.Drawing.Rectangle 0, 0, 700, 500
  $veil = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(245, 10, 13, 14)), ([System.Drawing.Color]::FromArgb(0, 10, 13, 14)), 0.0
  $g.FillRectangle($veil, 0, 0, 699, 500)
  $title = New-Object System.Drawing.Font $serif, 96, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
  $fmt = [System.Drawing.StringFormat]::GenericTypographic
  $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(239, 234, 224))
  $gold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(216, 183, 106))
  $muted = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(167, 161, 152))
  $x = 56; $y = 150
  $g.DrawString('LEVEL', $title, $white, $x, $y, $fmt)
  $w = $g.MeasureString('LEVEL', $title, 2000, $fmt).Width
  $g.DrawString('UP', $title, $gold, $x + $w + 24, $y, $fmt)
  $small = New-Object System.Drawing.Font $inter, 15, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
  $cx = $x + 2
  foreach ($ch in $subs[$lang].ToCharArray()) {
    $s = [string]$ch
    $g.DrawString($s, $small, $muted, $cx, 276, $fmt)
    $cw = if ($s -eq ' ') { 5 } else { $g.MeasureString($s, $small, 100, $fmt).Width }
    $cx += $cw + 3.5
  }
  $g.Dispose(); $menu.Dispose()
  $b.Save("$repo\store\$lang\presentation-1024x500.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
}
