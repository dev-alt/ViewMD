# Requires -Version 5.1
<#
.SYNOPSIS
Builds and runs ViewMD for local debugging.

.DESCRIPTION
Runs dotnet restore/build and launches the app with optional arguments.

.PARAMETER Configuration
Build configuration (Debug | Release). Default Debug.

.PARAMETER AppArgs
Arguments to pass through to the app (e.g., file paths).

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/dev-run.ps1 -- "C:\Docs\README.md"
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug',
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$AppArgs
)

$ErrorActionPreference = 'Stop'

Write-Host "[dev-run] Restoring packages..." -ForegroundColor Cyan
& dotnet restore MarkdownViewer.csproj | Out-Null

Write-Host "[dev-run] Building ($Configuration)..." -ForegroundColor Cyan
& dotnet build -c $Configuration MarkdownViewer.csproj

Write-Host "[dev-run] Launching app..." -ForegroundColor Cyan
if ($AppArgs -and $AppArgs.Length -gt 0) {
    & dotnet run -c $Configuration --project MarkdownViewer.csproj -- @AppArgs
} else {
    & dotnet run -c $Configuration --project MarkdownViewer.csproj
}
