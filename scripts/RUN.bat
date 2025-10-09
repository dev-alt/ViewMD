@echo off
REM Quick Run Script for Markdown Viewer
REM Double-click this file to clean, build, and run

echo.
echo ========================================
echo    Markdown Viewer - Quick Run
echo ========================================
echo.

echo Cleaning previous builds...
dotnet clean --verbosity quiet

echo Building project...
dotnet build --verbosity quiet

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! Starting application...
    echo.
    dotnet run
) else (
    echo.
    echo Build failed! Showing errors:
    echo.
    dotnet build
    pause
)
