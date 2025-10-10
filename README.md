# ViewMD

A lightweight, feature-rich Markdown reader and editor built with Avalonia UI and .NET 8.

## Features

### Markdown Support
- **CommonMark + GitHub Flavored Markdown (GFM)**
- All basic syntax: headings, bold, italic, strikethrough
- Lists (ordered, unordered, nested)
- Links and images
- Code blocks with syntax highlighting
- Tables with alignment
- Task lists (checkboxes)
- Blockquotes
- Horizontal rules
- Footnotes
- Definition lists
- Abbreviations
- Emoji support (:smile: :heart:)
- Math expressions (LaTeX/KaTeX)
- Mermaid diagrams (flowcharts, sequence, class, etc.)
- HTML embedding

### Editor Features
- **Syntax-highlighted editor** using AvaloniaEdit with TextMate
- **Live preview** with debounced rendering (300ms)
- **Split-pane layout** with resizable splitter
- Line numbers
- Word and character count
- Auto-save support
- Keyboard shortcuts for common operations

### File Operations
- Create new documents (Ctrl+N)
- Open markdown files (Ctrl+O)
- Save and Save As (Ctrl+S, Ctrl+Shift+S)
- Export to HTML
- Recent files tracking

### UI/UX
- **Light and Dark themes** (Ctrl+Shift+T to toggle)
- Clean, modern interface
- Zoom controls for preview
- Status bar with document stats
- Beautiful typography and styling

## Keyboard Shortcuts

### File Operations
- `Ctrl+N` - New file
- `Ctrl+O` - Open file
- `Ctrl+S` - Save
- `Ctrl+Shift+S` - Save As
- `Alt+F4` - Exit

### Editing
- `Ctrl+B` - Insert bold
- `Ctrl+I` - Insert italic
- `Ctrl+K` - Insert link
- `Ctrl+Shift+I` - Insert image
- `Ctrl+Shift+C` - Insert code block
- `Ctrl+T` - Insert table

### View
- `Ctrl+Shift+T` - Toggle theme
- `Ctrl++` - Zoom in preview
- `Ctrl+-` - Zoom out preview
- `Ctrl+0` - Reset zoom

## Architecture

### Technology Stack
- **.NET 8** - Modern, cross-platform framework
- **Avalonia UI 11.3** - Cross-platform UI framework
- **Markdig** - Fast, extensible Markdown processor
- **AvaloniaEdit** - Syntax-highlighted text editor
- **CommunityToolkit.Mvvm** - MVVM helpers and code generation

### Project Structure
```
MarkdownViewer/
├── Models/           # Data models
├── ViewModels/       # MVVM view models
├── Views/            # Avalonia XAML views
├── Services/         # Business logic services
└── Assets/           # Resources and styles
```

### Design Patterns
- **MVVM** - Model-View-ViewModel architecture
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **Service Layer** - Separation of concerns
- **Observable Pattern** - CommunityToolkit.Mvvm

## Building from Source

### Prerequisites
- .NET 8.0 SDK or later
- Any OS: Windows, macOS, or Linux

### Build Instructions
```bash
# Clone the repository
git clone <repository-url>
cd Markdown_viewer

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Development
```bash
# Build in Release mode
dotnet build -c Release

# Publish for your platform
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
```

## Markdown Feature Support

### Basic Syntax ✅
- Headings (H1-H6)
- Paragraphs and line breaks
- Bold, italic, strikethrough
- Lists (ordered, unordered, nested)
- Links and images
- Code (inline and blocks)
- Blockquotes
- Horizontal rules

### Extended Syntax ✅
- Tables
- Task lists
- Footnotes
- Definition lists
- Abbreviations
- Subscript and superscript

### GitHub Flavored Markdown ✅
- Syntax highlighting in code blocks
- Autolinks
- Strikethrough
- Tables
- Task lists
- Emoji support

### Advanced Features ✅
- Math expressions (LaTeX/KaTeX)
- Mermaid diagrams
- HTML embedding
- Front matter (YAML)

## Known Limitations

1. **WebView Preview**: Current implementation uses a basic renderer. For full HTML preview with math and diagrams, the Avalonia.WebView package needs to be integrated.

2. **PDF Export**: Not yet implemented. HTML export is available.

3. **Image Handling**: Images must be accessible via file path or URL.

4. **Collaborative Editing**: Not supported (single-user application).

## Future Enhancements

- [ ] Full WebView integration for rich preview
- [ ] PDF export functionality
- [ ] Vim mode for power users
- [ ] Custom CSS themes
- [ ] Plugin system
- [ ] Real-time collaboration
- [ ] Cloud sync integration
- [ ] Mobile support (iOS/Android)

## License

This project is open source and available under the MIT License.

## Credits

Built with:
- [Avalonia UI](https://avaloniaui.net/)
- [Markdig](https://github.com/xoofx/markdig)
- [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)

## File association (Windows)

To make `.md` files open with this app when double-clicked on Windows, you can register a file association. There are two common ways:

1) During installer/publish: add a file-association entry in your installer (recommended).

2) Per-user registration script: run `scripts/register-md-association.ps1` with the path to your installed exe. This registers entries under HKCU so “Open with” works and lets you set ViewMD as default in Windows Settings.

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\register-md-association.ps1 -ExePath "C:\\Program Files\\ViewMD\\MarkdownViewer.exe" -SetDefault
```

