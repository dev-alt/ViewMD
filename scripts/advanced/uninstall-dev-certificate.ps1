# Requires -Version 5.1
<#
.SYNOPSIS
Removes the developer certificate from Trusted Root and Trusted People stores.
#>

[CmdletBinding()]
param(
    [Parameter(ParameterSetName='thumb', Mandatory=$true)]
    [string]$Thumbprint,

    [Parameter(ParameterSetName='cer', Mandatory=$true)]
    [string]$CerPath,

    [Parameter(ParameterSetName='pfx', Mandatory=$true)]
    [string]$PfxPath,

    [ValidateSet('LocalMachine','CurrentUser')]
    [string]$Scope = 'LocalMachine'
)

function Test-IsAdmin { try { $id=[Security.Principal.WindowsIdentity]::GetCurrent(); $p=New-Object Security.Principal.WindowsPrincipal($id); return $p.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator) } catch { return $false } }

try {
    $ErrorActionPreference = 'Stop'
    if ($PSCmdlet.ParameterSetName -eq 'cer') { if (-not (Test-Path -LiteralPath $CerPath)) { throw "CER not found: $CerPath" }; $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2((Resolve-Path -LiteralPath $CerPath)); $Thumbprint = $cert.Thumbprint }
    elseif ($PSCmdlet.ParameterSetName -eq 'pfx') { if (-not (Test-Path -LiteralPath $PfxPath)) { throw "PFX not found: $PfxPath" }; $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $PfxPath)); $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2; $cert.Import($bytes); $Thumbprint = $cert.Thumbprint }

    if ($Scope -eq 'LocalMachine' -and -not (Test-IsAdmin)) { throw "Admin privileges required to remove from LocalMachine stores. Re-run PowerShell as Administrator or use -Scope CurrentUser." }

    $Thumbprint = $Thumbprint.ToUpperInvariant().Replace(" ","")
    $stores = @(("Cert:\{0}\Root" -f $Scope),("Cert:\{0}\TrustedPeople" -f $Scope))
    foreach ($store in $stores) {
        $toRemove = Get-ChildItem -Path $store | Where-Object { $_.Thumbprint -eq $Thumbprint }
        if ($toRemove) { foreach ($c in $toRemove) { Remove-Item -Path ("{0}\{1}" -f $store, $c.Thumbprint) -Force } }
    }
}
catch { Write-Host $_ -ForegroundColor Red; exit 1 }
