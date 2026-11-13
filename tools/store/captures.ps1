param($src, $repo)
Add-Type -AssemblyName System.Drawing
function Save-Resized($in, $out, $w, $h) {
  $i = [System.Drawing.Image]::FromFile($in)
  $b = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
  $g = [System.Drawing.Graphics]::FromImage($b)
  $g.InterpolationMode = 'HighQualityBicubic'
  $g.DrawImage($i, 0, 0, $w, $h)
  $g.Dispose(); $i.Dispose()
  $b.Save($out, [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
}
# Icône 512
$icon = [System.Drawing.Image]::FromFile("$repo\LevelUp\Assets\Icon\AppIcon.png")
$b = New-Object System.Drawing.Bitmap 512, 512
$g = [System.Drawing.Graphics]::FromImage($b); $g.InterpolationMode = 'HighQualityBicubic'
$g.DrawImage($icon, 0, 0, 512, 512); $g.Dispose(); $icon.Dispose()
$b.Save("$repo\store\icone-512.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()

# Captures retenues (24 bits, sans transparence)
$picks = @{ 'ls' = @('03_game','03b_table','01_menu','03e_round_summary'); 'pt' = @('03b_table','02d_setup') }
foreach ($lang in 'fr','en') {
  $n = 1
  foreach ($o in 'ls','pt') {
    foreach ($name in $picks[$o]) {
      $in = "$src\${o}_$lang\$name.png"
      if (Test-Path $in) {
        if ($o -eq 'ls') { Save-Resized $in "$repo\store\$lang\capture-$n-paysage.png" 1920 1080 }
        else { Save-Resized $in "$repo\store\$lang\capture-$n-portrait.png" 1080 1920 }
        $n++
      }
    }
  }
}

# Image de présentation 1024x500 : titre + main de cartes
foreach ($lang in 'fr','en') {
  $menu = New-Object System.Drawing.Bitmap "$src\ls_$lang\01_menu.png"
  $game = New-Object System.Drawing.Bitmap "$src\ls_$lang\03_game.png"
  $bg = $menu.GetPixel(900, 40)
  $b = New-Object System.Drawing.Bitmap 1024, 500, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
  $g = [System.Drawing.Graphics]::FromImage($b); $g.InterpolationMode = 'HighQualityBicubic'
  $g.Clear($bg)
  $title = New-Object System.Drawing.Rectangle 190, 80, 620, 210
  $g.DrawImage($menu, (New-Object System.Drawing.Rectangle 264, 30, 496, 168), $title, 'Pixel')
  $hand = New-Object System.Drawing.Rectangle 400, 752, 1220, 300
  $g.DrawImage($game, (New-Object System.Drawing.Rectangle 146, 232, 732, 180), $hand, 'Pixel')
  $g.Dispose(); $menu.Dispose(); $game.Dispose()
  $b.Save("$repo\store\$lang\presentation-1024x500.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
}
