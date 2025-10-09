@echo off
REM Update, Build, and Run Script for Markdown Viewer
REM Pulls latest from git, cleans, builds, and runs

echo.
echo ========================================
echo    Markdown Viewer - Update and Run
echo ========================================
echo.

REM Check if git repo exists
if exist ".git" (
    echo Pulling latest changes from git...
    git pull
    if %ERRORLEVEL% EQU 0 (
        echo Git pull successful!
    ) else (
        echo Git pull had issues, continuing anyway...
    )
    echo.
) else (
    echo No git repository found, skipping git pull
    echo.
)

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
