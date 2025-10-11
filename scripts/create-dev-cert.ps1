# Requires -Version 5.1
<#
.SYNOPSIS
Creates a new self-signed code-signing certificate for local packaging, exports to PFX/CER,
optionally trusts it (LocalMachine or CurrentUser), and can update AppxManifest Publisher.

.PARAMETER Subject
Subject/Publisher string, e.g. "CN=Your Name". Defaults to "CN=ViewMD Dev".

.PARAMETER PfxPath
Output PFX path. Defaults to ./certs/ViewMD.pfx

.PARAMETER CerPath
Output CER path. Defaults to ./certs/ViewMD.cer

.PARAMETER PfxPasswordText
Password to protect the PFX export. REQUIRED for export.

.PARAMETER Scope
Where to trust the certificate: LocalMachine (default) or CurrentUser.

.PARAMETER UpdateManifest
If set, updates installer/AppxManifest.xml Identity@Publisher to match the Subject.

.PARAMETER ManifestPath
Path to AppxManifest.xml. Defaults to ./installer/AppxManifest.xml

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/create-dev-cert.ps1 -Subject "CN=Your Name" -PfxPasswordText "secret" -UpdateManifest

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/create-dev-cert.ps1 -PfxPasswordText "secret" -Scope LocalMachine
#>

[CmdletBinding()]
param(
    [string]$Subject = 'CN=ViewMD Dev',
    [string]$PfxPath = './certs/ViewMD.pfx',
    [string]$CerPath = './certs/ViewMD.cer',
    [SecureString]$PfxPassword,
    [string]$PfxPasswordText,
    [ValidateSet('LocalMachine','CurrentUser')]
    [string]$Scope = 'LocalMachine',
    [switch]$UpdateManifest,
    [string]$ManifestPath = './installer/AppxManifest.xml'
)

$ErrorActionPreference = 'Stop'

function Test-IsAdmin {
    try {
        $id = [Security.Principal.WindowsIdentity]::GetCurrent()
        $p = New-Object Security.Principal.WindowsPrincipal($id)
        return $p.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
    } catch { return $false }
}

# Ensure output folder exists
$certDir = Split-Path -Parent $PfxPath
if (-not $certDir -or $certDir -eq '') { $certDir = 'certs' }
if (-not (Test-Path -LiteralPath $certDir)) { New-Item -ItemType Directory -Path $certDir | Out-Null }

Write-Host "[create-dev-cert] Creating self-signed code-signing certificate: $Subject" -ForegroundColor Cyan

# Create a self-signed cert in CurrentUser\My with Code Signing EKU
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -CertStoreLocation Cert:\CurrentUser\My `
    -FriendlyName 'ViewMD Dev Cert' `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyExportPolicy Exportable `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3')  # EKU Code Signing

if (-not $cert) { throw 'Failed to create self-signed certificate.' }

if (-not $PfxPassword -and $PfxPasswordText) {
    $PfxPassword = ConvertTo-SecureString $PfxPasswordText -AsPlainText -Force
}
if (-not $PfxPassword) { throw 'PfxPassword or PfxPasswordText is required.' }

$pfxFull = [System.IO.Path]::GetFullPath((Join-Path $certDir (Split-Path -Leaf $PfxPath)))
$cerFull = [System.IO.Path]::GetFullPath((Join-Path $certDir (Split-Path -Leaf $CerPath)))

Write-Host "[create-dev-cert] Exporting PFX -> $pfxFull" -ForegroundColor Cyan
Export-PfxCertificate -Cert $cert -FilePath $pfxFull -Password $PfxPassword | Out-Null

Write-Host "[create-dev-cert] Exporting CER -> $cerFull" -ForegroundColor Cyan
Export-Certificate -Cert $cert -FilePath $cerFull | Out-Null

# Trust the cert
if ($Scope -eq 'LocalMachine' -and -not (Test-IsAdmin)) {
    Write-Host "[create-dev-cert] Elevation required to trust in LocalMachine stores. Relaunching..." -ForegroundColor Yellow
    $scriptPath = (Resolve-Path -LiteralPath $MyInvocation.MyCommand.Path).Path
    $procArgs = @('-ExecutionPolicy','Bypass','-File',"$scriptPath",'-Subject',"$Subject",'-PfxPath',"$pfxFull",'-CerPath',"$cerFull",'-PfxPasswordText')
    if ($PfxPasswordText) {
        $procArgs += "$PfxPasswordText"
    } else {
        # Convert secure string to plain for relay; acceptable since we are relaunching locally
        $plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($PfxPassword))
        $procArgs += $plain
    }
    $procArgs += @('-Scope','LocalMachine')
    if ($UpdateManifest) { $procArgs += @('-UpdateManifest','-ManifestPath',"$ManifestPath") }
    Start-Process pwsh -Verb RunAs -ArgumentList $procArgs | Out-Null
    exit 0
}

Write-Host "[create-dev-cert] Trusting certificate in $Scope stores..." -ForegroundColor Cyan
if ($Scope -eq 'LocalMachine') {
    Import-Certificate -FilePath $cerFull -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
    Import-Certificate -FilePath $cerFull -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
} else {
    Import-Certificate -FilePath $cerFull -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
    Import-Certificate -FilePath $cerFull -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
}

# Optionally update manifest Publisher
if ($UpdateManifest) {
    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        throw "Manifest not found: $ManifestPath"
    }
    [xml]$xml = Get-Content -LiteralPath $ManifestPath
    if (-not $xml.Package.Identity) { throw 'Package/Identity element not found in manifest.' }
    $old = $xml.Package.Identity.Publisher
    $xml.Package.Identity.SetAttribute('Publisher', $Subject) | Out-Null
    $xml.Save((Resolve-Path -LiteralPath $ManifestPath))
    Write-Host "[create-dev-cert] Updated manifest Publisher: '$old' -> '$Subject'" -ForegroundColor Cyan
}

Write-Host "[create-dev-cert] Done." -ForegroundColor Green
