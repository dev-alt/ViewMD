using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using MarkdownViewer.Services;

namespace MarkdownViewer.ViewModels;

public partial class TitleBarViewModel : ViewModelBase
{
    private readonly MainViewModel _mainVM;

    public TitleBarViewModel(MainViewModel mainVM)
    {
        _mainVM = mainVM;
    }

    [ObservableProperty] private string _windowTitle = "ViewMD";

    [ObservableProperty] private ObservableCollection<string> _recentFiles = new();

    [RelayCommand]
    internal async Task NewFile()
    {
        if (_mainVM.IsDirty)
        {
            // TODO: Show confirmation dialog
        }

        var docVm = _mainVM.CreateDocumentViewModel();
        var doc = await _mainVM.FileService.CreateNewDocumentAsync();
        docVm.ApplyDocument(doc);
        _mainVM.Documents.Add(docVm);
        _mainVM.ActiveDocument = docVm;
        _mainVM.SyncTopLevelWithActive();
        _mainVM.StatusText = "New document created";
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        if (_mainVM.IsDirty)
        {
            // TODO: Show confirmation dialog
        }

        var openDlg = _mainVM.ShowOpenFileDialogAsync;
        if (openDlg is null) return;
        var result = await openDlg();
        if (string.IsNullOrEmpty(result)) return;

        var document = await _mainVM.FileService.OpenFileAsync(result);
        if (document != null)
        {
            _mainVM.RecentFilesService.AddRecentFile(result);
            _mainVM.LoadRecentFiles();

            var docVm = _mainVM.CreateDocumentViewModel();
            docVm.ApplyDocument(document);
            _mainVM.Documents.Add(docVm);
            _mainVM.ActiveDocument = docVm;
            _mainVM.SyncTopLevelWithActive();
            _mainVM.StatusText = $"Opened: {document.Title}";
        }
        else
        {
            _mainVM.StatusText = "Failed to open file";
        }
    }

    [RelayCommand]
    private async Task SaveFile()
    {
        if (_mainVM.ActiveDocument?.CurrentDocument.IsNewDocument == true)
        {
            await SaveFileAs();
            return;
        }

        if (_mainVM.ActiveDocument is null) return;
        var updatedDoc = _mainVM.ActiveDocument.CurrentDocument with { Content = _mainVM.ActiveDocument.EditorViewModel.Text };
        var success = await _mainVM.FileService.SaveFileAsync(updatedDoc);

        if (success)
        {
            _mainVM.ActiveDocument.ApplyDocument(updatedDoc with { IsDirty = false });
            _mainVM.SyncTopLevelWithActive();
            _mainVM.StatusText = $"Saved: {_mainVM.ActiveDocument.Title}";
        }
        else
        {
            _mainVM.StatusText = "Failed to save file";
        }
    }

    [RelayCommand]
    private async Task SaveFileAs()
    {
        var saveDlg = _mainVM.ShowSaveFileDialogAsync;
        if (saveDlg is null) return;
        string? defaultExt = null;
        if (_mainVM.ActiveDocument != null && !string.IsNullOrEmpty(_mainVM.ActiveDocument.CurrentDocument.FilePath))
        {
            var ext = System.IO.Path.GetExtension(_mainVM.ActiveDocument.CurrentDocument.FilePath);
            if (!string.IsNullOrEmpty(ext) && ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                defaultExt = "txt";
            }
        }
        var result = await saveDlg(defaultExt);
        if (string.IsNullOrEmpty(result)) return;

        if (_mainVM.ActiveDocument is null) return;
        var updatedDoc = _mainVM.ActiveDocument.CurrentDocument with
        {
            Content = _mainVM.ActiveDocument.EditorViewModel.Text,
            FilePath = result,
            Title = System.IO.Path.GetFileNameWithoutExtension(result)
        };

        var success = await _mainVM.FileService.SaveFileAsAsync(updatedDoc, result);

        if (success)
        {
            _mainVM.ActiveDocument.ApplyDocument(updatedDoc with { IsDirty = false });
            _mainVM.SyncTopLevelWithActive();
            _mainVM.StatusText = $"Saved as: {_mainVM.ActiveDocument.Title}";
        }
        else
        {
            _mainVM.StatusText = "Failed to save file";
        }
    }

