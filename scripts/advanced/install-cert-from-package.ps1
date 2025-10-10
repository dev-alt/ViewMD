# Requires -Version 5.1
<#
.SYNOPSIS
Installs the signer certificate from a given MSIX/MSIXBundle into Trusted Root and Trusted People stores.

.DESCRIPTION
Fixes 0x800B010A (publisher certificate not verified) by extracting the signer certificate from the package signature and trusting it.

.PARAMETER PackagePath
Path to the .msix or .msixbundle file.

.PARAMETER Scope
Certificate store scope: LocalMachine (default, requires admin) or CurrentUser.

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/install-cert-from-package.ps1 -PackagePath ./ViewMD.msixbundle
#>

[CmdletBinding()] 
param(
    [Parameter(Mandatory=$true)]
    [string]$PackagePath,

    [ValidateSet('LocalMachine','CurrentUser')]
    [string]$Scope = 'LocalMachine'
)

function Write-Info($msg){ Write-Host "[info] $msg" -ForegroundColor Cyan }
function Write-Ok($msg){ Write-Host "[ok] $msg" -ForegroundColor Green }
function Write-Err($msg){ Write-Host "[error] $msg" -ForegroundColor Red }

function Test-IsAdmin {
    try {
        $id = [Security.Principal.WindowsIdentity]::GetCurrent()
        $p = New-Object Security.Principal.WindowsPrincipal($id)
        return $p.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
    } catch { return $false }
}

try {
    $ErrorActionPreference = 'Stop'

    if (-not (Test-Path -LiteralPath $PackagePath)) { throw "Package not found: $PackagePath" }
    $PackagePath = (Resolve-Path -LiteralPath $PackagePath).Path

    if ($Scope -eq 'LocalMachine' -and -not (Test-IsAdmin)) {
        throw "Admin privileges required to install into LocalMachine stores. Re-run PowerShell as Administrator or use -Scope CurrentUser."
    }

    Write-Info "Reading signature from: $PackagePath"
    $sig = Get-AuthenticodeSignature -FilePath $PackagePath
    if ($sig.Status -eq 'NotSigned' -or -not $sig.SignerCertificate) {
        throw "Package is not signed or signer certificate not found."
    }

    $cert = $sig.SignerCertificate
    $thumb = $cert.Thumbprint.ToUpperInvariant()
    $subject = $cert.Subject

    $tempCer = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($PackagePath), "_temp_$thumb.cer")
    Write-Info "Exporting signer certificate to: $tempCer"
    $cerBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
    [System.IO.File]::WriteAllBytes($tempCer, $cerBytes)

    $rootStore = "Cert:\$Scope\Root"
    $peopleStore = "Cert:\$Scope\TrustedPeople"

    Write-Info "Installing into: $rootStore"
    Import-Certificate -FilePath $tempCer -CertStoreLocation $rootStore | Out-Null
    Write-Info "Installing into: $peopleStore"
    Import-Certificate -FilePath $tempCer -CertStoreLocation $peopleStore | Out-Null

    Remove-Item -LiteralPath $tempCer -ErrorAction SilentlyContinue

    Write-Ok "Certificate trusted for $Scope. Subject: $subject | Thumbprint: $thumb"
    Write-Host "You can now install the MSIX/MSIXBundle." -ForegroundColor Yellow
}
catch {
    Write-Err $_
    exit 1
}
