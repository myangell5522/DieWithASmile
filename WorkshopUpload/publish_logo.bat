@echo off
setlocal EnableExtensions
title Update Workshop preview - Die With A Smile
cd /d "%~dp0"

rem ---------------------------------------------------------------------------
rem Uploads animated-logo-480.gif as the main preview image of the mod's Steam
rem Workshop page. Only the preview image changes: no "contentfolder" is sent,
rem so the mod files, title, description and tags on the page stay as they are.
rem tModLoader's Steam app id is 1281930.
rem ---------------------------------------------------------------------------

set "GIF=%~dp0animated-logo-480.gif"
set "VDF=%~dp0logo_upload.vdf"
set "STEAMCMD=%~dp0steamcmd\steamcmd.exe"

if not exist "%GIF%" (
	echo Could not find animated-logo-480.gif next to this script.
	pause
	exit /b 1
)

for %%A in ("%GIF%") do set "SIZE=%%~zA"
if %SIZE% GTR 1048576 (
	echo The GIF is larger than 1 MB; Steam rejects preview images over 1 MB.
	pause
	exit /b 1
)

rem --- steamcmd: use an existing copy, or download the official one -----------
if not exist "%STEAMCMD%" (
	echo steamcmd not found. Downloading the official build from Valve...
	mkdir "%~dp0steamcmd" 2>nul
	powershell -NoProfile -ExecutionPolicy Bypass -Command ^
		"Invoke-WebRequest -Uri 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip' -OutFile '%~dp0steamcmd\steamcmd.zip'; Expand-Archive -Force '%~dp0steamcmd\steamcmd.zip' '%~dp0steamcmd'; Remove-Item '%~dp0steamcmd\steamcmd.zip'"
	if not exist "%STEAMCMD%" (
		echo Download failed. Get steamcmd from https://developer.valvesoftware.com/wiki/SteamCMD
		echo and put steamcmd.exe in "%~dp0steamcmd\".
		pause
		exit /b 1
	)
)

rem --- which Workshop item -----------------------------------------------------
echo.
echo Open your mod's Workshop page in Steam. The number after "?id=" in its
echo address is the item ID, e.g. https://steamcommunity.com/sharedfiles/filedetails/?id=1234567890
echo.
set /p "ITEMID=Workshop item ID: "
if "%ITEMID%"=="" (
	echo No ID entered.
	pause
	exit /b 1
)

set /p "STEAMUSER=Steam account name (the one that owns the mod): "
if "%STEAMUSER%"=="" (
	echo No account name entered.
	pause
	exit /b 1
)

rem --- build the upload description (paths need forward slashes or doubled backslashes)
set "GIFPATH=%GIF:\=/%"
> "%VDF%" (
	echo "workshopitem"
	echo {
	echo 	"appid"		"1281930"
	echo 	"publishedfileid"	"%ITEMID%"
	echo 	"previewfile"	"%GIFPATH%"
	echo 	"changenote"	"Animated logo preview"
	echo }
)

echo.
echo steamcmd will now ask for your password and Steam Guard code itself.
echo They are typed into Valve's own tool and are not stored by this script.
echo.
"%STEAMCMD%" +login %STEAMUSER% +workshop_build_item "%VDF%" +quit
set "RESULT=%ERRORLEVEL%"

del "%VDF%" 2>nul
echo.
if "%RESULT%"=="0" (
	echo Done. Refresh the Workshop page; Steam can take a minute to show the new preview.
) else (
	echo steamcmd reported an error ^(code %RESULT%^). Scroll up for the message.
)
pause
endlocal
