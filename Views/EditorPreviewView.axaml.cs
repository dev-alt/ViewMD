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
    private StackPanel? _previewContentRight;
    private StackPanel? _previewContentFull;
    private Border? _editorBorder;
    private Border? _previewBorderRight;
    private Border? _previewBorderFull;
    private GridSplitter? _splitter;
    private TextBlock? _placeholderRight;
    private TextBlock? _placeholderFull;
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
        _previewContentRight = this.FindControl<StackPanel>("PreviewContentRight");
        _previewContentFull = this.FindControl<StackPanel>("PreviewContentFull");
        _editorBorder = this.FindControl<Border>("EditorBorder");
        _previewBorderRight = this.FindControl<Border>("PreviewBorderRight");
        _previewBorderFull = this.FindControl<Border>("PreviewBorderFull");
        _splitter = this.FindControl<GridSplitter>("Splitter");
        _placeholderRight = this.FindControl<TextBlock>("PlaceholderRight");
        _placeholderFull = this.FindControl<TextBlock>("PlaceholderFull");
    }

    private void SetupEditor()
    {
        if (_editor == null) return;

        // Subscribe to text changes
        _editor.PropertyChanged += (_, e) =>
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
        _renderTimer.Tick += async (_, _) =>
        {
            _renderTimer?.Stop();
            if (DataContext is DocumentViewModel vm && _editor != null)
            {
                var text = _editor.Text ?? string.Empty;
                await vm.PreviewViewModel.UpdatePreviewAsync(text);
                UpdatePreview(text, vm.IsReadMode);
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

    if (DataContext is DocumentViewModel vm)
        {
            // Wire up editor to view model
            vm.EditorViewModel.TextInsertRequested += OnTextInsertRequested;

            // Subscribe to EditorViewModel.Text changes (for file loading)
            vm.EditorViewModel.PropertyChanged += (_, args) =>
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

            // Subscribe to read mode changes
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.IsReadMode) && _editor != null)
                {
                    UpdatePreview(_editor.Text ?? string.Empty, vm.IsReadMode);
                }
            };

            // Sync initial text if ViewModel has content, otherwise keep our starter text
            if (_editor != null)
            {
                if (!string.IsNullOrEmpty(vm.EditorViewModel.Text))
                {
                    _editor.Text = vm.EditorViewModel.Text;
                }
                ApplyLightTheme();

                // Trigger initial preview
                UpdatePreview(_editor.Text ?? string.Empty, vm.IsReadMode);

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

    private void ApplyLightTheme()
    {
        if (_editor == null) return;

        _editor.Background = Brushes.White;
        _editor.Foreground = Brushes.Black;

        if (_editorBorder != null)
        {
            _editorBorder.Background = Brushes.White;
            _editorBorder.BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
        }
        if (_previewBorderRight != null)
            _previewBorderRight.Background = Brushes.White;
        if (_previewBorderFull != null)
            _previewBorderFull.Background = Brushes.White;
        if (_splitter != null)
            _splitter.Background = new SolidColorBrush(Color.Parse("#E0E0E0"));
        if (_placeholderRight != null)
            _placeholderRight.Foreground = new SolidColorBrush(Color.Parse("#999999"));
        if (_placeholderFull != null)
            _placeholderFull.Foreground = new SolidColorBrush(Color.Parse("#999999"));
    }

    private void UpdatePreview(string markdownText, bool isReadMode)
    {
        var target = isReadMode ? _previewContentFull : _previewContentRight;
        if (target == null || markdownText == _lastRenderedText)
            return;

        _lastRenderedText = markdownText;
        target.Children.Clear();

        if (string.IsNullOrWhiteSpace(markdownText))
        {
            var placeholder = new SelectableTextBlock
            {
                Text = "Start typing markdown in the editor to see the preview...",
                Foreground = new SolidColorBrush(Color.Parse("#999999")),
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            target.Children.Add(placeholder);
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
            var element = RenderBlock(block);
            if (element != null)
            {
                target.Children.Add(element);
            }
        }
    }

    private Control? RenderBlock(Block block)
    {
        var textColor = Color.Parse("#333333");
        var mutedColor = Color.Parse("#666666");

        return block switch
        {
            HeadingBlock heading => RenderHeading(heading, textColor),
            ParagraphBlock paragraph => RenderParagraph(paragraph, textColor),
            CodeBlock codeBlock => RenderCodeBlock(codeBlock),
            ListBlock list => RenderList(list, textColor),
            QuoteBlock quote => RenderQuote(quote, textColor),
            ThematicBreakBlock => new Border
            {
                Height = 1,
                Background = new SolidColorBrush(mutedColor),
                Margin = new Thickness(0, 10, 0, 10)
            },
            _ => null
        };
    }

    private Control RenderHeading(HeadingBlock heading, Color textColor)
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

        var textBlock = new SelectableTextBlock
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
        var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        RenderInlines(panel, paragraph.Inline, textColor);
        return panel;
    }

    private void RenderInlines(WrapPanel panel, ContainerInline? inline, Color textColor)
    {
        if (inline == null) return;
        foreach (var item in inline)
        {
            switch (item)
            {
                case LiteralInline literal:
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = literal.Content.ToString(),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor)
                    });
                    break;
                case LinkInline link:
                    var linkButton = new HyperlinkButton
                    {
                        Content = ExtractText(link),
                        NavigateUri = new Uri(link.Url ?? ""),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.Parse("#0066CC")),
                        Margin = new Thickness(0)
                    };
                    panel.Children.Add(linkButton);
                    break;
                case EmphasisInline emphasis:
                    var empText = ExtractText(emphasis);
                    var empBlock = new SelectableTextBlock
                    {
                        Text = empText,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor)
                    };
                    if (emphasis.DelimiterCount == 2 && emphasis.DelimiterChar == '*')
                        empBlock.FontWeight = FontWeight.Bold;
                    else if (emphasis.DelimiterCount == 1)
                        empBlock.FontStyle = FontStyle.Italic;
                    panel.Children.Add(empBlock);
                    break;
                case CodeInline code:
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = $"`{code.Content}`",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        FontFamily = new FontFamily("Consolas,Courier New,monospace")
                    });
                    break;
                case LineBreakInline:
                    // Add a line break as a new SelectableTextBlock with newline
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = "\n",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor)
                    });
                    break;
                default:
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = item.ToString(),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor)
                    });
                    break;
            }
        }
    }

    private Control RenderCodeBlock(CodeBlock codeBlock)
    {
        var code = new StringBuilder();
        foreach (var line in codeBlock.Lines)
        {
            code.AppendLine(line.ToString());
        }

        var bgColor = Color.Parse("#F5F5F5");
        var textColor = Color.Parse("#333333");

        return new Border
        {
            Background = new SolidColorBrush(bgColor),
            BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Child = new SelectableTextBlock
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
                    itemPanel.Children.Add(new SelectableTextBlock
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

    private Control RenderQuote(QuoteBlock quote, Color textColor)
    {
        var panel = new StackPanel { Spacing = 4 };

        foreach (var block in quote)
        {
            if (block is ParagraphBlock para)
            {
                var text = ExtractText(para.Inline);
                panel.Children.Add(new SelectableTextBlock
                {
                    Text = text,
                    FontSize = 14,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(textColor),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        var borderColor = Color.Parse("#CCCCCC");
        var bgColor = Color.Parse("#F9F9F9");

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
