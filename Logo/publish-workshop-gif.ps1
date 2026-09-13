# Sets Logo\mod-icon.gif as the Steam Workshop preview via SteamCMD.
# The Steam website cannot take a GIF. Double-click publish-workshop-gif.cmd

param(
	[string]$WorkshopId = "3783384562",
	[string]$SteamCmd = "",
	[string]$SteamUser = ""
)

$ErrorActionPreference = "Stop"
if (-not $PSScriptRoot) {
	$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$gif = Join-Path $PSScriptRoot "mod-icon.gif"
$appId = "1281930"
$id = $WorkshopId
if (-not $id) {
	$id = "3783384562"
}

if (!(Test-Path -LiteralPath $gif)) {
	throw "No GIF: $gif"
}

function Find-SteamCmd([string]$hint) {
	if ($hint -and (Test-Path -LiteralPath $hint)) { return (Resolve-Path -LiteralPath $hint).Path }
	$cmd = Get-Command steamcmd -ErrorAction SilentlyContinue
	if ($cmd) { return $cmd.Source }
	foreach ($p in @(
		"$env:LOCALAPPDATA\steamcmd\steamcmd.exe",
		"$env:USERPROFILE\steamcmd\steamcmd.exe",
		"C:\steamcmd\steamcmd.exe",
		"C:\Steam\steamcmd\steamcmd.exe"
	)) {
		if (Test-Path -LiteralPath $p) { return $p }
	}
	return ""
}

function Install-SteamCmd([string]$dir) {
	New-Item -ItemType Directory -Force -Path $dir | Out-Null
	$zip = Join-Path $dir "steamcmd.zip"
	Write-Host "Downloading SteamCMD..."
	Invoke-WebRequest -Uri "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" -OutFile $zip
	Expand-Archive -LiteralPath $zip -DestinationPath $dir -Force
	Remove-Item -LiteralPath $zip -Force
	$exe = Join-Path $dir "steamcmd.exe"
	if (!(Test-Path -LiteralPath $exe)) { throw "SteamCMD download failed." }
	return $exe
}

function Get-SteamAccountName([string]$given) {
	if ($given) { return $given.Trim() }
	$userFile = Join-Path $PSScriptRoot "steam-user.txt"
	if (Test-Path -LiteralPath $userFile) {
		$fromFile = (Get-Content -LiteralPath $userFile -Raw).Trim()
		if ($fromFile) { return $fromFile }
	}
	$steamRoots = @()
	$reg = (Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue).SteamPath
	if ($reg) { $steamRoots += $reg }
	$steamRoots += @("C:\steam", "C:\Program Files (x86)\Steam", "C:\Program Files\Steam")
	foreach ($root in $steamRoots) {
		$vdf = Join-Path $root "config\loginusers.vdf"
		if (!(Test-Path -LiteralPath $vdf)) { continue }
		$text = Get-Content -LiteralPath $vdf -Raw
		$hit = [regex]::Match($text, '"AccountName"\s+"([^"]+)"')
		if ($hit.Success) { return $hit.Groups[1].Value }
	}
	$cfg = Join-Path $env:LOCALAPPDATA "steamcmd\config\config.vdf"
	if (Test-Path -LiteralPath $cfg) {
		$text = Get-Content -LiteralPath $cfg -Raw
		$hit = [regex]::Match($text, '"Accounts"\s*\{\s*"([^"]+)"')
		if ($hit.Success) { return $hit.Groups[1].Value }
	}
	return ""
}

try {
	Set-Content -LiteralPath (Join-Path $PSScriptRoot "workshop-id.txt") -Value $id -NoNewline
	Write-Host "Workshop id: $id"

	$steamCmdPath = Find-SteamCmd $SteamCmd
	if (-not $steamCmdPath) {
		$steamCmdPath = Install-SteamCmd (Join-Path $env:LOCALAPPDATA "steamcmd")
	}
	$steamCmdDir = Split-Path $steamCmdPath -Parent
	Write-Host "SteamCMD: $steamCmdPath"

	$user = Get-SteamAccountName $SteamUser
	if (-not $user) {
		Write-Host ""
		$user = Read-Host "Steam login (not display name)"
	}
	if (-not $user) { throw "Need a Steam login for SteamCMD." }
	Set-Content -LiteralPath (Join-Path $PSScriptRoot "steam-user.txt") -Value $user -NoNewline
	Write-Host "Steam login: $user"

	$preview = Join-Path $steamCmdDir "dwas-preview.gif"
	$vdf = Join-Path $steamCmdDir "dwas-preview.vdf"
	Copy-Item -LiteralPath $gif -Destination $preview -Force
	$previewEsc = $preview.Replace('\', '\\')
	@"
"workshopitem"
{
	"appid" "$appId"
	"publishedfileid" "$id"
	"previewfile" "$previewEsc"
}
"@ | Set-Content -LiteralPath $vdf -Encoding ASCII
	Write-Host "Preview: $preview"

	Write-Host ""
	Write-Host "Uploading GIF preview. If SteamCMD asks for a password or Steam Guard, type it here."
	Write-Host "Close Steam if login fails (same account cannot sit in Steam and SteamCMD at once)."
	Write-Host ""

	Push-Location $steamCmdDir
	try {
		& $steamCmdPath +@ShutdownOnFailedCommand 1 +login $user +workshop_build_item $vdf +quit
		$code = $LASTEXITCODE
	}
	finally {
		Pop-Location
	}

	if ($code -ne 0) {
		throw "SteamCMD failed (exit $code). Log in in this window, or close Steam and run the .cmd again."
	}

	Write-Host ""
	Write-Host "OK. Workshop preview GIF uploaded."
	Start-Process "https://steamcommunity.com/sharedfiles/filedetails/?id=$id"
}
catch {
	Write-Host ""
	Write-Host "ERROR: $($_.Exception.Message)"
	exit 1
}
