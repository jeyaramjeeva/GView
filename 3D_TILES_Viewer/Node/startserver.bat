@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "NODE_HOME=%SCRIPT_DIR%node-v24.14.0-win-x64"

if not exist "%NODE_HOME%\node.exe" (
  echo [ERROR] Node runtime not found at:
  echo         %NODE_HOME%
  pause
  exit /b 1
)

set "PATH=%NODE_HOME%;%PATH%"
echo Starting HTTP server from: %SCRIPT_DIR%
pushd "%SCRIPT_DIR%"
call "%NODE_HOME%\npx.cmd" http-server --cors -g -c-1
popd
endlocal