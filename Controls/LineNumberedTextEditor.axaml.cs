using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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

    // Events
    public event EventHandler<string>? TextChanged;

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
