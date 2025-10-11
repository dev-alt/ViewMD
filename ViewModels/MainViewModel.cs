using System;
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

    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<DocumentViewModel> _documents = new();
    [ObservableProperty] private DocumentViewModel? _activeDocument;
    [ObservableProperty] private TitleBarViewModel _titleBarVM;

    private readonly IFileService _fileService;
    private readonly IExportService _exportService;
    private readonly IRecentFilesService _recentFilesService;

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
    }
    
    public MainViewModel(
        IFileService fileService,
        IExportService exportService,
        IRecentFilesService recentFilesService,
        EditorViewModel editorViewModel,
        PreviewViewModel previewViewModel)
    {
        _fileService = fileService;
        _exportService = exportService;
        _recentFilesService = recentFilesService;
        _editorViewModel = editorViewModel;
        _previewViewModel = previewViewModel;
        _currentDocument = new MarkdownDocument();

        TitleBarVM = new TitleBarViewModel(this);

        // Load recent files
        LoadRecentFiles();

        // Initialize with one tab
        _ = NewFileAsync();
    }

    internal void LoadRecentFiles()
    {
        TitleBarVM.RecentFiles.Clear();
        foreach (var file in _recentFilesService.RecentFiles)
        {
            TitleBarVM.RecentFiles.Add(file);
        }
    }

    // These methods will be called from the View
    public Func<Task<string?>>? ShowOpenFileDialogAsync { get; set; }
    public Func<string?, Task<string?>>? ShowSaveFileDialogAsync { get; set; }

    // Open a file from a provided path (used for command-line / file association startup)
    public async Task OpenFileFromPathAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (IsDirty)
        {
            // TODO: Show confirmation dialog before discarding unsaved changes
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

    public DocumentViewModel CreateDocumentViewModel()
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
    TitleBarVM.WindowTitle = $"{fileName}{dirtyMarker} - ViewMD";
    }

    [RelayCommand]
    public async Task CloseDocument(DocumentViewModel? document)
    {
        if (document == null) return;

        // TODO: Show confirmation dialog if dirty
        if (document.IsDirty)
        {
            // For now, just close it
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
            await TitleBarVM.NewFile();
        }

        StatusText = "Document closed";
    }

    // Add this method to MainViewModel
public async Task NewFileAsync()
{
    var docVm = CreateDocumentViewModel();
    docVm.ApplyDocument(new MarkdownDocument());
    Documents.Add(docVm);
    ActiveDocument = docVm;
    SyncTopLevelWithActive();
    StatusText = "New file created";
}

[RelayCommand]
private void ToggleReadMode()
{
    if (ActiveDocument == null) return;
    ActiveDocument.IsReadMode = !ActiveDocument.IsReadMode;
}
}
