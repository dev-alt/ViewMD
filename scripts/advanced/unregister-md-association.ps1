$hkcu = 'HKCU:\Software\Classes'
$progId = 'MarkdownViewer.Document'

function Remove-Key($path) { if (Test-Path $path) { Remove-Item -Path $path -Recurse -Force } }

Remove-Key "$hkcu\$progId"
Remove-Key "$hkcu\\.md\\OpenWithProgids"
if (Test-Path "$hkcu\\.md") {
    $cur = (Get-ItemProperty -Path "$hkcu\\.md" -Name '(Default)' -ErrorAction SilentlyContinue)."(Default)"
    if ($cur -eq $progId) { Remove-Key "$hkcu\\.md" }
}
Remove-Key "$hkcu\\.txt\\OpenWithProgids"
$appsKey = "$hkcu\Applications"
Remove-Key "$appsKey\MarkdownViewer.exe"

Write-Host "Unregistered ViewMD per-user association (where applicable)."
