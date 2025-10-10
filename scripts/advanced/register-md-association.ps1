Param(
    [string]$ExePath = "",
    [switch]$SetDefault
)

if (-not (Test-Path $ExePath)) {
    Write-Error "ExePath not found: $ExePath"
    exit 1
}

$exe = (Resolve-Path $ExePath).Path
$quotedExe = '"' + $exe + '"'
$openCmd = $quotedExe + ' "%1"'

$progId = 'MarkdownViewer.Document'
$hkcu = 'HKCU:\Software\Classes'

New-Item -Path "$hkcu\$progId" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\$progId" -Name '(Default)' -Value 'Markdown Document' -Force
New-Item -Path "$hkcu\$progId\DefaultIcon" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\$progId\DefaultIcon" -Name '(Default)' -Value "$quotedExe,0" -Force
New-Item -Path "$hkcu\$progId\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\$progId\shell\open\command" -Name '(Default)' -Value $openCmd -Force

New-Item -Path "$hkcu\\.md" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\\.md" -Name '(Default)' -Value $progId -Force
Set-ItemProperty -Path "$hkcu\\.md" -Name 'Content Type' -Value 'text/markdown' -Force

New-Item -Path "$hkcu\\.md\\OpenWithProgids" -Force | Out-Null
New-ItemProperty -Path "$hkcu\\.md\\OpenWithProgids" -Name $progId -PropertyType String -Value '' -Force | Out-Null

New-Item -Path "$hkcu\\.txt" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\\.txt" -Name 'Content Type' -Value 'text/plain' -Force
New-Item -Path "$hkcu\\.txt\\OpenWithProgids" -Force | Out-Null
New-ItemProperty -Path "$hkcu\\.txt\\OpenWithProgids" -Name $progId -PropertyType String -Value '' -Force | Out-Null

$appsKey = "$hkcu\Applications"
New-Item -Path "$appsKey\MarkdownViewer.exe" -Force | Out-Null
New-Item -Path "$appsKey\MarkdownViewer.exe\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "$appsKey\MarkdownViewer.exe\shell\open\command" -Name '(Default)' -Value $openCmd -Force

Write-Host "Registered ViewMD for .md/.txt (per-user)."

if ($SetDefault) {
    try { Start-Process "ms-settings:defaultapps"; Write-Host "Open Settings to set 'ViewMD' as default for .md" } catch {}
}
