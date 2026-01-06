@echo off
REM Start DocFX documentation server
REM Double-click this file to launch the documentation server

echo Starting DocFX documentation server...
echo.
echo Navigate to: http://localhost:8080
echo Press Ctrl+C to stop the server.
echo.

cd /d "%~dp0..\docfx_project"
if not exist "docfx.json" (
    echo ERROR: docfx_project not found or not initialized
    echo Run 'misc_utils\init-docfx.ps1' from the solution root first.
    pause
    exit /b 1
)

docfx docfx.json --serve
pause
