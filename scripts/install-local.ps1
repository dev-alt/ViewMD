# Requires -Version 5.1
<#
.SYNOPSIS
Trusts the signer cert and installs the ViewMD MSIX bundle locally (sideload).

.DESCRIPTION
Extracts signer from ViewMD.msixbundle and trusts it in LocalMachine stores (requires admin), then launches the bundle installer.

.PARAMETER BundlePath
Path to the .msixbundle. Defaults to ./ViewMD.msixbundle.

.PARAMETER Scope
LocalMachine (default) or CurrentUser for certificate trust.

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/install-local.ps1
#>

[CmdletBinding()]
param(
    [string]$BundlePath = './ViewMD.msixbundle',
    [ValidateSet('LocalMachine','CurrentUser')]
    [string]$Scope = 'LocalMachine'
)

$ErrorActionPreference = 'Stop'

function Test-IsAdmin {
    try {
        $id = [Security.Principal.WindowsIdentity]::GetCurrent()
        $p = New-Object Security.Principal.WindowsPrincipal($id)
        return $p.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
    } catch { return $false }
}

# Auto-elevate if targeting LocalMachine stores but not running as admin
if ($Scope -eq 'LocalMachine' -and -not (Test-IsAdmin)) {
    Write-Host "[install-local] Elevating to Administrator for LocalMachine certificate install..." -ForegroundColor Yellow
    $scriptPath = (Resolve-Path -LiteralPath $MyInvocation.MyCommand.Path).Path
    $args = @('-ExecutionPolicy','Bypass','-File',"$scriptPath",'-BundlePath',"$BundlePath",'-Scope','LocalMachine')
    Start-Process pwsh -Verb RunAs -ArgumentList $args | Out-Null
    exit 0
}

if (-not (Test-Path -LiteralPath $BundlePath)) {
    throw "Bundle not found: $BundlePath"
}

Write-Host "[install-local] Trusting signer certificate..." -ForegroundColor Cyan
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/install-cert-from-package.ps1 -PackagePath $BundlePath -Scope $Scope

Write-Host "[install-local] Launching installer UI..." -ForegroundColor Cyan
Start-Process -FilePath (Resolve-Path -LiteralPath $BundlePath) -Verb Open

if ($Scope -eq 'CurrentUser') {
    Write-Host "[install-local] Note: If the App Installer still shows 0x800B010A, re-run this script without --Scope (defaults to LocalMachine) from an elevated PowerShell." -ForegroundColor DarkYellow
}
