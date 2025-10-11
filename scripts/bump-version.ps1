# Requires -Version 5.1
<#
.SYNOPSIS
Increments the Version in installer/AppxManifest.xml (the 4th "revision" segment).

.PARAMETER ManifestPath
Path to AppxManifest.xml. Defaults to installer/AppxManifest.xml

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/bump-version.ps1

.EXAMPLE
pwsh -ExecutionPolicy Bypass -File ./scripts/bump-version.ps1 -ManifestPath ./installer/AppxManifest.xml
#>

[CmdletBinding()]
param(
    [string]$ManifestPath = './installer/AppxManifest.xml'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ManifestPath)) {
    throw "Manifest not found: $ManifestPath"
}

[xml]$xml = Get-Content -LiteralPath $ManifestPath
$identity = $xml.Package.Identity
if (-not $identity) {
    throw 'Package/Identity element not found in manifest.'
}

$current = $identity.Version
if (-not $current) { $current = '1.0.0.0' }

try {
    $parts = ($current -split '\.')
    while ($parts.Count -lt 4) { $parts += '0' }
    $rev = 0
    [void][int]::TryParse($parts[3], [ref]$rev)
    $rev++
    $newVersion = "{0}.{1}.{2}.{3}" -f $parts[0], $parts[1], $parts[2], $rev
    $identity.SetAttribute('Version', $newVersion) | Out-Null
    $xml.Save((Resolve-Path -LiteralPath $ManifestPath))
    Write-Host "[bump-version] $current -> $newVersion" -ForegroundColor Cyan
    $newVersion
} catch {
    throw "Failed to bump version: $($_.Exception.Message)"
}
