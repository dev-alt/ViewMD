using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownViewer.Models;

namespace MarkdownViewer.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private EditorState _state = new();
    [ObservableProperty] private bool _showLineNumbers = true;

    private System.Timers.Timer? _debounceTimer;
    private const int DebounceDelay = 300;

    public event EventHandler<string>? TextChangedDebounced;

    partial void OnTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer = new System.Timers.Timer(DebounceDelay);
        _debounceTimer.Elapsed += (s, e) =>
        {
            TextChangedDebounced?.Invoke(this, value);
            _debounceTimer?.Stop();
        };
        _debounceTimer.Start();

        UpdateState();
    }

    private void UpdateState()
    {
        var wordCount = string.IsNullOrWhiteSpace(Text) ? 0 :
            Text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        State = State with
        {
            CharCount = Text.Length,
            WordCount = wordCount
        };
    }

    [RelayCommand]
    private void InsertBold()
    {
        InsertMarkdown("**", "**");
    }

    [RelayCommand]
    private void InsertItalic()
    {
        InsertMarkdown("*", "*");
    }

    [RelayCommand]
    private void InsertLink()
    {
        InsertMarkdown("[", "](url)");
    }

    [RelayCommand]
    private void InsertImage()
    {
        InsertMarkdown("![", "](url)");
    }

    [RelayCommand]
    private void InsertCodeBlock()
    {
        InsertMarkdown("```\n", "\n```");
    }

    [RelayCommand]
    private void InsertTable()
    {
        var table = @"| Column 1 | Column 2 | Column 3 |
|----------|----------|----------|
| Cell 1   | Cell 2   | Cell 3   |
| Cell 4   | Cell 5   | Cell 6   |";
        InsertText(table);
    }

    private void InsertMarkdown(string prefix, string suffix)
    {
        InsertText($"{prefix}text{suffix}");
    }

    private void InsertText(string text)
    {
        // This will be handled by the view's code-behind
        TextInsertRequested?.Invoke(this, text);
    }

    public event EventHandler<string>? TextInsertRequested;
}
