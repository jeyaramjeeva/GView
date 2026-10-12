@echo off
title GView
cd /d "%~dp0"

if exist "GView.exe" (
    start "" "GView.exe" %*
    exit /b 0
)

if exist "dist\win-unpacked\GView.exe" (
    start "" "dist\win-unpacked\GView.exe" %*
    exit /b 0
)

if exist "node_modules\electron\dist\electron.exe" (
    start "" "node_modules\electron\dist\electron.exe" . %*
    exit /b 0
)

cmd /c npx electron . %*
