# Markdown Viewer

A lightweight, feature-rich Markdown editor and viewer built with Avalonia UI and .NET 8.

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