    [RelayCommand]
    private async Task OpenRecentFile(string filePath) => await _mainVM.OpenFileFromPathAsync(filePath);

    [RelayCommand]
    private void ClearRecentFiles()
    {
        _mainVM.RecentFilesService.ClearRecentFiles();
        _mainVM.LoadRecentFiles();
        _mainVM.StatusText = "Recent files cleared";
    }

    [RelayCommand]
    private void ToggleReadMode()
    {
        if (_mainVM.ActiveDocument == null) return;
        _mainVM.ActiveDocument.IsReadMode = !_mainVM.ActiveDocument.IsReadMode;
    }

    [RelayCommand]
    private void CloseActiveTab()
    {
        _mainVM.CloseDocument(_mainVM.ActiveDocument);
    }

    [RelayCommand]
    private void CloseOtherTabs()
    {
        if (_mainVM.ActiveDocument == null) return;

        var activeDoc = _mainVM.ActiveDocument;
        _mainVM.Documents.Clear();
        _mainVM.Documents.Add(activeDoc);
        _mainVM.ActiveDocument = activeDoc;
        _mainVM.SyncTopLevelWithActive();
        _mainVM.StatusText = "Other tabs closed";
    }

    [RelayCommand]
    private void CloseAllTabs()
    {
        // Close all but create a new one at the end
        _mainVM.Documents.Clear();
        _mainVM.ActiveDocument = null;
        _ = NewFile();
        _mainVM.StatusText = "All tabs closed";
    }

    [RelayCommand]
    private async Task CopyHtmlToClipboard()
    {
        if (_mainVM.ActiveDocument == null) return;

        try
        {
            var markdown = _mainVM.ActiveDocument.EditorViewModel.Text;
            var html = await (Application.Current as App)?.Services?.GetRequiredService<IMarkdownService>().RenderToHtmlAsync(markdown)!;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var clipboard = desktop.MainWindow.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(html);
                    _mainVM.StatusText = "HTML copied to clipboard";
                }
            }
        }
        catch (Exception ex)
        {
            _mainVM.StatusText = $"Failed to copy HTML: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportHtml()
    {
        var saveHtmlDlg = _mainVM.ShowSaveFileDialogAsync;
        if (saveHtmlDlg is null) return;
        var result = await saveHtmlDlg("html");
        if (string.IsNullOrEmpty(result)) return;

        if (_mainVM.ActiveDocument is null) return;
        var success = await _mainVM.ExportService.ExportToHtmlAsync(
            _mainVM.ActiveDocument.CurrentDocument with { Content = _mainVM.ActiveDocument.EditorViewModel.Text },
            result);

        _mainVM.StatusText = success ? $"Exported to: {result}" : "Failed to export";
    }

    [RelayCommand]
    private void Exit()
    {
        if (_mainVM.IsDirty)
        {
            // TODO: Show confirmation dialog
        }
        Environment.Exit(0);
    }

    [RelayCommand]
    private void NextTab()
    {
        if (_mainVM.Documents.Count <= 1 || _mainVM.ActiveDocument == null) return;

        int currentIndex = _mainVM.Documents.IndexOf(_mainVM.ActiveDocument);
        int nextIndex = (currentIndex + 1) % _mainVM.Documents.Count;
        _mainVM.ActiveDocument = _mainVM.Documents[nextIndex];
        _mainVM.SyncTopLevelWithActive();
    }

    [RelayCommand]
    private void PreviousTab()
    {
        if (_mainVM.Documents.Count <= 1 || _mainVM.ActiveDocument == null) return;

        int currentIndex = _mainVM.Documents.IndexOf(_mainVM.ActiveDocument);
        int previousIndex = currentIndex - 1;
        if (previousIndex < 0) previousIndex = _mainVM.Documents.Count - 1;
        _mainVM.ActiveDocument = _mainVM.Documents[previousIndex];
        _mainVM.SyncTopLevelWithActive();
    }
}