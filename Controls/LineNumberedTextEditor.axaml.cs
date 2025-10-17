using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Linq;

namespace MarkdownViewer.Controls;

public partial class LineNumberedTextEditor : UserControl
{
    private TextBox? _editor;
    private TextBlock? _lineNumbers;
    private ScrollViewer? _lineNumberScroller;
    private Border? _lineNumberBorder;

    // Dependency property for Text binding
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<LineNumberedTextEditor, string>(nameof(Text), defaultValue: string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // Dependency property for ShowLineNumbers
    public static readonly StyledProperty<bool> ShowLineNumbersProperty =
        AvaloniaProperty.Register<LineNumberedTextEditor, bool>(nameof(ShowLineNumbers), defaultValue: true);

    public bool ShowLineNumbers
    {
        get => GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    // Expose editor properties
    public bool IsReadOnly
    {
        get => _editor?.IsReadOnly ?? false;
        set { if (_editor != null) _editor.IsReadOnly = value; }
    }

    public int CaretIndex
    {
        get => _editor?.CaretIndex ?? 0;
        set { if (_editor != null) _editor.CaretIndex = value; }
    }

    // Get the internal scroll viewer for synchronized scrolling
    public ScrollViewer? GetEditorScrollViewer()
    {
        if (_editor == null) return null;

        // Find the ScrollViewer inside the TextBox template
        return _editor.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
    }

    // Events
    public event EventHandler<string>? TextChanged;
    public event EventHandler<double>? ScrollChanged;

    public LineNumberedTextEditor()
    {
        InitializeComponent();
        SetupControls();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _editor = this.FindControl<TextBox>("Editor");
        _lineNumbers = this.FindControl<TextBlock>("LineNumbers");
        _lineNumberScroller = this.FindControl<ScrollViewer>("LineNumberScroller");
        _lineNumberBorder = this.FindControl<Border>("LineNumberBorder");
    }

    private void SetupControls()
    {
        if (_editor == null) return;

        // Listen to text changes
        _editor.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(TextBox.Text))
            {
                var newText = _editor.Text ?? string.Empty;
                Text = newText;
                UpdateLineNumbers(newText);
                TextChanged?.Invoke(this, newText);
            }
        };

        // Initial line numbers
        UpdateLineNumbers(Text);

        // Set up scroll synchronization after the control is loaded
        _editor.AttachedToVisualTree += (_, _) =>
        {
            var scrollViewer = GetEditorScrollViewer();
            if (scrollViewer != null)
            {
                scrollViewer.PropertyChanged += (_, e) =>
                {
                    if (e.Property.Name == nameof(ScrollViewer.Offset))
                    {
                        var offset = scrollViewer.Offset;
                        var scrollPercentage = scrollViewer.Extent.Height > 0
                            ? offset.Y / scrollViewer.Extent.Height
                            : 0;
                        ScrollChanged?.Invoke(this, scrollPercentage);
                    }
                };
            }
        };

        // Add keyboard shortcuts for markdown formatting
        _editor.KeyDown += OnEditorKeyDown;
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (_editor == null) return;

        var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var isAlt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);

