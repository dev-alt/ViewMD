@echo off
setlocal enabledelayedexpansion

REM Change to script directory
cd /d "%~dp0"

echo [step] Pulling latest from git...
git rev-parse --is-inside-work-tree >NUL 2>&1
if errorlevel 1 (
  echo Not a git repo; skipping pull.
) else (
  git pull --ff-only || goto :error
)

echo [step] Bumping AppxManifest version...
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\bump-version.ps1 || goto :error

REM Configure build parameters
set CONFIG=Release
set PFX_PATH=certs\ViewMD.pfx
set PFX_PASSWORD=NEW-STRONG-PASSWORD

REM Detect if PFX exists; if so we will sign
if exist "%PFX_PATH%" (
  set SIGN_ARGS=-PfxPath "%PFX_PATH%"
  if defined PFX_PASSWORD set SIGN_ARGS=%SIGN_ARGS% -PfxPasswordText "%PFX_PASSWORD%"
) else (
  set SIGN_ARGS=
  echo [warn] PFX not found; packages will be unsigned.
)

echo [step] Building MSIX (x64)...
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-msix.ps1 -Configuration %CONFIG% -Runtime win-x64 %SIGN_ARGS% -MsixPath ViewMD-x64.msix || goto :error

echo [step] Building MSIX (arm64)...
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-msix.ps1 -Configuration %CONFIG% -Runtime win-arm64 %SIGN_ARGS% -MsixPath ViewMD-arm64.msix || goto :error

echo [step] Creating bundle...
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-msix.ps1 -Configuration %CONFIG% -Runtime win-x64 %SIGN_ARGS% -BundlePath ViewMD.msixbundle -BundleInputs ViewMD-x64.msix,ViewMD-arm64.msix || goto :error

echo [step] Installing (trusts cert and launches installer)...
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install-local.ps1 -BundlePath .\ViewMD.msixbundle || goto :error

echo Done.
exit /b 0

:error
echo.
echo [error] The update/installation process failed (exit code %errorlevel%).
echo        Review the output above. Common fixes:
echo        - Install Windows 10/11 SDK (MakeAppx, signtool)
echo        - Provide a valid PFX in certs\ViewMD.pfx and set PFX_PASSWORD in this .bat
echo        - Ensure AppxManifest version increases and Publisher matches existing install
exit /b 1
