using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Layout;
using MarkdownViewer.ViewModels;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Linq;
using System.Text;

namespace MarkdownViewer.Views;

public partial class EditorPreviewView : UserControl
{
    private TextBox? _editor;
    private StackPanel? _previewContent;
    private Border? _editorBorder;
    private Border? _previewBorder;
    private GridSplitter? _splitter;
    private DispatcherTimer? _renderTimer;
    private string _lastRenderedText = string.Empty;

    public EditorPreviewView()
    {
        InitializeComponent();
        SetupEditor();
        SetupPreview();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _editor = this.FindControl<TextBox>("MarkdownEditor");
        _previewContent = this.FindControl<StackPanel>("PreviewContent");
        _editorBorder = this.FindControl<Border>("EditorBorder");
        _previewBorder = this.FindControl<Border>("PreviewBorder");
        _splitter = this.FindControl<GridSplitter>("Splitter");
    }

    private void SetupEditor()
    {
        if (_editor == null) return;

        // Subscribe to text changes
        _editor.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(TextBox.Text))
            {
                OnEditorTextChanged();
            }
        };

        // Focus the editor
        Dispatcher.UIThread.Post(() => _editor.Focus(), DispatcherPriority.Background);
    }

    private void SetupPreview()
    {
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _renderTimer.Tick += async (s, e) =>
        {
            _renderTimer?.Stop();
            if (DataContext is MainViewModel vm && _editor != null)
            {
                var text = _editor.Text ?? string.Empty;
                await vm.PreviewViewModel.UpdatePreviewAsync(text);
                UpdatePreview(text, vm.IsDarkTheme);
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainViewModel vm)
        {
            // Wire up editor to view model
            vm.EditorViewModel.TextInsertRequested += OnTextInsertRequested;

            // Subscribe to EditorViewModel.Text changes (for file loading)
            vm.EditorViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(vm.EditorViewModel.Text) && _editor != null)
                {
                    // Only update if different to avoid loops
                    if (_editor.Text != vm.EditorViewModel.Text)
                    {
                        _editor.Text = vm.EditorViewModel.Text;
                    }
                }
            };

            // Subscribe to theme changes
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(vm.IsDarkTheme) && _editor != null)
                {
                    ApplyTheme(vm.IsDarkTheme);
                    UpdatePreview(_editor.Text ?? string.Empty, vm.IsDarkTheme);
                }
            };

            // Sync initial text if ViewModel has content, otherwise keep our starter text
            if (_editor != null)
            {
                if (!string.IsNullOrEmpty(vm.EditorViewModel.Text))
                {
                    _editor.Text = vm.EditorViewModel.Text;
                }
                ApplyTheme(vm.IsDarkTheme);

                // Trigger initial preview
                UpdatePreview(_editor.Text ?? string.Empty, vm.IsDarkTheme);

                // Focus the editor
                Dispatcher.UIThread.Post(() => _editor.Focus(), DispatcherPriority.Background);
            }
        }
    }

    private void OnEditorTextChanged()
    {
        if (DataContext is MainViewModel vm && _editor != null)
        {
            vm.EditorViewModel.Text = _editor.Text ?? string.Empty;

            // Restart debounce timer
            _renderTimer?.Stop();
            _renderTimer?.Start();
        }
    }

    private void OnTextInsertRequested(object? sender, string text)
    {
        if (_editor == null) return;

        var currentText = _editor.Text ?? string.Empty;
        var caretIndex = _editor.CaretIndex;

        // Insert text at caret position
        _editor.Text = currentText.Insert(caretIndex, text);

        // Move caret after inserted text
        _editor.CaretIndex = caretIndex + text.Length;
        _editor.Focus();
    }

    private void ApplyTheme(bool isDarkTheme)
    {
        if (_editor == null) return;

        if (isDarkTheme)
        {
            // Dark theme
            _editor.Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
            _editor.Foreground = new SolidColorBrush(Color.Parse("#D4D4D4"));

            if (_editorBorder != null)
                _editorBorder.BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E"));
            if (_previewBorder != null)
                _previewBorder.Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
            if (_splitter != null)
                _splitter.Background = new SolidColorBrush(Color.Parse("#3E3E3E"));
        }
        else
        {
            // Light theme
            _editor.Background = Brushes.White;
            _editor.Foreground = Brushes.Black;

            if (_editorBorder != null)
                _editorBorder.BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
            if (_previewBorder != null)
                _previewBorder.Background = Brushes.White;
            if (_splitter != null)
                _splitter.Background = new SolidColorBrush(Color.Parse("#E0E0E0"));
        }
    }

    private void UpdatePreview(string markdownText, bool isDarkTheme)
    {
        if (_previewContent == null || markdownText == _lastRenderedText)
            return;

        _lastRenderedText = markdownText;
        _previewContent.Children.Clear();

        if (string.IsNullOrWhiteSpace(markdownText))
        {
            var placeholder = new TextBlock
            {
                Text = "Start typing markdown in the editor to see the preview...",
                Foreground = isDarkTheme
                    ? new SolidColorBrush(Color.Parse("#666666"))
                    : new SolidColorBrush(Color.Parse("#999999")),
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            _previewContent.Children.Add(placeholder);
            return;
        }

        // Parse markdown using Markdig
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build();

        var document = Markdown.Parse(markdownText, pipeline);

        // Render each block element
        foreach (var block in document)
        {
            var element = RenderBlock(block, isDarkTheme);
            if (element != null)
            {
                _previewContent.Children.Add(element);
            }
        }
    }

    private Control? RenderBlock(Block block, bool isDarkTheme)
    {
        var textColor = isDarkTheme ? Color.Parse("#D4D4D4") : Color.Parse("#333333");
        var mutedColor = isDarkTheme ? Color.Parse("#999999") : Color.Parse("#666666");

        return block switch
        {
            HeadingBlock heading => RenderHeading(heading, isDarkTheme, textColor),
            ParagraphBlock paragraph => RenderParagraph(paragraph, textColor),
            CodeBlock codeBlock => RenderCodeBlock(codeBlock, isDarkTheme),
            ListBlock list => RenderList(list, textColor),
            QuoteBlock quote => RenderQuote(quote, isDarkTheme, textColor),
            ThematicBreakBlock => new Border
            {
                Height = 1,
                Background = new SolidColorBrush(mutedColor),
                Margin = new Thickness(0, 10, 0, 10)
            },
            _ => null
        };
    }

    private Control RenderHeading(HeadingBlock heading, bool isDarkTheme, Color textColor)
    {
        var text = ExtractText(heading.Inline);
        var fontSize = heading.Level switch
        {
            1 => 32.0,
            2 => 28.0,
            3 => 24.0,
            4 => 20.0,
            5 => 18.0,
            _ => 16.0
        };

        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(textColor),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, heading.Level == 1 ? 10 : 8, 0, 6)
        };

        if (heading.Level == 1)
        {
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(textBlock);
            panel.Children.Add(new Border
            {
                Height = 2,
                Background = new SolidColorBrush(textColor),
                Margin = new Thickness(0, 0, 0, 8)
            });
            return panel;
        }

        return textBlock;
    }

    private Control RenderParagraph(ParagraphBlock paragraph, Color textColor)
    {
        var text = ExtractText(paragraph.Inline);
        return new TextBlock
        {
            Text = text,
            FontSize = 14,
            Foreground = new SolidColorBrush(textColor),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
    }

    private Control RenderCodeBlock(CodeBlock codeBlock, bool isDarkTheme)
    {
        var code = new StringBuilder();
        foreach (var line in codeBlock.Lines)
        {
            code.AppendLine(line.ToString());
        }

        var bgColor = isDarkTheme ? Color.Parse("#2D2D2D") : Color.Parse("#F5F5F5");
        var textColor = isDarkTheme ? Color.Parse("#D4D4D4") : Color.Parse("#333333");

        return new Border
        {
            Background = new SolidColorBrush(bgColor),
            BorderBrush = isDarkTheme
                ? new SolidColorBrush(Color.Parse("#404040"))
                : new SolidColorBrush(Color.Parse("#E0E0E0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Child = new TextBlock
            {
                Text = code.ToString().TrimEnd(),
                FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                FontSize = 13,
                Foreground = new SolidColorBrush(textColor),
                TextWrapping = TextWrapping.NoWrap
            }
        };
    }

    private Control RenderList(ListBlock list, Color textColor)
    {
        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 10) };

        int itemNumber = 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            foreach (var block in item)
            {
                if (block is ParagraphBlock para)
                {
                    var text = ExtractText(para.Inline);
                    var prefix = list.IsOrdered ? $"{itemNumber}. " : "• ";

                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = prefix,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        Margin = new Thickness(20, 0, 8, 0)
                    });
                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = text,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        TextWrapping = TextWrapping.Wrap
                    });
                    panel.Children.Add(itemPanel);
                }
            }
            if (list.IsOrdered) itemNumber++;
        }

        return panel;
    }

    private Control RenderQuote(QuoteBlock quote, bool isDarkTheme, Color textColor)
    {
        var panel = new StackPanel { Spacing = 4 };

        foreach (var block in quote)
        {
            if (block is ParagraphBlock para)
            {
                var text = ExtractText(para.Inline);
                panel.Children.Add(new TextBlock
                {
                    Text = text,
                    FontSize = 14,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(textColor),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        var borderColor = isDarkTheme ? Color.Parse("#666666") : Color.Parse("#CCCCCC");
        var bgColor = isDarkTheme ? Color.Parse("#2A2A2A") : Color.Parse("#F9F9F9");

        return new Border
        {
            BorderBrush = new SolidColorBrush(borderColor),
            BorderThickness = new Thickness(4, 0, 0, 0),
            Background = new SolidColorBrush(bgColor),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 10),
            Child = panel
        };
    }

    private string ExtractText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline == null) return string.Empty;

        var result = new StringBuilder();
        foreach (var item in inline)
        {
            result.Append(item switch
            {
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => $"`{code.Content}`",
                EmphasisInline emphasis => ExtractEmphasisText(emphasis),
                LineBreakInline => "\n",
                LinkInline link => ExtractText(link as ContainerInline) + $" ({link.Url})",
                _ => item.ToString()
            });
        }
        return result.ToString();
    }

    private string ExtractEmphasisText(EmphasisInline emphasis)
    {
        var text = ExtractText(emphasis);
        // Bold (** or __)
        if (emphasis.DelimiterChar == '*' && emphasis.DelimiterCount == 2)
            return $"**{text}**";
        // Italic (* or _)
        if (emphasis.DelimiterCount == 1)
            return $"*{text}*";
        return text;
    }
}
