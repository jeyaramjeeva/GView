@echo off
title GView
cd /d "%~dp0"

if exist "dist\win-unpacked\GView.exe" (
    start "" "dist\win-unpacked\GView.exe" %*
    exit /b 0
)

if exist "node_modules\electron\dist\electron.exe" (
    start "" "node_modules\electron\dist\electron.exe" . %*
    exit /b 0
)

if not exist "node_modules\electron" (
    echo ======================================================================
    echo                GView - First-Time Launch Setup
    echo ======================================================================
    echo [INFO] Installing required runtime components... Please wait.
    call npm install --no-audit --no-fund
    if exist "node_modules\electron\dist\electron.exe" (
        start "" "node_modules\electron\dist\electron.exe" . %*
        exit /b 0
    )
)

cmd /c npx --yes electron . %*
