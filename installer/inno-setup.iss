; Inno Setup script for ViewMD
; Requires Inno Setup (https://jrsoftware.org/isinfo.php)

#define MyAppName "ViewMD"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Dev-Alt"
#define MyAppURL "https://example.com"
#define MyAppExeName "MarkdownViewer.exe"
#define PublishDir "..\publish\win-x64"

[Setup]
AppId={{6B3B8A74-8E62-4B2A-9E3E-6B0D7F59C5A1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={pf}\ViewMD
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=ViewMD-Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: fileassoc; Description: "Associate .md files with {#MyAppName}"; Flags: unchecked
Name: fileassoc_txt; Description: "Enable {#MyAppName} in Open with for .txt"; Flags: unchecked
Name: fileassoc_txt_default; Description: "Associate .txt files with {#MyAppName} (set as default)"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
; File association (ProgID)
Root: HKCR; Subkey: "MarkdownViewer.Document"; ValueType: string; ValueName: ""; ValueData: "Markdown Document"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKCR; Subkey: "MarkdownViewer.Document\\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\\{#MyAppExeName},0"; Tasks: fileassoc
Root: HKCR; Subkey: "MarkdownViewer.Document\\shell\\open\\command"; ValueType: string; ValueName: ""; ValueData: '"{app}\\{#MyAppExeName}" "%1"'; Tasks: fileassoc

; .md association
Root: HKCR; Subkey: ".md"; ValueType: string; ValueName: ""; ValueData: "MarkdownViewer.Document"; Tasks: fileassoc
Root: HKCR; Subkey: ".md"; ValueType: string; ValueName: "Content Type"; ValueData: "text/markdown"; Tasks: fileassoc

; Applications listing for Open with
Root: HKCR; Subkey: "Applications\\{#MyAppExeName}\\shell\\open\\command"; ValueType: string; ValueName: ""; ValueData: '"{app}\\{#MyAppExeName}" "%1"'
; Allow Open with for .txt
Root: HKCR; Subkey: ".txt\\OpenWithProgids"; ValueType: string; ValueName: "MarkdownViewer.Document"; ValueData: ""; Tasks: fileassoc_txt
; Associate .txt as default (optional)
Root: HKCR; Subkey: ".txt"; ValueType: string; ValueName: ""; ValueData: "MarkdownViewer.Document"; Tasks: fileassoc_txt_default
Root: HKCR; Subkey: ".txt"; ValueType: string; ValueName: "Content Type"; ValueData: "text/plain"; Tasks: fileassoc_txt_default
