# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ViewMD is a cross-platform Markdown reader and editor built with Avalonia UI 11.3 and .NET 8. It supports GitHub Flavored Markdown, syntax highlighting, live preview, and file associations for `.md` and `.txt` files.

## Build and Run Commands

### Development
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

# Run with a file argument (file association testing)
dotnet run -- "path/to/file.md"

# Using the dev script (PowerShell)
pwsh -ExecutionPolicy Bypass -File ./scripts/dev-run.ps1
pwsh -ExecutionPolicy Bypass -File ./scripts/dev-run.ps1 -- "C:\Docs\README.md"
```

### Publishing
```bash
# Publish for Windows (self-contained)
dotnet publish -c Release -r win-x64 --self-contained -o publish/win-x64

# Publish for Linux
dotnet publish -c Release -r linux-x64 --self-contained -o publish/linux-x64

# Publish for macOS
dotnet publish -c Release -r osx-x64 --self-contained -o publish/osx-x64
```

### Packaging
```bash
# Package for Microsoft Store (creates .msix and .msixbundle)
pwsh -ExecutionPolicy Bypass -File ./scripts/package-store.ps1 -PfxPath ./certs/ViewMD.pfx

# Install locally for testing (sideload)
pwsh -ExecutionPolicy Bypass -File ./scripts/install-local.ps1

# Register file association (Windows)
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/register-md-association.ps1 -ExePath "C:\Program Files\ViewMD\MarkdownViewer.exe" -SetDefault
```

## Architecture

### MVVM with Dependency Injection

The application follows a strict MVVM architecture with Microsoft's dependency injection container configured in `App.axaml.cs:25-44`.

**Service Registration:**
- `IMarkdownService`, `IFileService`, `IExportService` - Registered as **Singletons**
- All ViewModels (`MainViewModel`, `EditorViewModel`, `PreviewViewModel`, `DocumentViewModel`) - Registered as **Transient**

**Important:** ViewModels must be resolved via DI to ensure services are properly injected. See `MainViewModel.CreateDocumentViewModel():229-243` for the pattern.

### Document Management

The app supports multiple documents via a tab-based system:
- `MainViewModel` maintains an `ObservableCollection<DocumentViewModel>` of open documents
- Each `DocumentViewModel` has its own `EditorViewModel` and `PreviewViewModel` instances
- `ActiveDocument` property tracks the currently selected document
- `SyncTopLevelWithActive()` synchronizes the active document's state with top-level properties

### Preview Rendering Pipeline

1. User types in `EditorViewModel` → text changes are debounced (300ms) via `EditorViewModel:19-31`
2. `TextChangedDebounced` event fires → `DocumentViewModel:23-27` handles it
3. Calls `PreviewViewModel.UpdatePreviewAsync()` with cancellation token support
4. `MarkdownService.RenderToHtmlAsync()` processes Markdown using Markdig pipeline
5. `MarkdownService.GeneratePreviewHtml()` wraps HTML with styling, Mermaid, and KaTeX support
6. HTML is displayed in the preview pane

### Key Design Patterns

- **Record types** for immutable data models (`MarkdownDocument`, `EditorState`, `AppSettings`)
- **CommunityToolkit.Mvvm** for `[ObservableProperty]` and `[RelayCommand]` source generation
- **Compiled bindings** enabled globally via `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`
- **Async/await** with `CancellationToken` for rendering operations to prevent race conditions

### File Association & Startup

Command-line arguments are processed in `App.axaml.cs:62-103`:
- Supports plain file paths and `file://` URIs
- First file opens in the main window
- Additional files open in separate windows
- See README.md for Windows registry association setup

## Markdown Processing

Markdig pipeline configuration in `MarkdownService.cs:14-31`:
- Uses `UseAdvancedExtensions()` for GFM support
- Includes emoji, task lists, tables, math (KaTeX), diagrams (Mermaid)
- HTML output is wrapped with CSS, Mermaid.js, and KaTeX scripts

Preview HTML generation includes theme-aware styling (light/dark) with proper syntax highlighting colors.

## Important Implementation Details

### Debouncing
Editor changes are debounced (300ms) to avoid excessive re-renders during typing. The timer is recreated on each keystroke in `EditorViewModel:19-31`.

### Cancellation
Preview rendering uses `CancellationTokenSource` that gets cancelled when new render requests arrive, preventing stale renders from completing (`PreviewViewModel:25-39`).

### Read Mode
Documents have a `IsReadMode` property (default: true) that hides the editor and shows only the preview. Toggle via `MainViewModel.ToggleReadMode()`.

### Dialog Delegation
File dialogs are delegated from `MainViewModel` to the View layer via function properties:
- `ShowOpenFileDialogAsync` - Set by View to display open file dialog
- `ShowSaveFileDialogAsync` - Set by View to display save file dialog

This pattern keeps platform-specific UI code out of ViewModels.

### Theme Management
Theme state is maintained at both the `MainViewModel` level and per-`DocumentViewModel`. When toggling themes:
1. `MainViewModel.IsDarkTheme` is updated
2. `ActiveDocument.ApplyTheme()` is called
3. Preview is regenerated with new theme CSS

## Common Gotchas

1. **Never instantiate ViewModels directly** - Always use DI via `Services.GetRequiredService<T>()` or they won't have their dependencies injected.

2. **Project file name vs namespace** - The .csproj is named `MarkdownViewer.csproj` but the namespace is `MarkdownViewer`. The executable is `MarkdownViewer.exe`.

3. **Compiled bindings** - Binding errors will cause compile-time failures. Always bind to properties that exist on the DataContext.

4. **File paths in arguments** - The app normalizes both plain paths and `file://` URIs. Windows file associations may pass either format.

5. **IsDirty tracking** - Currently tracked per document but confirmation dialogs are TODO (see `MainViewModel:47-50`, `63-67`, `183-187`, `207-211`).
