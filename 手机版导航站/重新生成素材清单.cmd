@echo off
cd /d "%~dp0"
chcp 65001 >nul
title Rebuild media list
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0重新生成素材清单.ps1"
echo.
echo Press any key to close this window.
pause >nul
