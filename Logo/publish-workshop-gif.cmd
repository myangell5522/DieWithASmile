@echo off
cd /d "%~dp0"
echo Uploading Workshop GIF via SteamCMD...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-workshop-gif.ps1" %*
echo.
pause
