using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownViewer.Models;
using MarkdownViewer.Services;

namespace MarkdownViewer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private MarkdownDocument _currentDocument;
    [ObservableProperty] private EditorViewModel _editorViewModel;
    [ObservableProperty] private PreviewViewModel _previewViewModel;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _isDirty = false;
    [ObservableProperty] private bool _isDarkTheme = false;
    [ObservableProperty] private string _windowTitle = "Markdown Viewer";

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

        // Subscribe to editor text changes
        _editorViewModel.TextChangedDebounced += async (s, text) =>
        {
            await _previewViewModel.UpdatePreviewAsync(text);
            IsDirty = CurrentDocument.Content != text;
            UpdateWindowTitle();
        };

        // Initialize with empty document
        _ = NewFileAsync();
    }

    [RelayCommand]
    private async Task NewFileAsync()
    {
        if (IsDirty)
        {
            // TODO: Show confirmation dialog
        }

        CurrentDocument = await _fileService.CreateNewDocumentAsync();
        EditorViewModel.Text = string.Empty;
        IsDirty = false;
        StatusText = "New document created";
        UpdateWindowTitle();
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (IsDirty)
        {
            // TODO: Show confirmation dialog
        }

        var result = await ShowOpenFileDialogAsync?.Invoke()!;
        if (result == null) return;

        var document = await _fileService.OpenFileAsync(result);
        if (document != null)
        {
            CurrentDocument = document;
            EditorViewModel.Text = document.Content;
            IsDirty = false;
            StatusText = $"Opened: {document.Title}";
            UpdateWindowTitle();
        }
        else
        {
            StatusText = "Failed to open file";
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (CurrentDocument.IsNewDocument)
        {
            await SaveFileAsAsync();
            return;
        }

        var updatedDoc = CurrentDocument with { Content = EditorViewModel.Text };
        var success = await _fileService.SaveFileAsync(updatedDoc);

        if (success)
        {
            CurrentDocument = updatedDoc with { IsDirty = false };
            IsDirty = false;
            StatusText = $"Saved: {CurrentDocument.Title}";
            UpdateWindowTitle();
        }
        else
        {
            StatusText = "Failed to save file";
        }
    }

    [RelayCommand]
    private async Task SaveFileAsAsync()
    {
        var result = await ShowSaveFileDialogAsync?.Invoke(null);
        if (result == null) return;

        var updatedDoc = CurrentDocument with
        {
            Content = EditorViewModel.Text,
            FilePath = result,
            Title = System.IO.Path.GetFileNameWithoutExtension(result)
        };

        var success = await _fileService.SaveFileAsAsync(updatedDoc, result);

        if (success)
        {
            CurrentDocument = updatedDoc with { IsDirty = false };
            IsDirty = false;
            StatusText = $"Saved as: {CurrentDocument.Title}";
            UpdateWindowTitle();
        }
        else
        {
            StatusText = "Failed to save file";
        }
    }

    [RelayCommand]
    private async Task ExportHtmlAsync()
    {
        var result = await ShowSaveFileDialogAsync?.Invoke("html")!;
        if (result == null) return;

        var success = await _exportService.ExportToHtmlAsync(
            CurrentDocument with { Content = EditorViewModel.Text },
            result);

        StatusText = success ? $"Exported to: {result}" : "Failed to export";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        PreviewViewModel.IsDarkTheme = IsDarkTheme;
        _ = PreviewViewModel.UpdatePreviewAsync(EditorViewModel.Text);
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
        var dirtyMarker = IsDirty ? "*" : "";
        var fileName = string.IsNullOrEmpty(CurrentDocument.Title) ? "Untitled" : CurrentDocument.Title;
        WindowTitle = $"{fileName}{dirtyMarker} - Markdown Viewer";
    }

    // These methods will be called from the View
    public Func<Task<string?>>? ShowOpenFileDialogAsync { get; set; }
    public Func<string?, Task<string?>>? ShowSaveFileDialogAsync { get; set; }
}
