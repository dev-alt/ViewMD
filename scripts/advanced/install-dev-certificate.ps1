# Requires -Version 5.1
<#
.SYNOPSIS
Installs a developer signing certificate (.cer or from .pfx) into Trusted Root and Trusted People to enable sideloading MSIX locally.

.DESCRIPTION
Fixes installer error 0x800B010A (publisher certificate could not be verified) by trusting the certificate used to sign the MSIX.
Can accept a .cer directly or extract a .cer from a .pfx (public portion only).

.PARAMETER CerPath
Path to a .cer file containing the public certificate. Provide either CerPath or PfxPath.

.PARAMETER PfxPath
Path to a .pfx file. If provided, the script exports a .cer next to the PFX and uses it.

.PARAMETER PfxPassword
SecureString password for the PFX (if needed). If omitted, you’ll be prompted securely.

.PARAMETER Scope
Certificate store scope: LocalMachine (default, requires admin) or CurrentUser.

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/install-dev-certificate.ps1 -CerPath ./certs/ViewMD.cer

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/install-dev-certificate.ps1 -PfxPath ./certs/ViewMD.pfx
#>

[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [Parameter(ParameterSetName='cer', Mandatory=$true)]
    [string]$CerPath,

    [Parameter(ParameterSetName='pfx', Mandatory=$true)]
    [string]$PfxPath,

    [Parameter(ParameterSetName='pfx')]
    [SecureString]$PfxPassword,

    [ValidateSet('LocalMachine','CurrentUser')]
    [string]$Scope = 'LocalMachine'
)

function Write-Info($msg){ Write-Host "[info] $msg" -ForegroundColor Cyan }
function Write-Ok($msg){ Write-Host "[ok] $msg" -ForegroundColor Green }
function Write-Warn($msg){ Write-Warning $msg }
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

    if ($PSCmdlet.ParameterSetName -eq 'pfx') {
        if (-not (Test-Path -LiteralPath $PfxPath)) { throw "PFX not found: $PfxPath" }
        Write-Info "Loading PFX: $PfxPath"
        $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $PfxPath))
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
        if (-not $PfxPassword) {
            Write-Host "Enter PFX password (leave empty if none):" -ForegroundColor Yellow
            $PfxPassword = Read-Host -AsSecureString
        }
        if ($PfxPassword.Length -gt 0) {
            $cert.Import($bytes, $PfxPassword, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::DefaultKeySet)
        } else {
            $cert.Import($bytes)
        }
        $destCer = [System.IO.Path]::ChangeExtension((Resolve-Path -LiteralPath $PfxPath), '.cer')
        Write-Info "Exporting public certificate to: $destCer"
        $cerBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
        [System.IO.File]::WriteAllBytes($destCer, $cerBytes)
        $CerPath = $destCer
    }

    if (-not (Test-Path -LiteralPath $CerPath)) { throw "CER not found: $CerPath" }
    $CerPath = (Resolve-Path -LiteralPath $CerPath).Path

    if ($Scope -eq 'LocalMachine' -and -not (Test-IsAdmin)) {
        throw "Admin privileges required to install into LocalMachine stores. Re-run PowerShell as Administrator or use -Scope CurrentUser."
    }

    Write-Info "Reading certificate: $CerPath"
    $pubCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CerPath)
    $thumb = $pubCert.Thumbprint.ToUpperInvariant()
    $subject = $pubCert.Subject

    $rootStore = "Cert:\$Scope\Root"
    $peopleStore = "Cert:\$Scope\TrustedPeople"

    Write-Info "Installing into: $rootStore"
    Import-Certificate -FilePath $CerPath -CertStoreLocation $rootStore | Out-Null
    Write-Info "Installing into: $peopleStore"
    Import-Certificate -FilePath $CerPath -CertStoreLocation $peopleStore | Out-Null

    # Verify presence
    $foundRoot = Get-ChildItem -Path $rootStore | Where-Object { $_.Thumbprint -eq $thumb }
    $foundPeople = Get-ChildItem -Path $peopleStore | Where-Object { $_.Thumbprint -eq $thumb }
    if ($foundRoot -and $foundPeople) {
        Write-Ok "Certificate trusted for $Scope. Subject: $subject | Thumbprint: $thumb"
        Write-Host "You can now install the MSIX/MSIXBundle signed with this certificate." -ForegroundColor Yellow
    } else {
        throw "Certificate not found in one or both stores after import. Root: $($null -ne $foundRoot) People: $($null -ne $foundPeople)"
    }
}
catch {
    Write-Err $_
    exit 1
}
