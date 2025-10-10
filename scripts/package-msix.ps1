Param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$PublishDir = "publish\\win-x64",
    [string]$PackageDir = "package",
    [string]$MsixPath = "MarkdownViewer.msix",
    [string]$PfxPath = "signing-test.pfx",
    [SecureString]$PfxPassword,
    [string]$PfxPasswordText,
    [string]$BundlePath,
    [string[]]$BundleInputs
)

$ErrorActionPreference = 'Stop'

function Find-Tool {
    param(
        [Parameter(Mandatory=$true)][string]$ToolName
    )
    # First try PATH
    $cmd = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Path }

    $candidates = @()
    $kits = @()
    if ($env:ProgramFiles) { $kits += Join-Path $env:ProgramFiles 'Windows Kits\10\bin' }
    if (${env:ProgramFiles(x86)}) { $kits += Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin' }

    foreach ($root in $kits) {
        if (Test-Path $root) {
            # Collect versioned subfolders (e.g., 10.0.22621.0)
            Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                $candidates += Join-Path $_.FullName 'x64'
                $candidates += Join-Path $_.FullName 'x86'
                $candidates += Join-Path $_.FullName 'arm64'
            }
        }
    }

    foreach ($dir in $candidates | Sort-Object -Descending) {
        $path = Join-Path $dir "$ToolName.exe"
        if (Test-Path $path) { return $path }
    }
    return $null
}

# 1) Generate stub icons if missing
pwsh -ExecutionPolicy Bypass -File .\scripts\create-stub-icons.ps1 | Out-Null

# 2) Check assets
pwsh -ExecutionPolicy Bypass -File .\scripts\check-msix-assets.ps1

# 3) Publish app
& dotnet publish -c $Configuration -r $Runtime --self-contained -o $PublishDir MarkdownViewer.csproj

# 4) Prepare package dir
if (Test-Path $PackageDir) { Remove-Item -Recurse -Force $PackageDir }
New-Item -ItemType Directory -Path $PackageDir | Out-Null
Copy-Item -Recurse -Force $PublishDir\* $PackageDir
Copy-Item -Recurse -Force installer\AppxManifest.xml $PackageDir
Copy-Item -Recurse -Force Assets $PackageDir

# Inject ProcessorArchitecture into manifest based on runtime (needed for valid bundles)
$manifestPath = Join-Path $PackageDir 'AppxManifest.xml'
if (Test-Path $manifestPath) {
    try {
        [xml]$xml = Get-Content -Path $manifestPath
        $arch = 'neutral'
        if ($Runtime -match 'x64') { $arch = 'x64' }
        elseif ($Runtime -match 'arm64') { $arch = 'arm64' }
        elseif ($Runtime -match 'x86') { $arch = 'x86' }
        if ($xml.Package.Identity) {
            $xml.Package.Identity.SetAttribute('ProcessorArchitecture', $arch) | Out-Null
            $xml.Save($manifestPath)
        }
    } catch {
        Write-Warning "Failed to set ProcessorArchitecture in manifest: $($_.Exception.Message)"
    }
}

# 5) Build MSIX (auto-locate MakeAppx)
$makeAppx = Find-Tool -ToolName 'MakeAppx'
if (-not $makeAppx) {
    throw "MakeAppx.exe not found. Install the Windows 10/11 SDK or add MakeAppx to PATH."
}
if (Test-Path $MsixPath) { Remove-Item $MsixPath -Force }
& $makeAppx pack /d $PackageDir /p $MsixPath

# 6) Sign package (optional, auto-locate SignTool)
if ($PfxPath -and (Test-Path $PfxPath)) {
    if (-not $PfxPassword -and $PfxPasswordText) {
        $PfxPassword = ConvertTo-SecureString $PfxPasswordText -AsPlainText -Force
    }
    $signTool = Find-Tool -ToolName 'signtool'
    if (-not $signTool) {
        Write-Warning "SignTool.exe not found. Install the Windows 10/11 SDK or add SignTool to PATH. Skipping signing."
    }
    $plain = $null
    if ($PfxPassword) {
        $plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($PfxPassword))
    }
    if ($signTool) {
        if ($plain) {
            & $signTool sign /fd SHA256 /a /f $PfxPath /p $plain $MsixPath
        } else {
            & $signTool sign /fd SHA256 /a /f $PfxPath $MsixPath
        }
        Write-Host "Signed $MsixPath with $PfxPath"
    }
} else {
    Write-Warning "No PFX provided; package is unsigned. Provide -PfxPath and -PfxPassword to sign."
}

Write-Host "MSIX ready: $MsixPath"

# 7) Optionally create a bundle from provided MSIX inputs
if ($BundlePath -and $BundleInputs -and $BundleInputs.Length -gt 0) {
    $makeAppx = Find-Tool -ToolName 'MakeAppx'
    if (-not $makeAppx) {
        Write-Warning "MakeAppx.exe not found; cannot create bundle."
    } else {
        # Normalize bundle inputs: allow single comma-separated string
        if ($BundleInputs.Length -eq 1 -and ($BundleInputs[0] -match ',')) {
            $BundleInputs = @($BundleInputs[0].Split(',') | ForEach-Object { $_.Trim() })
        }
        $bundleDir = Join-Path $PWD 'bundle_temp'
        if (Test-Path $bundleDir) { Remove-Item -Recurse -Force $bundleDir }
        New-Item -ItemType Directory -Path $bundleDir | Out-Null
        foreach ($pkg in $BundleInputs) {
            if (-not (Test-Path $pkg)) { throw "Bundle input not found: $pkg" }
            Copy-Item -Force $pkg $bundleDir
        }
        if (Test-Path $BundlePath) { Remove-Item -Force $BundlePath }
        & $makeAppx bundle /d $bundleDir /p $BundlePath

        # Sign the bundle if PFX available
        if ($PfxPath -and (Test-Path $PfxPath)) {
            $signTool = Find-Tool -ToolName 'signtool'
            if (-not $signTool) {
                Write-Warning "SignTool.exe not found. Bundle created but unsigned."
            } else {
                $plain = $null
                if ($PfxPassword) { $plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($PfxPassword)) }
                if ($plain) {
                    & $signTool sign /fd SHA256 /a /f $PfxPath /p $plain $BundlePath
                } else {
                    & $signTool sign /fd SHA256 /a /f $PfxPath $BundlePath
                }
                Write-Host "Signed bundle $BundlePath with $PfxPath"
            }
        } else {
            Write-Warning "Bundle created but unsigned (no PFX provided)."
        }
        # Cleanup temp dir
        Remove-Item -Recurse -Force $bundleDir
        Write-Host "MSIX bundle ready: $BundlePath"
    }
}