        // Ctrl+B - Bold
        if (isCtrl && e.Key == Key.B && !isAlt)
        {
            e.Handled = true;
            WrapSelection("**", "**");
        }
        // Ctrl+I - Italic
        else if (isCtrl && e.Key == Key.I && !isAlt)
        {
            e.Handled = true;
            WrapSelection("*", "*");
        }
        // Ctrl+K - Code
        else if (isCtrl && e.Key == Key.K && !isAlt)
        {
            e.Handled = true;
            WrapSelection("`", "`");
        }
        // Ctrl+Shift+K - Code block
        else if (isCtrl && e.Key == Key.K && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            WrapSelection("```\n", "\n```");
        }
        // Ctrl+L - Link
        else if (isCtrl && e.Key == Key.L && !isAlt)
        {
            e.Handled = true;
            InsertLink();
        }
        // Ctrl+Shift+S - Strikethrough
        else if (isCtrl && e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            WrapSelection("~~", "~~");
        }
        // Alt+H - Heading (increases with number keys)
        else if (isAlt && e.Key >= Key.D1 && e.Key <= Key.D6)
        {
            e.Handled = true;
            var level = (int)(e.Key - Key.D1) + 1;
            InsertHeading(level);
        }
    }

    private void WrapSelection(string prefix, string suffix)
    {
        if (_editor == null) return;

        var text = _editor.Text ?? string.Empty;
        var selectionStart = _editor.SelectionStart;
        var selectionEnd = _editor.SelectionEnd;

        if (selectionStart == selectionEnd)
        {
            // No selection - insert prefix and suffix with cursor in between
            var newText = text.Insert(selectionStart, prefix + suffix);
            _editor.Text = newText;
            _editor.CaretIndex = selectionStart + prefix.Length;
            _editor.SelectionStart = selectionStart + prefix.Length;
            _editor.SelectionEnd = selectionStart + prefix.Length;
        }
        else
        {
            // Wrap selection
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);
            var selectedText = text.Substring(start, end - start);
            var newText = text.Remove(start, end - start)
                             .Insert(start, prefix + selectedText + suffix);
            _editor.Text = newText;
            _editor.CaretIndex = start + prefix.Length + selectedText.Length + suffix.Length;
            _editor.SelectionStart = start + prefix.Length;
            _editor.SelectionEnd = start + prefix.Length + selectedText.Length;
        }
    }

    private void InsertLink()
    {
        if (_editor == null) return;

        var text = _editor.Text ?? string.Empty;
        var selectionStart = _editor.SelectionStart;
        var selectionEnd = _editor.SelectionEnd;

        if (selectionStart == selectionEnd)
        {
            // No selection - insert link template
            var linkText = "[Link text](url)";
            var newText = text.Insert(selectionStart, linkText);
            _editor.Text = newText;
            _editor.CaretIndex = selectionStart + 1;
            _editor.SelectionStart = selectionStart + 1;
            _editor.SelectionEnd = selectionStart + 10; // Select "Link text"
        }
        else
        {
            // Use selection as link text
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);
            var selectedText = text.Substring(start, end - start);
            var linkText = $"[{selectedText}](url)";
            var newText = text.Remove(start, end - start).Insert(start, linkText);
            _editor.Text = newText;
            _editor.CaretIndex = start + selectedText.Length + 3;
            _editor.SelectionStart = start + selectedText.Length + 3;
            _editor.SelectionEnd = start + selectedText.Length + 6; // Select "url"
        }
    }

    private void InsertHeading(int level)
    {
        if (_editor == null) return;

        var text = _editor.Text ?? string.Empty;
        var caretIndex = _editor.CaretIndex;

        // Find the start of the current line
        var lineStart = text.LastIndexOf('\n', Math.Max(0, caretIndex - 1)) + 1;
        var prefix = new string('#', level) + " ";

        // Check if line already starts with heading
        var lineEnd = text.IndexOf('\n', caretIndex);
        if (lineEnd == -1) lineEnd = text.Length;
        var currentLine = text.Substring(lineStart, lineEnd - lineStart);

        if (currentLine.StartsWith("#"))
        {
            // Replace existing heading
            var existingHashCount = currentLine.TakeWhile(c => c == '#').Count();
            var existingPrefix = new string('#', existingHashCount) + " ";
            var newText = text.Remove(lineStart, existingPrefix.Length).Insert(lineStart, prefix);
            _editor.Text = newText;
            _editor.CaretIndex = caretIndex - existingPrefix.Length + prefix.Length;
        }
        else
        {
            // Insert heading prefix
            var newText = text.Insert(lineStart, prefix);
            _editor.Text = newText;
            _editor.CaretIndex = caretIndex + prefix.Length;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            var newText = change.GetNewValue<string>() ?? string.Empty;
            if (_editor != null && _editor.Text != newText)
            {
                _editor.Text = newText;
            }
            UpdateLineNumbers(newText);
        }
        else if (change.Property == ShowLineNumbersProperty)
        {
            if (_lineNumberBorder != null)
            {
                _lineNumberBorder.IsVisible = change.GetNewValue<bool>();
            }
        }
    }

    private void UpdateLineNumbers(string text)
    {
        if (_lineNumbers == null) return;

        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Count(c => c == '\n') + 1;
        _lineNumbers.Text = string.Join("\n", Enumerable.Range(1, lineCount));
    }

    public void ApplyLightTheme()
    {
        if (_editor != null)
        {
            _editor.Background = Brushes.White;
            _editor.Foreground = Brushes.Black;
        }
        if (_lineNumberBorder != null)
        {
            _lineNumberBorder.Background = new SolidColorBrush(Color.Parse("#F8F8F8"));
            _lineNumberBorder.BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
        }
        if (_lineNumbers != null)
        {
            _lineNumbers.Foreground = new SolidColorBrush(Color.Parse("#888888"));
        }
    }

    public void ApplyDarkTheme()
    {
        if (_editor != null)
        {
            _editor.Background = new SolidColorBrush(Color.Parse("#F5F5F5"));
            _editor.Foreground = Brushes.Black;
        }
        if (_lineNumberBorder != null)
        {
            _lineNumberBorder.Background = new SolidColorBrush(Color.Parse("#E8E8E8"));
            _lineNumberBorder.BorderBrush = new SolidColorBrush(Color.Parse("#C0C0C0"));
        }
        if (_lineNumbers != null)
        {
            _lineNumbers.Foreground = new SolidColorBrush(Color.Parse("#666666"));
        }
    }

    public void Focus()
    {
        _editor?.Focus();
    }

    public void InsertText(string text, int position)
    {
        if (_editor == null) return;
        var currentText = _editor.Text ?? string.Empty;
        _editor.Text = currentText.Insert(position, text);
        _editor.CaretIndex = position + text.Length;
    }
}
