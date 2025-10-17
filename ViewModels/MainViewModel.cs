using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownViewer.Models;
using MarkdownViewer.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;

namespace MarkdownViewer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private MarkdownDocument _currentDocument;
    [ObservableProperty] private EditorViewModel _editorViewModel;
    [ObservableProperty] private PreviewViewModel _previewViewModel;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _isDirty = false;

    [ObservableProperty] private ObservableCollection<DocumentViewModel> _documents = [];
    [ObservableProperty] private DocumentViewModel? _activeDocument;
    [ObservableProperty] private string _windowTitle = "ViewMD";
    [ObservableProperty] private ObservableCollection<string> _recentFiles = [];
    [ObservableProperty] private AppTheme _currentTheme = AppTheme.GlassLight;

    private readonly IFileService _fileService;
    private readonly IExportService _exportService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IDialogService _dialogService;

    internal IFileService FileService => _fileService;
    internal IExportService ExportService => _exportService;
    internal IRecentFilesService RecentFilesService => _recentFilesService;


    public MainViewModel()
    {
        // Design-time only: minimal initialization without DI
        Documents = [];
        ActiveDocument = null;
        _editorViewModel = new EditorViewModel();
        _previewViewModel = new PreviewViewModel(null!);
        _currentDocument = new MarkdownDocument();
        _fileService = null!;
        _exportService = null!;
        _recentFilesService = null!;
        _dialogService = null!;
    }

    public MainViewModel(
        IFileService fileService,
        IExportService exportService,
        IRecentFilesService recentFilesService,
        IDialogService dialogService,
        EditorViewModel editorViewModel,
        PreviewViewModel previewViewModel)
    {
        _fileService = fileService;
        _exportService = exportService;
        _recentFilesService = recentFilesService;
        _dialogService = dialogService;
        _editorViewModel = editorViewModel;
        _previewViewModel = previewViewModel;
        _currentDocument = new MarkdownDocument();

        // Load recent files
        LoadRecentFiles();

        // Initialize with one tab (fire-and-forget for constructor)
        _ = NewFile();
    }

    internal void LoadRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var file in _recentFilesService.RecentFiles)
        {
            RecentFiles.Add(file);
        }
    }

    // These methods will be called from the View
    public Func<Task<string?>>? ShowOpenFileDialogAsync { get; set; }
    public Func<string?, Task<string?>>? ShowSaveFileDialogAsync { get; set; }

    // Open a file from a provided path (used for command-line / file association startup)
    public async Task OpenFileFromPathAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (ActiveDocument?.IsDirty == true)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Unsaved Changes",
                $"The document '{ActiveDocument.Title}' has unsaved changes. Do you want to discard them?");

            if (!result)
            {
                return; // User chose not to discard changes
            }
        }

        var document = await _fileService.OpenFileAsync(path);
        if (document != null)
        {
            _recentFilesService.AddRecentFile(path);
            LoadRecentFiles();

            var docVm = CreateDocumentViewModel();
            docVm.ApplyDocument(document);
            Documents.Add(docVm);
            ActiveDocument = docVm;
            SyncTopLevelWithActive();
            StatusText = $"Opened: {document.Title}";
        }
        else
        {
            StatusText = "Failed to open file";
        }
    }

    public static DocumentViewModel CreateDocumentViewModel()
    {
        // Resolve via DI to ensure services (e.g., IMarkdownService) are injected
        var sp = (Application.Current as App)?.Services;
        if (sp == null)
        {
            // Fallback (should not happen): create with bare VMs (limited functionality)
            return new DocumentViewModel(new EditorViewModel(), new PreviewViewModel(null!));
        }
        return sp.GetRequiredService<DocumentViewModel>();
    }

    internal void SyncTopLevelWithActive()
    {
        if (ActiveDocument == null) return;
        CurrentDocument = ActiveDocument.CurrentDocument;
        EditorViewModel = ActiveDocument.EditorViewModel;
        PreviewViewModel = ActiveDocument.PreviewViewModel;
        IsDirty = ActiveDocument.IsDirty;
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var isDirty = ActiveDocument?.IsDirty == true;
        var fileName = ActiveDocument?.Title ?? "Untitled";
        var dirtyMarker = isDirty ? "*" : "";
        WindowTitle = $"{fileName}{dirtyMarker} - ViewMD";
    }

    [RelayCommand]
    public async Task CloseDocument(DocumentViewModel? document)
    {
        if (document == null) return;

        if (document.IsDirty)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Unsaved Changes",
                $"The document '{document.Title}' has unsaved changes. Do you want to close without saving?");

            if (!result)
            {
                return; // User chose not to close
            }
        }

        Documents.Remove(document);

        // If we closed the active document, select another one
        if (ActiveDocument == document)
        {
            ActiveDocument = Documents.Count > 0 ? Documents[^1] : null;
            SyncTopLevelWithActive();
        }

        // If no documents remain, create a new one
        if (Documents.Count == 0)
        {
            _ = NewFile();
        }

        StatusText = "Document closed";
    }

    // File Operations
    [RelayCommand]
    public async Task NewFile()
    {
        if (ActiveDocument?.IsDirty == true)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Unsaved Changes",
                $"The document '{ActiveDocument.Title}' has unsaved changes. Do you want to create a new file anyway?");

            if (!result)
            {
                return; // User chose not to create new file
            }
        }

        var docVm = CreateDocumentViewModel();
        docVm.ApplyDocument(new MarkdownDocument());
        Documents.Add(docVm);
        ActiveDocument = docVm;
        SyncTopLevelWithActive();
        StatusText = "New file created";
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        if (ActiveDocument?.IsDirty == true)
        {
            var confirmResult = await _dialogService.ShowConfirmationAsync(
                "Unsaved Changes",
                $"The document '{ActiveDocument.Title}' has unsaved changes. Do you want to open a new file anyway?");

            if (!confirmResult)
            {
                return; // User chose not to open new file
            }
        }

        var openDlg = ShowOpenFileDialogAsync;
        if (openDlg is null)
        {
            return;
        }

        var result = await openDlg();
        if (string.IsNullOrEmpty(result))
        {
            return;
        }

        var document = await _fileService.OpenFileAsync(result);
        if (document != null)
        {
            _recentFilesService.AddRecentFile(result);
            LoadRecentFiles();

            var docVm = CreateDocumentViewModel();
            docVm.ApplyDocument(document);
            Documents.Add(docVm);
            ActiveDocument = docVm;
            SyncTopLevelWithActive();
            StatusText = $"Opened: {document.Title}";
        }
        else
        {
            StatusText = "Failed to open file";
        }
    }

    [RelayCommand]
    private async Task SaveFile()
    {
        if (ActiveDocument?.CurrentDocument.IsNewDocument == true)
        {
            await SaveFileAs();
            return;
        }

        if (ActiveDocument is null) return;
        var updatedDoc = ActiveDocument.CurrentDocument with { Content = ActiveDocument.EditorViewModel.Text };
        var success = await _fileService.SaveFileAsync(updatedDoc);

        if (success)
        {
            ActiveDocument.ApplyDocument(updatedDoc with { IsDirty = false });
            SyncTopLevelWithActive();
            StatusText = $"Saved: {ActiveDocument.Title}";
        }
        else
        {
            StatusText = "Failed to save file";
        }
    }

    [RelayCommand]
    private async Task SaveFileAs()
    {
        var saveDlg = ShowSaveFileDialogAsync;
        if (saveDlg is null) return;
        string? defaultExt = null;
        if (ActiveDocument != null && !string.IsNullOrEmpty(ActiveDocument.CurrentDocument.FilePath))
        {
            var ext = System.IO.Path.GetExtension(ActiveDocument.CurrentDocument.FilePath);
            if (!string.IsNullOrEmpty(ext) && ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                defaultExt = "txt";
            }
        }
        var result = await saveDlg(defaultExt);
        if (string.IsNullOrEmpty(result)) return;

        if (ActiveDocument is null) return;
        var updatedDoc = ActiveDocument.CurrentDocument with
        {
            Content = ActiveDocument.EditorViewModel.Text,
            FilePath = result,
            Title = System.IO.Path.GetFileNameWithoutExtension(result)
        };

        var success = await _fileService.SaveFileAsAsync(updatedDoc, result);

        if (success)
        {
            ActiveDocument.ApplyDocument(updatedDoc with { IsDirty = false });
            SyncTopLevelWithActive();
            StatusText = $"Saved as: {ActiveDocument.Title}";
        }
        else
        {
            StatusText = "Failed to save file";
        }
    }

    [RelayCommand]
    private async Task OpenRecentFile(string filePath) => await OpenFileFromPathAsync(filePath);

    [RelayCommand]
    private void ClearRecentFiles()
    {
        _recentFilesService.ClearRecentFiles();
        LoadRecentFiles();
        StatusText = "Recent files cleared";
    }

    // View Operations
    [RelayCommand]
    private void ToggleReadMode()
    {
        if (ActiveDocument == null) return;
        ActiveDocument.IsReadMode = !ActiveDocument.IsReadMode;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        if (ActiveDocument == null) return;
        ActiveDocument.IsDarkTheme = !ActiveDocument.IsDarkTheme;
    }

    // Theme Management
    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;

        // Update Application RequestedThemeVariant
        if (Application.Current is App app)
        {
            app.RequestedThemeVariant = theme.IsDarkTheme()
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
        }

        // Update all documents to use the theme's dark/light preference
        foreach (var doc in Documents)
        {
            doc.IsDarkTheme = theme.IsDarkTheme();
        }

        StatusText = $"Theme changed to {theme.GetDisplayName()}";
    }

    [RelayCommand]
    private void SetGlassLightTheme()
    {
        System.Diagnostics.Debug.WriteLine("SetGlassLightTheme called");
        SetTheme(AppTheme.GlassLight);
    }

    [RelayCommand]
    private void SetGlassDarkTheme()
    {
        System.Diagnostics.Debug.WriteLine("SetGlassDarkTheme called");
        SetTheme(AppTheme.GlassDark);
    }

    [RelayCommand]
    private void SetAcrylicLightTheme()
    {
        System.Diagnostics.Debug.WriteLine("SetAcrylicLightTheme called");
        SetTheme(AppTheme.AcrylicLight);
    }

    [RelayCommand]
    private void SetAcrylicDarkTheme()
    {
        System.Diagnostics.Debug.WriteLine("SetAcrylicDarkTheme called");
        SetTheme(AppTheme.AcrylicDark);
    }

    [RelayCommand]
    private void SetPureDarkTheme()
    {
        System.Diagnostics.Debug.WriteLine("SetPureDarkTheme called");
        SetTheme(AppTheme.PureDark);
    }

    // Tab Management
    [RelayCommand]
    private void CloseActiveTab()
    {
        _ = CloseDocument(ActiveDocument);
    }

    [RelayCommand]
    private void CloseOtherTabs()
    {
        if (ActiveDocument == null) return;

        var activeDoc = ActiveDocument;
        Documents.Clear();
        Documents.Add(activeDoc);
        ActiveDocument = activeDoc;
        SyncTopLevelWithActive();
        StatusText = "Other tabs closed";
    }

    [RelayCommand]
    private void CloseAllTabs()
    {
        // Close all but create a new one at the end
        Documents.Clear();
        ActiveDocument = null;
        _ = NewFile();
        StatusText = "All tabs closed";
    }

    [RelayCommand]
    private void NextTab()
    {
        if (Documents.Count <= 1 || ActiveDocument == null) return;

        int currentIndex = Documents.IndexOf(ActiveDocument);
        int nextIndex = (currentIndex + 1) % Documents.Count;
        ActiveDocument = Documents[nextIndex];
        SyncTopLevelWithActive();
    }

    [RelayCommand]
    private void PreviousTab()
    {
        if (Documents.Count <= 1 || ActiveDocument == null) return;

        int currentIndex = Documents.IndexOf(ActiveDocument);
        int previousIndex = currentIndex - 1;
        if (previousIndex < 0) previousIndex = Documents.Count - 1;
        ActiveDocument = Documents[previousIndex];
        SyncTopLevelWithActive();
    }

    // Export Operations
    [RelayCommand]
    private async Task CopyHtmlToClipboard()
    {
        if (ActiveDocument == null) return;

        try
        {
            var markdown = ActiveDocument.EditorViewModel.Text;
            var html = await (Application.Current as App)?.Services?.GetRequiredService<IMarkdownService>().RenderToHtmlAsync(markdown)!;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var clipboard = desktop.MainWindow.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(html);
                    StatusText = "HTML copied to clipboard";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to copy HTML: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportHtml()
    {
        var saveHtmlDlg = ShowSaveFileDialogAsync;
        if (saveHtmlDlg is null) return;
        var result = await saveHtmlDlg("html");
        if (string.IsNullOrEmpty(result)) return;

        if (ActiveDocument is null) return;
        var success = await _exportService.ExportToHtmlAsync(
            ActiveDocument.CurrentDocument with { Content = ActiveDocument.EditorViewModel.Text },
            result);

        StatusText = success ? $"Exported to: {result}" : "Failed to export";
    }

    [RelayCommand]
    private async Task Exit()
    {
        // Check if any documents have unsaved changes
        var dirtyDocuments = Documents.Where(d => d.IsDirty).ToList();

        if (dirtyDocuments.Any())
        {
            var message = dirtyDocuments.Count == 1
                ? $"The document '{dirtyDocuments[0].Title}' has unsaved changes. Do you want to exit anyway?"
                : $"{dirtyDocuments.Count} documents have unsaved changes. Do you want to exit anyway?";

            var result = await _dialogService.ShowConfirmationAsync(
                "Unsaved Changes",
                message);

            if (!result)
            {
                return; // User chose not to exit
            }
        }

        Environment.Exit(0);
    }

    // Update Operations
    [RelayCommand]
    private void CheckForUpdates()
    {
        // TODO: Replace with your actual Store ProductId after publishing
        const string productId = "9N1234567890"; // Placeholder: Get from Partner Center after publishing
        var uri = $"ms-windows-store://pdp/?ProductId={productId}";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            StatusText = "Opening Microsoft Store...";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to open Store: {ex.Message}";
        }
    }
}
