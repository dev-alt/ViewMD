$assets = @(
    'Assets/StoreLogo.png',
    'Assets/Square44x44Logo.png',
    'Assets/Square71x71Logo.png',
    'Assets/Square150x150Logo.png',
    'Assets/Square310x310Logo.png',
    'Assets/Wide310x150Logo.png'
)

$missing = @()
foreach ($a in $assets) {
    if (-not (Test-Path $a)) { $missing += $a }
}

if ($missing.Count -gt 0) {
    Write-Error "Missing required MSIX asset(s):`n - " + ($missing -join "`n - ")
    exit 1
}
else {
    Write-Host "All required MSIX assets present."
}
