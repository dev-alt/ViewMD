<#
.SYNOPSIS
    Combined helper to sync from WSL, pull from git, build and run the Markdown Viewer on Windows.

.DESCRIPTION
    Merges the behavior of `sync-from-wsl.ps1`, `run-windows.ps1` and `build-and-run.ps1`.
    - SyncFromWsl: copy files from WSL path to Windows workspace (excludes bin/obj/.git etc.)
    - SkipGit: don't run `git pull`
    - NoRun: build but do not run the application
    - Verbose output and safe error handling

.PARAMETER SyncFromWsl
    When provided, attempts to copy files from a WSL path into the Windows workspace.

.PARAMETER WslSource
    Override the default WSL source path. Default: "\\wsl$\\Ubuntu\\root\\projects\\Markdown_viewer"

.PARAMETER WindowsTarget
    Override the target directory on Windows. Default: the script's repository root.

.PARAMETER SkipGit
    Skip `git pull` even if the current directory is a git repo.

.PARAMETER NoRun
    Build but do not run the application.

.PARAMETER WhatIf
    Do a dry-run (only shows planned actions).

.EXAMPLE
    .\run-all.ps1 -SyncFromWsl -SkipGit
#>

[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [switch]$SyncFromWsl,
    [string]$WslSource = "\\wsl$\\Ubuntu\\root\\projects\\Markdown_viewer",
    [string]$WindowsTarget = "C:\\Users\\andre\\Desktop\\Projects\\Markdown_viewer",
    [switch]$SkipGit,
    [switch]$NoRun
)

function Write-Info($msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host $msg -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host $msg -ForegroundColor Red }
function Write-Success($msg) { Write-Host $msg -ForegroundColor Green }

# Move to script directory (assume repo root is script's parent)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
# Change to repository root (parent of scripts folder)
Set-Location (Split-Path -Parent $scriptDir)

# Helper: perform git pull if repo
if (-not $SkipGit) {
    if (Test-Path ".git") {
        if ($PSCmdlet.ShouldProcess("git pull", "Pull latest changes")) {
            Write-Warn "Pulling latest changes from git..."
            try {
                git pull
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Git pull successful"
                } else {
                    Write-Warn "Git pull had issues, continuing anyway..."
                }
            } catch {
                Write-Warn "Git pull failed: $_"
                Write-Warn "Continuing without git pull..."
            }
            Write-Host ""
        }
    } else {
        Write-Warn "No git repository found, skipping git pull"
        Write-Host ""
    }
} else {
    Write-Warn "Skipping git pull because -SkipGit was provided"
}

# Optional: Sync from WSL
if ($SyncFromWsl) {
    if ($PSCmdlet.ShouldProcess("Sync from WSL", "Copy files from $WslSource to $WindowsTarget")) {
        Write-Info "🔄 Syncing from WSL..."

        if (-not (Test-Path $WslSource)) {
            Write-Err "❌ Error: Cannot access WSL path: $WslSource"
            Write-Err "Make sure WSL is running and the path is correct."
            exit 1
        }

        if (-not (Test-Path $WindowsTarget)) {
            Write-Info "Creating target directory: $WindowsTarget"
            New-Item -ItemType Directory -Path $WindowsTarget -Force | Out-Null
        }

    $excludeDirs = @('bin', 'obj', '.vs', '.vscode', '.git')

        Write-Info "Copying files (excluding build artifacts)..."

        Get-ChildItem -Path $WslSource -Recurse -Force | ForEach-Object {
            $relativePath = $_.FullName.Substring($WslSource.Length)
            $targetPath = Join-Path $WindowsTarget $relativePath

            $shouldExclude = $false
            foreach ($excludeDir in $excludeDirs) {
                if ($relativePath -like "*\\$excludeDir\\*" -or $relativePath -like "*/$excludeDir/*") {
                    $shouldExclude = $true
                    break
                }
            }

            if (-not $shouldExclude) {
                if ($_.PSIsContainer) {
                    if (-not (Test-Path $targetPath)) {
                        New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
                    }
                } else {
                    Copy-Item -Path $_.FullName -Destination $targetPath -Force
                }
            }
        }

        Write-Success "✅ Sync complete!"
        Write-Host ""
        Write-Info "Files synced to: $WindowsTarget"
        Write-Host ""
    }
}

# Clean previous builds
if ($PSCmdlet.ShouldProcess("dotnet clean", "Clean previous builds")) {
    Write-Warn "Cleaning previous builds..."
    dotnet clean --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Clean successful"
    } else {
        Write-Err "Clean failed"
        exit 1
    }
    Write-Host ""
}

# Build the project
if ($PSCmdlet.ShouldProcess("dotnet build", "Build project")) {
    Write-Warn "Building project..."
    dotnet build --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build successful"
    } else {
        Write-Err "Build failed"
        Write-Host ""
        Write-Warn "Running build with errors shown:" 
        dotnet build
        exit 1
    }
    Write-Host ""
}

# Run the application unless NoRun is set
if (-not $NoRun) {
    if ($PSCmdlet.ShouldProcess("dotnet run", "Run application")) {
        Write-Success "Starting Markdown Viewer..."
        Write-Host ""
        dotnet run
    }
} else {
    Write-Info "Build completed. Skipping run because -NoRun was provided."
}
