using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownViewer.Models;

namespace MarkdownViewer.ViewModels;

public enum ReadModeWidth
{
    Fit = 0,      // Full width
    Percent50 = 1,
    Percent75 = 2,
    Percent100 = 3 // 900px default
}

public partial class DocumentViewModel : ViewModelBase
{
    [ObservableProperty] private MarkdownDocument _currentDocument = new();
    [ObservableProperty] private EditorViewModel _editorViewModel;
    [ObservableProperty] private PreviewViewModel _previewViewModel;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _isReadMode = true;
    [ObservableProperty] private string _title = "Untitled";
    [ObservableProperty] private ReadModeWidth _readWidth = ReadModeWidth.Percent100;

    public double PreviewMaxWidth => ReadWidth switch
    {
        ReadModeWidth.Fit => double.PositiveInfinity,
        ReadModeWidth.Percent50 => 450,
        ReadModeWidth.Percent75 => 675,
        ReadModeWidth.Percent100 => 900,
        _ => 900
    };

    public DocumentViewModel(EditorViewModel editorViewModel, PreviewViewModel previewViewModel)
    {
        _editorViewModel = editorViewModel;
        _previewViewModel = previewViewModel;

        _editorViewModel.TextChangedDebounced += async (_, text) =>
        {
            await _previewViewModel.UpdatePreviewAsync(text);
            IsDirty = CurrentDocument.Content != text;
        };
    }

    partial void OnReadWidthChanged(ReadModeWidth value)
    {
        OnPropertyChanged(nameof(PreviewMaxWidth));
    }

    public void ApplyDocument(MarkdownDocument document)
    {
        CurrentDocument = document;
        Title = string.IsNullOrEmpty(document.Title) ? "Untitled" : document.Title;
        EditorViewModel.Text = document.Content ?? string.Empty;
        IsDirty = false;
    }

    [RelayCommand]
    private void SetReadWidth(object parameter)
    {
        if (parameter is string strValue && int.TryParse(strValue, out int value))
        {
            ReadWidth = (ReadModeWidth)value;
        }
        else if (parameter is int intValue)
        {
            ReadWidth = (ReadModeWidth)intValue;
        }
    }
}
