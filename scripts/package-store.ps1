# Requires -Version 5.1
<#
.SYNOPSIS
Produce a Store-ready, signed multi-arch MSIX bundle for ViewMD.

.DESCRIPTION
Builds Release for win-x64 and win-arm64, creates per-arch MSIX packages with architecture set in the manifest, signs them with the provided PFX, then creates and signs a .msixbundle. Outputs files in repo root.

.PARAMETER PfxPath
Path to the PFX used to sign packages (for sideload testing; Store will re-sign).

.PARAMETER PfxPassword
SecureString PFX password (if omitted, you will be prompted securely).

.PARAMETER Version
Optional version override for the AppxManifest before packaging (e.g. 1.0.3.0).

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/package-store.ps1 -PfxPath ./certs/ViewMD.pfx -Version 1.0.0.0
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$PfxPath,
    [SecureString]$PfxPassword,
    [string]$Version
)

$ErrorActionPreference = 'Stop'

function Set-AppxVersionIfProvided {
    param([string]$ManifestPath,[string]$NewVersion)
    if (-not $NewVersion) { return }
    if (-not (Test-Path $ManifestPath)) { return }
    try {
        [xml]$xml = Get-Content -Path $ManifestPath
        if ($xml.Package.Identity) {
            $xml.Package.Identity.SetAttribute('Version', $NewVersion) | Out-Null
            $xml.Save($ManifestPath)
            Write-Host "[package-store] Set manifest version to $NewVersion" -ForegroundColor Green
        }
    } catch {
        Write-Warning "[package-store] Failed to set manifest version: $($_.Exception.Message)"
    }
}

Write-Host "[package-store] Building and signing per-arch packages..." -ForegroundColor Cyan

# Compute plain password if provided securely
if (-not $PfxPassword) {
    Write-Host "Enter PFX password:" -ForegroundColor Yellow
    $PfxPassword = Read-Host -AsSecureString
}

# x64
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/package-msix.ps1 -Configuration Release -Runtime win-x64 -PublishDir publish/win-x64 -MsixPath ViewMD.msix -PfxPath $PfxPath -PfxPassword $PfxPassword
Set-AppxVersionIfProvided -ManifestPath ./package/AppxManifest.xml -NewVersion $Version

# arm64
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/package-msix.ps1 -Configuration Release -Runtime win-arm64 -PublishDir publish/win-arm64 -MsixPath ViewMD-arm64.msix -PfxPath $PfxPath -PfxPassword $PfxPassword
Set-AppxVersionIfProvided -ManifestPath ./package/AppxManifest.xml -NewVersion $Version

Write-Host "[package-store] Creating multi-arch bundle..." -ForegroundColor Cyan
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/package-msix.ps1 -Configuration Release -Runtime win-x64 -PublishDir publish/win-x64 -MsixPath ViewMD.msix -BundlePath ViewMD.msixbundle -BundleInputs "ViewMD.msix,ViewMD-arm64.msix" -PfxPath $PfxPath -PfxPassword $PfxPassword

Write-Host "[package-store] Done." -ForegroundColor Green
Write-Host "Artifacts:" -ForegroundColor Yellow
Write-Host " - ViewMD.msix (x64)"
Write-Host " - ViewMD-arm64.msix"
Write-Host " - ViewMD.msixbundle"
