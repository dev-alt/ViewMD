using CommunityToolkit.Mvvm.ComponentModel;
using MarkdownViewer.Models;

namespace MarkdownViewer.ViewModels;

public partial class DocumentViewModel : ViewModelBase
{
    [ObservableProperty] private MarkdownDocument _currentDocument = new();
    [ObservableProperty] private EditorViewModel _editorViewModel;
    [ObservableProperty] private PreviewViewModel _previewViewModel;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _isReadMode = true;
    [ObservableProperty] private string _title = "Untitled";

    public DocumentViewModel(EditorViewModel editorViewModel, PreviewViewModel previewViewModel)
    {
        _editorViewModel = editorViewModel;
        _previewViewModel = previewViewModel;

        _editorViewModel.TextChangedDebounced += async (s, text) =>
        {
            await _previewViewModel.UpdatePreviewAsync(text);
            IsDirty = CurrentDocument.Content != text;
        };
    }

    public void ApplyDocument(MarkdownDocument document)
    {
        CurrentDocument = document;
        Title = string.IsNullOrEmpty(document.Title) ? "Untitled" : document.Title;
        EditorViewModel.Text = document.Content ?? string.Empty;
        IsDirty = false;
    }
}
