$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = "C:\Users\myangell\Documents\My Games\Terraria\tModLoader\ModSources\DieWithASmile"
$bgPath = "C:\Users\myangell\Downloads\123312321321.png"
$wingPath = "C:\Users\myangell\Downloads\211232132123121312321231.png"
$calPath = Join-Path $root "Assets\Textures\Menu\CalamitasBackground.png"
$dfPath = Join-Path $root "Assets\Textures\Menu\DeltaruneHeartsBackground.png"
$modList = Join-Path $root "Assets\Textures\UI\ModList"
$previews = Join-Path $root "Assets\Textures\Menu\Previews"
$logoOut = Join-Path $root "Logo"
$tmp = Join-Path $root "obj\logo-gen"
$github = Join-Path $root "Calamitasgithub"
$ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source

function Load-Bitmap([string]$path) {
	$bytes = [IO.File]::ReadAllBytes($path)
	$ms = New-Object IO.MemoryStream(,$bytes)
	$img = [System.Drawing.Image]::FromStream($ms, $false, $false)
	$bmp = New-Object System.Drawing.Bitmap $img.Width, $img.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
	$g.DrawImage($img, 0, 0, $img.Width, $img.Height)
	$g.Dispose()
	$img.Dispose()
	$ms.Dispose()
	return $bmp
}

function Save-Png([System.Drawing.Bitmap]$bmp, [string]$path) {
	$dir = Split-Path $path
	if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
	$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Clone32([System.Drawing.Bitmap]$src) {
	$bmp = New-Object System.Drawing.Bitmap $src.Width, $src.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
	$g.DrawImageUnscaled($src, 0, 0)
	$g.Dispose()
	return $bmp
}

function Make-TransparentBlack([System.Drawing.Bitmap]$src, [int]$cut = 22) {
	$bmp = Clone32 $src
	$rect = New-Object System.Drawing.Rectangle 0, 0, $bmp.Width, $bmp.Height
	$lock = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$stride = $lock.Stride
	$len = $stride * $bmp.Height
	$raw = New-Object byte[] $len
	[Runtime.InteropServices.Marshal]::Copy($lock.Scan0, $raw, 0, $len)
	for ($y = 0; $y -lt $bmp.Height; $y++) {
		$o = $y * $stride
		for ($x = 0; $x -lt $bmp.Width; $x++) {
			$i = $o + $x * 4
			$b = $raw[$i]; $g = $raw[$i+1]; $r = $raw[$i+2]
			if ($r -le $cut -and $g -le $cut -and $b -le $cut) {
				$raw[$i] = 0; $raw[$i+1] = 0; $raw[$i+2] = 0; $raw[$i+3] = 0
			}
		}
	}
	[Runtime.InteropServices.Marshal]::Copy($raw, 0, $lock.Scan0, $len)
	$bmp.UnlockBits($lock)
	return $bmp
}

function New-Gfx([System.Drawing.Bitmap]$bmp, [bool]$nearest) {
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
	$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
	if ($nearest) {
		$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
		$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
	} else {
		$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
		$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
	}
	return $g
}

function Draw-Cover([System.Drawing.Graphics]$g, [System.Drawing.Image]$img, [int]$dw, [int]$dh) {
	$sa = $img.Width / [double]$img.Height
	$da = $dw / [double]$dh
	if ($sa -gt $da) {
		$srcH = $img.Height
		$srcW = [int]($img.Height * $da)
		$sx = [int](($img.Width - $srcW) / 2)
		$g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $dw, $dh), (New-Object System.Drawing.Rectangle $sx, 0, $srcW, $srcH), [System.Drawing.GraphicsUnit]::Pixel)
	} else {
		$srcW = $img.Width
		$srcH = [int]($img.Width / $da)
		$sy = [int](($img.Height - $srcH) / 2)
		$g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $dw, $dh), (New-Object System.Drawing.Rectangle 0, $sy, $srcW, $srcH), [System.Drawing.GraphicsUnit]::Pixel)
	}
}

foreach ($d in @($modList, $previews, $logoOut, $tmp, (Join-Path $github "calamitas"), (Join-Path $github "dontforget"))) {
	if (!(Test-Path $d)) { New-Item -ItemType Directory -Path $d | Out-Null }
}

$bg = Load-Bitmap $bgPath
$wingRaw = Load-Bitmap $wingPath
$wing = Make-TransparentBlack $wingRaw
$wingRaw.Dispose()

