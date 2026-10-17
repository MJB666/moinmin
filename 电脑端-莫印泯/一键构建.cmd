@echo off
cd /d "%~dp0"
echo.
echo ==========================================
echo   Building MoYinMin ...
echo ==========================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
echo.
echo Press any key to close this window.
pause >nul