3) Manual registry merge: a `.reg` file is provided in `docs/associate-md.reg` as an example. Edit the executable path in that file to point to your installed `MarkdownViewer.exe`, then double-click the `.reg` file to merge it into the registry (requires appropriate permissions).

Example usage (for testing):

1. Build and publish the app for Windows (self-contained recommended):

```powershell
dotnet publish -c Release -r win-x64 --self-contained -o publish\win-x64
```

2. Edit `docs\associate-md.reg` to point to the published executable path (or copy the EXE into `C:\Program Files\\ViewMD\\`).

3. Double-click the `.reg` file to merge it. After that, double-clicking a `.md` file should start ViewMD and open the file.

4. Alternatively for testing without registry edits, you can run the app with a file path argument:

```powershell
dotnet run -- "C:\path\to\example.md"
# or run the published exe directly:
publish\win-x64\MarkdownViewer.exe "C:\path\to\example.md"
```

To undo the per-user registration:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\unregister-md-association.ps1
```

Security: editing the registry affects your system configuration — only run scripts/merge `.reg` files you trust. Installer-based registration is safer for end users.

## Packaging / Installers

### Inno Setup (EXE installer)
An Inno Setup script is provided at `installer/inno-setup.iss` which:
- Copies the published binaries to Program Files
- Registers `.md` file associations (optional task during install)
- Adds the app to "Open with"

Steps:
1. Publish your app to `publish\win-x64`:
	```powershell
	dotnet publish -c Release -r win-x64 --self-contained -o publish\win-x64 MarkdownViewer.csproj
	```
2. Open `installer\inno-setup.iss` in Inno Setup and build the installer.
3. Run the generated `MarkdownViewer-Setup.exe`. Choose the file association task if desired.

### MSIX (AppX) package
An MSIX `AppxManifest.xml` template is under `installer/` with file type associations for `.md`, `.markdown`, and `.txt`, and full-trust desktop process enabled.

Notes:
- You’ll need to sign the package and supply required assets (icons) referenced in the manifest.
- The app runs as a full-trust desktop app; ensure the executable path in the manifest matches your package layout.

#### Quick MSIX packaging guide

1) Generate stub icons (optional) and validate assets:
```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\create-stub-icons.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\check-msix-assets.ps1
```

2) Build and create a signed MSIX (optional signing):
```powershell
# Unsigned (for quick local testing, may require sideloading)
pwsh -ExecutionPolicy Bypass -File .\scripts\package-msix.ps1 -Configuration Release -Runtime win-x64 -MsixPath ViewMD.msix

# Signed (provide your PFX and password securely)
$sec = Read-Host -AsSecureString "PFX password"
pwsh -ExecutionPolicy Bypass -File .\scripts\package-msix.ps1 -Configuration Release -Runtime win-x64 -MsixPath ViewMD.msix -PfxPath ".\signing-test.pfx" -PfxPassword $sec
```

3) Install the MSIX
- Double-click the `.msix` file.
- If signed with a self-signed cert, install the cert to CurrentUser\Trusted People first.
- Ensure sideloading is enabled if required.

Troubleshooting
- Icons missing: run the asset check script and ensure all listed files exist under `Assets/`.
- Default app choice: Windows manages default apps; your file associations appear, but users may need to confirm defaults in Settings.
- Packaging tools: Make sure `MakeAppx.exe` and `signtool.exe` are available (Windows SDK).