Save-Png $bg (Join-Path $modList "LogoBg.png")
Save-Png $wing (Join-Path $modList "LogoWing.png")

$fill = $bg.GetPixel(6, 6)
$frames = 8
$amp = 7.0
for ($i = 0; $i -lt $frames; $i++) {
	$t = $i / [double]$frames
	$bob = [int][math]::Round([math]::Sin($t * 2.0 * [math]::PI) * $amp)

	$sq = New-Object System.Drawing.Bitmap 480, 480, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$gs = New-Gfx $sq $true
	$gs.Clear([System.Drawing.Color]::Transparent)
	$gs.DrawImageUnscaled($bg, 0, 0)
	$gs.DrawImageUnscaled($wing, 0, $bob)
	$gs.Dispose()
	Save-Png $sq (Join-Path $tmp ("logo_{0:D2}.png" -f $i))

	$wide = New-Object System.Drawing.Bitmap 1920, 498, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$gw = New-Gfx $wide $true
	$gw.Clear($fill)
	$gw.DrawImage($sq, 0, 0, 498, 498)
	$gw.Dispose()
	Save-Png $wide (Join-Path $modList ("Frame{0:D2}.png" -f $i))
	$wide.Dispose()

	$icon = New-Object System.Drawing.Bitmap 80, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$gi = New-Gfx $icon $true
	$gi.Clear([System.Drawing.Color]::Transparent)
	$gi.DrawImage($sq, 0, 0, 80, 80)
	$gi.Dispose()
	if ($i -eq 0) { Save-Png $icon (Join-Path $root "icon.png") }
	$icon.Dispose()
	$sq.Dispose()
}

# smoother gif frames
$gifN = 12
for ($i = 0; $i -lt $gifN; $i++) {
	$t = $i / [double]$gifN
	$bob = [int][math]::Round([math]::Sin($t * 2.0 * [math]::PI) * $amp)
	$sq = New-Object System.Drawing.Bitmap 480, 480, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$gs = New-Gfx $sq $true
	$gs.Clear([System.Drawing.Color]::Transparent)
	$gs.DrawImageUnscaled($bg, 0, 0)
	$gs.DrawImageUnscaled($wing, 0, $bob)
	$gs.Dispose()
	Save-Png $sq (Join-Path $tmp ("gif_{0:D2}.png" -f $i))
	$sq.Dispose()
}

$bg.Dispose()
$wing.Dispose()

if ($ffmpeg) {
	$gifPath = Join-Path $logoOut "mod-icon.gif"
	& $ffmpeg -y -framerate 12 -i (Join-Path $tmp "gif_%02d.png") -vf "fps=12,scale=480:480:flags=neighbor,split[s0][s1];[s0]palettegen=max_colors=96:reserve_transparent=1[p];[s1][p]paletteuse=dither=none" $gifPath
	Write-Output "GIF $gifPath"
} else {
	Write-Output "ffmpeg missing, skipped gif"
}

function Make-Preview([string]$src, [string]$dst, [int]$w, [int]$h) {
	$im = Load-Bitmap $src
	$out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$g = New-Gfx $out $false
	Draw-Cover $g $im $w $h
	$g.Dispose()
	Save-Png $out $dst
	$out.Dispose()
	$im.Dispose()
}

Make-Preview $calPath (Join-Path $previews "Calamitas.png") 480 292
Make-Preview $dfPath (Join-Path $previews "DontForget.png") 480 292
Make-Preview $calPath (Join-Path $github "calamitas\preview.png") 480 292
Make-Preview $dfPath (Join-Path $github "dontforget\preview.png") 480 292

Copy-Item -Force $calPath (Join-Path $github "calamitas\CalamitasBackground.png")
Copy-Item -Force $dfPath (Join-Path $github "dontforget\DeltaruneHeartsBackground.png")

Write-Output "done"
Get-ChildItem $modList | ForEach-Object { "$($_.Name) $($_.Length)" }
Get-ChildItem $previews | ForEach-Object { "$($_.Name) $($_.Length)" }
Get-ChildItem $github -Recurse -File | ForEach-Object { "$($_.FullName.Substring($github.Length+1)) $($_.Length)" }
if (Test-Path (Join-Path $logoOut "mod-icon.gif")) { "gif $((Get-Item (Join-Path $logoOut 'mod-icon.gif')).Length)" }
