@echo off
cd /d "%~dp0"
chcp 65001 >nul
title MoYinMin Mobile Server
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0服务.ps1"
echo.
echo Press any key to close this window.
pause >nul
