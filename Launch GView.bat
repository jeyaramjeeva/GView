@echo off
title GView
cd /d "%~dp0"

rem 1. Launch standalone native GView executable directly if present
if exist "GView.exe" (
    start "" "GView.exe" %*
    exit /b 0
)

rem 2. Fallback to local Electron runtime if present
if exist "node_modules\electron\dist\electron.exe" (
    start "" "node_modules\electron\dist\electron.exe" . %*
    exit /b 0
)

rem 3. Fallback to npx electron
cmd /c npx --yes electron . %*
