@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-MLAgentsVenv.ps1"
exit /b %ERRORLEVEL%
