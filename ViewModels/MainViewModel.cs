using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownViewer.Models;
using MarkdownViewer.Services;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace MarkdownViewer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private MarkdownDocument _currentDocument;
    [ObservableProperty] private EditorViewModel _editorViewModel;
    [ObservableProperty] private PreviewViewModel _previewViewModel;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _isDirty = false;
    [ObservableProperty] private bool _isDarkTheme = false;
    [ObservableProperty] private string _windowTitle = "ViewMD";

    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<DocumentViewModel> _documents = new();
    [ObservableProperty] private DocumentViewModel? _activeDocument;

    private readonly IFileService _fileService;
    private readonly IExportService _exportService;

    public MainViewModel(
        IFileService fileService,
        IExportService exportService,
        EditorViewModel editorViewModel,
        PreviewViewModel previewViewModel)
    {
        _fileService = fileService;
        _exportService = exportService;
        _editorViewModel = editorViewModel;
        _previewViewModel = previewViewModel;
        _currentDocument = new MarkdownDocument();

        // Initialize with one tab
        _ = NewFileAsync();
    }

    [RelayCommand]
    private async Task NewFileAsync()
    {
        if (IsDirty)
        {
            // TODO: Show confirmation dialog
        }

        var docVm = CreateDocumentViewModel();
        var doc = await _fileService.CreateNewDocumentAsync();
        docVm.ApplyDocument(doc);
        Documents.Add(docVm);
        ActiveDocument = docVm;
        SyncTopLevelWithActive();
        StatusText = "New document created";
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (IsDirty)
        {
            // TODO: Show confirmation dialog
        }

    var openDlg = ShowOpenFileDialogAsync;
    if (openDlg is null) return;
    var result = await openDlg();
    if (string.IsNullOrEmpty(result)) return;

        var document = await _fileService.OpenFileAsync(result);
        if (document != null)
        {
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
    private async Task SaveFileAsync()
    {
        if (ActiveDocument?.CurrentDocument.IsNewDocument == true)
        {
            await SaveFileAsAsync();
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
    private async Task SaveFileAsAsync()
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
    private async Task ExportHtmlAsync()
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
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        if (ActiveDocument != null)
        {
            ActiveDocument.ApplyTheme(IsDarkTheme);
        }
        StatusText = IsDarkTheme ? "Dark theme enabled" : "Light theme enabled";
    }

    [RelayCommand]
    private void Exit()
    {
        if (IsDirty)
        {
            // TODO: Show confirmation dialog
        }
        Environment.Exit(0);
    }

    private void UpdateWindowTitle()
    {
        var isDirty = ActiveDocument?.IsDirty == true;
    var fileName = ActiveDocument?.Title ?? "Untitled";
    var dirtyMarker = isDirty ? "*" : "";
    WindowTitle = $"{fileName}{dirtyMarker} - ViewMD";
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

    private DocumentViewModel CreateDocumentViewModel()
    {
        // Resolve via DI to ensure services (e.g., IMarkdownService) are injected
        var sp = (Application.Current as App)?.Services;
        if (sp == null)
        {
            // Fallback (should not happen): create with bare VMs (limited functionality)
            var fallback = new DocumentViewModel(new EditorViewModel(), new PreviewViewModel(null!));
            fallback.ApplyTheme(IsDarkTheme);
            return fallback;
        }
        var docVm = sp.GetRequiredService<DocumentViewModel>();
        docVm.ApplyTheme(IsDarkTheme);
        return docVm;
    }

    private void SyncTopLevelWithActive()
    {
        if (ActiveDocument == null) return;
        CurrentDocument = ActiveDocument.CurrentDocument;
        EditorViewModel = ActiveDocument.EditorViewModel;
        PreviewViewModel = ActiveDocument.PreviewViewModel;
        IsDirty = ActiveDocument.IsDirty;
        UpdateWindowTitle();
    }

    [RelayCommand]
    private void ToggleReadMode()
    {
        if (ActiveDocument == null) return;
        ActiveDocument.IsReadMode = !ActiveDocument.IsReadMode;
    }

    [RelayCommand]
    private void CloseDocument(DocumentViewModel? document)
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
            _ = NewFileAsync();
        }

        StatusText = "Document closed";
    }
}
