Param(
    [string]$ExePath = "",
    [switch]$SetDefault
)

# Registers MarkdownViewer for .md files in HKCU so it shows in "Open with" and can be set as default.
# Usage:
#   .\scripts\register-md-association.ps1 -ExePath "C:\\Program Files\\MarkdownViewer\\MarkdownViewer.exe" -SetDefault

if (-not (Test-Path $ExePath)) {
    Write-Error "ExePath not found: $ExePath"
    exit 1
}

$exe = (Resolve-Path $ExePath).Path
$quotedExe = '"' + $exe + '"'
 $openCmd = $quotedExe + ' "%1"'

# ProgID
$progId = 'MarkdownViewer.Document'

# HKCU base
$hkcu = 'HKCU:\Software\Classes'

# 1) Create ProgID
New-Item -Path "$hkcu\$progId" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\$progId" -Name '(Default)' -Value 'Markdown Document' -Force
New-Item -Path "$hkcu\$progId\DefaultIcon" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\$progId\DefaultIcon" -Name '(Default)' -Value "$quotedExe,0" -Force
New-Item -Path "$hkcu\$progId\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\$progId\shell\open\command" -Name '(Default)' -Value $openCmd -Force

# 2) Associate .md extension to recognize markdown and map to ProgID (user-level)
New-Item -Path "$hkcu\\.md" -Force | Out-Null
Set-ItemProperty -Path "$hkcu\\.md" -Name '(Default)' -Value $progId -Force
Set-ItemProperty -Path "$hkcu\\.md" -Name 'Content Type' -Value 'text/markdown' -Force

# 3) Add to OpenWith lists
New-Item -Path "$hkcu\\.md\\OpenWithProgids" -Force | Out-Null
New-ItemProperty -Path "$hkcu\\.md\\OpenWithProgids" -Name $progId -PropertyType String -Value '' -Force | Out-Null

# 4) Register under Applications for Open with
$appsKey = "$hkcu\Applications"
New-Item -Path "$appsKey\MarkdownViewer.exe" -Force | Out-Null
New-Item -Path "$appsKey\MarkdownViewer.exe\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "$appsKey\MarkdownViewer.exe\shell\open\command" -Name '(Default)' -Value $openCmd -Force

Write-Host "Registered MarkdownViewer for .md files (per-user)."

if ($SetDefault) {
    try {
        # Windows manages default apps with hashes; programmatically setting this is restricted.
        # We can launch the Settings page to let user set the default cleanly.
        Start-Process "ms-settings:defaultapps"
        Write-Host "Please set 'Markdown Viewer' as the default for .md in Windows Settings > Default apps."
    }
    catch { }
}
