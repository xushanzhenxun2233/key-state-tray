@echo off
rem Build wrapper - runs build.ps1 with execution policy bypass
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if errorlevel 1 echo BUILD FAILED
pause