# Removes per-user registration for MarkdownViewer as an .md handler.

$hkcu = 'HKCU:\Software\Classes'
$progId = 'MarkdownViewer.Document'

function Remove-Key($path) {
    if (Test-Path $path) { Remove-Item -Path $path -Recurse -Force }
}

Remove-Key "$hkcu\$progId"
Remove-Key "$hkcu\\.md\\OpenWithProgids"
# Only remove HKCU\.md if it points to our ProgID to avoid breaking other apps
if (Test-Path "$hkcu\\.md") {
    $cur = (Get-ItemProperty -Path "$hkcu\\.md" -Name '(Default)' -ErrorAction SilentlyContinue)."(Default)"
    if ($cur -eq $progId) {
        Remove-Key "$hkcu\\.md"
    }
}

$appsKey = "$hkcu\Applications"
Remove-Key "$appsKey\MarkdownViewer.exe"

Write-Host "Unregistered MarkdownViewer per-user association (where applicable)."