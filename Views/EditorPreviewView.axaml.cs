using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Layout;
using MarkdownViewer.ViewModels;
using MarkdownViewer.Controls;
using MarkdownViewer.Services;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Abbreviations;
using Markdig.Extensions.Mathematics;
using System;
using System.Linq;
using System.Text;

namespace MarkdownViewer.Views;

public partial class EditorPreviewView : UserControl
{
    private LineNumberedTextEditor? _editor;
    private StackPanel? _previewContentRight;
    private StackPanel? _previewContentFull;
    private Border? _editorBorder;
    private Border? _previewBorderRight;
    private Border? _previewBorderFull;
    private GridSplitter? _splitter;
    private TextBlock? _placeholderRight;
    private TextBlock? _placeholderFull;
    private ScrollViewer? _previewScrollerRight;
    private ScrollViewer? _previewScrollerFull;
    private DispatcherTimer? _renderTimer;
    private string _lastRenderedText = string.Empty;
    private bool _isPreviewScrolling = false;

    public EditorPreviewView()
    {
        InitializeComponent();
        SetupEditor();
        SetupPreview();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _editor = this.FindControl<LineNumberedTextEditor>("MarkdownEditor");
        _previewContentRight = this.FindControl<StackPanel>("PreviewContentRight");
        _previewContentFull = this.FindControl<StackPanel>("PreviewContentFull");
        _editorBorder = this.FindControl<Border>("EditorBorder");
        _previewBorderRight = this.FindControl<Border>("PreviewBorderRight");
        _previewBorderFull = this.FindControl<Border>("PreviewBorderFull");
        _splitter = this.FindControl<GridSplitter>("Splitter");
        _placeholderRight = this.FindControl<TextBlock>("PlaceholderRight");
        _placeholderFull = this.FindControl<TextBlock>("PlaceholderFull");
        _previewScrollerRight = this.FindControl<ScrollViewer>("PreviewScrollerRight");
        _previewScrollerFull = this.FindControl<ScrollViewer>("PreviewScrollerFull");
    }

    private void SetupEditor()
    {
        if (_editor == null) return;

        // Subscribe to text changes
        _editor.TextChanged += (_, text) =>
        {
            OnEditorTextChanged();
        };

        // Subscribe to scroll changes for synchronized scrolling
        _editor.ScrollChanged += (_, scrollPercentage) =>
        {
            if (!_isPreviewScrolling && DataContext is DocumentViewModel vm)
            {
                var targetScroller = vm.IsReadMode ? _previewScrollerFull : _previewScrollerRight;
                if (targetScroller != null && targetScroller.Extent.Height > 0)
                {
                    _isPreviewScrolling = true;
                    var targetOffset = scrollPercentage * targetScroller.Extent.Height;
                    targetScroller.Offset = new Vector(targetScroller.Offset.X, targetOffset);
                    Dispatcher.UIThread.Post(() => _isPreviewScrolling = false, DispatcherPriority.Background);
                }
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
                UpdatePreview(text, vm.IsReadMode, vm.IsDarkTheme);
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
                    UpdatePreview(_editor.Text ?? string.Empty, vm.IsReadMode, vm.IsDarkTheme);
                }
                else if (args.PropertyName == nameof(vm.IsDarkTheme))
                {
                    // Apply theme to editor and preview
                    if (vm.IsDarkTheme)
                        ApplyDarkTheme();
                    else
                        ApplyLightTheme();

                    if (_editor != null)
                        UpdatePreview(_editor.Text ?? string.Empty, vm.IsReadMode, vm.IsDarkTheme);
                }
            };

            // Sync initial text if ViewModel has content, otherwise keep our starter text
            if (_editor != null)
            {
                if (!string.IsNullOrEmpty(vm.EditorViewModel.Text))
                {
                    _editor.Text = vm.EditorViewModel.Text;
                }

                // Apply appropriate theme
                if (vm.IsDarkTheme)
                    ApplyDarkTheme();
                else
                    ApplyLightTheme();

                // Trigger initial preview
                UpdatePreview(_editor.Text ?? string.Empty, vm.IsReadMode, vm.IsDarkTheme);

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

        var caretIndex = _editor.CaretIndex;
        _editor.InsertText(text, caretIndex);
        _editor.Focus();
    }

    private void ApplyLightTheme()
    {
        // Apply theme to editor
        _editor?.ApplyLightTheme();

        if (_editorBorder != null)
            _editorBorder.BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
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

    public void ApplyDarkTheme()
    {
        // Apply theme to editor
        _editor?.ApplyDarkTheme();

        if (_editorBorder != null)
            _editorBorder.BorderBrush = new SolidColorBrush(Color.Parse("#C0C0C0"));
        // Preview can be dark in dark mode
        if (_previewBorderRight != null)
            _previewBorderRight.Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
        if (_previewBorderFull != null)
            _previewBorderFull.Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
        if (_splitter != null)
            _splitter.Background = new SolidColorBrush(Color.Parse("#404040"));
        if (_placeholderRight != null)
            _placeholderRight.Foreground = new SolidColorBrush(Color.Parse("#999999"));
        if (_placeholderFull != null)
            _placeholderFull.Foreground = new SolidColorBrush(Color.Parse("#999999"));
    }

    private void UpdatePreview(string markdownText, bool isReadMode, bool isDarkTheme = false)
    {
        var target = isReadMode ? _previewContentFull : _previewContentRight;
        if (target == null)
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
            .UseDiagrams()
            .Build();

        var document = Markdown.Parse(markdownText, pipeline);

        // Render each block element
        foreach (var block in document)
        {
            var element = RenderBlock(block, isDarkTheme);
            if (element != null)
            {
                target.Children.Add(element);
            }
        }
    }

    private Control? RenderBlock(Block block, bool isDarkTheme)
    {
        var textColor = isDarkTheme ? Color.Parse("#E0E0E0") : Color.Parse("#333333");
        var mutedColor = isDarkTheme ? Color.Parse("#888888") : Color.Parse("#666666");

        return block switch
        {
            HeadingBlock heading => RenderHeading(heading, textColor, isDarkTheme),
            ParagraphBlock paragraph => RenderParagraph(paragraph, textColor),
            CodeBlock codeBlock => RenderCodeBlock(codeBlock, isDarkTheme),
            ListBlock list => RenderList(list, textColor, isDarkTheme),
            QuoteBlock quote => RenderQuote(quote, textColor, isDarkTheme),
            ThematicBreakBlock => new Border
            {
                Height = 1,
                Background = new SolidColorBrush(mutedColor),
                Margin = new Thickness(0, 10, 0, 10)
            },
            Markdig.Extensions.Tables.Table table => RenderTable(table, textColor, isDarkTheme),
            _ => null
        };
    }

    private Control RenderHeading(HeadingBlock heading, Color textColor, bool isDarkTheme)
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

        if (heading.Level == 1 || heading.Level == 2)
        {
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(textBlock);
            panel.Children.Add(new Border
            {
                Height = heading.Level == 1 ? 2 : 1,
                Background = new SolidColorBrush(isDarkTheme ? Color.Parse("#404040") : Color.Parse("#E0E0E0")),
                Margin = new Thickness(0, 0, 0, 8)
            });
            return panel;
        }

        return textBlock;
    }

    private Control RenderParagraph(ParagraphBlock paragraph, Color textColor)
    {
        // Check if this paragraph contains only an image
        if (paragraph.Inline?.FirstChild is LinkInline { IsImage: true } singleImage && paragraph.Inline.Count() == 1)
        {
            return RenderImage(singleImage);
        }

        var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        RenderInlines(panel, paragraph.Inline, textColor);
        return panel;
    }

    private Control RenderImage(LinkInline imageLink)
    {
        if (string.IsNullOrEmpty(imageLink.Url))
        {
            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = "🖼️ Image: (No URL provided)",
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(Color.Parse("#888888"))
                }
            };
        }

        try
        {
            var image = new Avalonia.Controls.Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(imageLink.Url),
                Margin = new Thickness(0, 8, 0, 8),
                MaxWidth = 800,
                Stretch = Avalonia.Media.Stretch.Uniform
            };

            var border = new Border
            {
                Child = image,
                Margin = new Thickness(0, 0, 0, 10)
            };

            if (!string.IsNullOrEmpty(imageLink.Title))
            {
                var stack = new StackPanel { Spacing = 4 };
                stack.Children.Add(border);
                stack.Children.Add(new TextBlock
                {
                    Text = imageLink.Title,
                    FontStyle = FontStyle.Italic,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#888888")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return stack;
            }

            return border;
        }
        catch
        {
            // If image fails to load, show placeholder
            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = $"🖼️ Image: {imageLink.Url}\n(Unable to load)",
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(Color.Parse("#888888"))
                }
            };
        }
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
                case LinkInline link when link.IsImage:
                    // Inline images render as small images
                    if (!string.IsNullOrEmpty(link.Url))
                    {
                        try
                        {
                            var inlineImage = new Avalonia.Controls.Image
                            {
                                Source = new Avalonia.Media.Imaging.Bitmap(link.Url),
                                Height = 24,
                                Margin = new Thickness(0, 0, 4, 0),
                                Stretch = Avalonia.Media.Stretch.Uniform,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            panel.Children.Add(inlineImage);
                        }
                        catch
                        {
                            panel.Children.Add(new SelectableTextBlock
                            {
                                Text = $"[Image: {link.Url}]",
                                FontSize = 14,
                                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                                FontStyle = FontStyle.Italic
                            });
                        }
                    }
                    else
                    {
                        panel.Children.Add(new SelectableTextBlock
                        {
                            Text = "[Image: No URL]",
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.Parse("#888888")),
                            FontStyle = FontStyle.Italic
                        });
                    }
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
                // Special emphasis cases MUST come before general EmphasisInline case
                case EmphasisInline emphasis when emphasis.DelimiterChar == '=' && emphasis.DelimiterCount == 2:
                    // Mark/highlight text (==text==)
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 14,
                        Foreground = Brushes.Black,
                        Background = new SolidColorBrush(Color.Parse("#FFF3CD"))
                    });
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '+' && emphasis.DelimiterCount == 2:
                    // Inserted text - underline (++text++)
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        TextDecorations = TextDecorations.Underline
                    });
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2:
                    // Strikethrough (~~text~~)
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        TextDecorations = TextDecorations.Strikethrough
                    });
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 1:
                    // Subscript (~text~)
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(textColor),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, -4)
                    });
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '^' && emphasis.DelimiterCount == 1:
                    // Superscript (^text^)
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(textColor),
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, -4, 0, 0)
                    });
                    break;
                case EmphasisInline emphasis:
                    // General bold/italic case
                    var empText = ExtractText(emphasis);
                    var empBlock = new SelectableTextBlock
                    {
                        Text = empText,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor)
                    };
                    // Handle bold (**text** or __text__)
                    if (emphasis.DelimiterCount == 2 && (emphasis.DelimiterChar == '*' || emphasis.DelimiterChar == '_'))
                        empBlock.FontWeight = FontWeight.Bold;
                    // Handle italic (*text* or _text_)
                    else if (emphasis.DelimiterCount == 1 && (emphasis.DelimiterChar == '*' || emphasis.DelimiterChar == '_'))
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
                case FootnoteLink footnoteLink:
                    // Render footnote reference
                    var fnLink = new HyperlinkButton
                    {
                        Content = $"[{footnoteLink.Index + 1}]",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.Parse("#0066CC")),
                        Padding = new Thickness(0),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    panel.Children.Add(fnLink);
                    break;
                case AbbreviationInline abbreviation:
                    // Render abbreviation with underline
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = abbreviation.Abbreviation?.Label ?? abbreviation.ToString(),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        TextDecorations = TextDecorations.Underline
                    });
                    break;
                case MathInline mathInline:
                    // Render inline math with $ delimiters for visual indication
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = $"${mathInline.Content}$",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.Parse("#8B008B")),
                        FontFamily = new FontFamily("Consolas,Courier New,monospace")
                    });
                    break;
                case ContainerInline container:
                    // Recursively render container inlines
                    RenderInlines(panel, container, textColor);
                    break;
                default:
                    // Fallback - try to extract text
                    var text = item.ToString();
                    if (!string.IsNullOrWhiteSpace(text) && text != item.GetType().Name)
                    {
                        panel.Children.Add(new SelectableTextBlock
                        {
                            Text = text,
                            FontSize = 14,
                            Foreground = new SolidColorBrush(textColor)
                        });
                    }
                    break;
            }
        }
    }

    private Control RenderCodeBlock(CodeBlock codeBlock, bool isDarkTheme)
    {
        var code = new StringBuilder();
        foreach (var line in codeBlock.Lines)
        {
            code.AppendLine(line.ToString());
        }

        var bgColor = isDarkTheme ? Color.Parse("#2D2D2D") : Color.Parse("#F5F5F5");
        var borderColor = isDarkTheme ? Color.Parse("#404040") : Color.Parse("#E0E0E0");

        // Get language from fence code block info
        string? language = null;
        if (codeBlock is Markdig.Syntax.FencedCodeBlock fencedBlock)
        {
            language = fencedBlock.Info;
        }

        // Handle special code block types
        if (language?.ToLower() == "mermaid")
        {
            // Render Mermaid diagram placeholder with the code
            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#E8F4F8")),
                BorderBrush = new SolidColorBrush(Color.Parse("#4A90E2")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "📊 Mermaid Diagram",
                            FontSize = 16,
                            FontWeight = FontWeight.Bold,
                            Foreground = new SolidColorBrush(Color.Parse("#4A90E2")),
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        new TextBlock
                        {
                            Text = "Mermaid diagrams require HTML rendering. The diagram code is:",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.Parse("#666666")),
                            Margin = new Thickness(0, 0, 0, 8),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new SelectableTextBlock
                        {
                            Text = code.ToString().TrimEnd(),
                            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.Parse("#333333")),
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            };
        }
        else if (language?.ToLower() == "math" || language?.ToLower() == "latex")
        {
            // Render math block with special styling
            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#F5EEF8")),
                BorderBrush = new SolidColorBrush(Color.Parse("#8B008B")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "📐 Mathematical Expression",
                            FontSize = 16,
                            FontWeight = FontWeight.Bold,
                            Foreground = new SolidColorBrush(Color.Parse("#8B008B")),
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        new SelectableTextBlock
                        {
                            Text = code.ToString().TrimEnd(),
                            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.Parse("#8B008B")),
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            };
        }

        // Use syntax highlighter for regular code blocks
        var content = SyntaxHighlighter.CreateHighlightedBlock(code.ToString().TrimEnd(), language, isDarkTheme);

        return new Border
        {
            Background = new SolidColorBrush(bgColor),
            BorderBrush = new SolidColorBrush(borderColor),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Child = content
        };
    }

    private Control RenderList(ListBlock list, Color textColor, bool isDarkTheme)
    {
        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 10) };

        int itemNumber = 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            // Check if this is a task list item
            var taskListItem = item.Descendants<TaskList>().FirstOrDefault();

            foreach (var block in item)
            {
                if (block is ParagraphBlock para)
                {
                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 0, 0, 0) };

                    // If it's a task list item, render checkbox with colors
                    if (taskListItem != null)
                    {
                        var isChecked = taskListItem.Checked;
                        var checkboxText = isChecked ? "✓" : "✗";
                        var checkboxColor = isChecked ? Color.Parse("#22C55E") : Color.Parse("#EF4444"); // Green for checked, red for unchecked

                        // Make checkbox clickable
                        var checkboxButton = new Button
                        {
                            Content = new TextBlock
                            {
                                Text = checkboxText,
                                FontSize = 16,
                                FontWeight = FontWeight.Bold,
                                Foreground = new SolidColorBrush(checkboxColor)
                            },
                            Background = Brushes.Transparent,
                            BorderThickness = new Thickness(0),
                            Padding = new Thickness(0),
                            Margin = new Thickness(0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                        };

                        // Store the task list item for later updates
                        checkboxButton.Tag = new { TaskList = taskListItem, ParagraphBlock = para };
                        checkboxButton.Click += OnCheckboxClicked;

                        itemPanel.Children.Add(checkboxButton);

                        // Extract text without the checkbox markdown
                        var text = ExtractText(para.Inline);
                        itemPanel.Children.Add(new SelectableTextBlock
                        {
                            Text = text,
                            FontSize = 14,
                            Foreground = new SolidColorBrush(textColor),
                            TextWrapping = TextWrapping.Wrap,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    }
                    else
                    {
                        // Regular list item
                        var text = ExtractText(para.Inline);
                        var prefix = list.IsOrdered ? $"{itemNumber}. " : "• ";

                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = prefix,
                            FontSize = 14,
                            Foreground = new SolidColorBrush(textColor),
                            Margin = new Thickness(0, 0, 8, 0)
                        });
                        itemPanel.Children.Add(new SelectableTextBlock
                        {
                            Text = text,
                            FontSize = 14,
                            Foreground = new SolidColorBrush(textColor),
                            TextWrapping = TextWrapping.Wrap
                        });
                    }

                    panel.Children.Add(itemPanel);
                }
            }
            if (list.IsOrdered) itemNumber++;
        }

        return panel;
    }

    private void OnCheckboxClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag == null || DataContext is not DocumentViewModel vm || _editor == null)
            return;

        dynamic tag = button.Tag;
        TaskList taskList = tag.TaskList;

        // Toggle the checkbox state
        var currentText = _editor.Text ?? string.Empty;
        var pattern = taskList.Checked ? @"\[x\]" : @"\[ \]";
        var replacement = taskList.Checked ? "[ ]" : "[x]";

        // Find and replace the first occurrence (simple implementation)
        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var newText = regex.Replace(currentText, replacement, 1);

        _editor.Text = newText;
        vm.EditorViewModel.Text = newText;
    }

    private Control RenderQuote(QuoteBlock quote, Color textColor, bool isDarkTheme)
    {
        var panel = new StackPanel { Spacing = 4 };

        foreach (var block in quote)
        {
            if (block is ParagraphBlock para)
            {
                // Use WrapPanel to properly render inline formatting
                var wrapPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
                RenderInlines(wrapPanel, para.Inline, textColor);
                panel.Children.Add(wrapPanel);
            }
            else if (block is CodeBlock codeBlock)
            {
                // Render code blocks inside quotes
                panel.Children.Add(RenderCodeBlock(codeBlock, isDarkTheme));
            }
            else if (block is ListBlock listBlock)
            {
                // Render lists inside quotes
                panel.Children.Add(RenderList(listBlock, textColor, isDarkTheme));
            }
        }

        var borderColor = isDarkTheme ? Color.Parse("#505050") : Color.Parse("#CCCCCC");
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

    private Control RenderTable(Markdig.Extensions.Tables.Table table, Color textColor, bool isDarkTheme)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 16),
            RowDefinitions = new RowDefinitions(),
            ColumnDefinitions = new ColumnDefinitions()
        };

        var borderColor = isDarkTheme ? Color.Parse("#404040") : Color.Parse("#E0E0E0");
        var headerBg = isDarkTheme ? Color.Parse("#2D2D2D") : Color.Parse("#F5F5F5");
        var evenRowBg = isDarkTheme ? Color.Parse("#252525") : Color.Parse("#FAFAFA");
        var oddRowBg = isDarkTheme ? Color.Parse("#1E1E1E") : Brushes.White.Color;

        // Count columns from first row
        var firstRow = table.FirstOrDefault() as Markdig.Extensions.Tables.TableRow;
        if (firstRow == null) return new Border();

        int columnCount = firstRow.Count;
        for (int i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        int rowIndex = 0;
        bool isHeader = true;

        foreach (var row in table.OfType<Markdig.Extensions.Tables.TableRow>())
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            int colIndex = 0;
            foreach (var cell in row.OfType<Markdig.Extensions.Tables.TableCell>())
            {
                var cellText = ExtractTableCellText(cell);
                var cellBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(borderColor),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 6, 8, 6),
                    Background = new SolidColorBrush(isHeader ? headerBg : (rowIndex % 2 == 0 ? evenRowBg : oddRowBg)),
                    Child = new SelectableTextBlock
                    {
                        Text = cellText,
                        FontSize = 14,
                        FontWeight = isHeader ? FontWeight.Bold : FontWeight.Normal,
                        Foreground = new SolidColorBrush(textColor),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                Grid.SetRow(cellBorder, rowIndex);
                Grid.SetColumn(cellBorder, colIndex);
                grid.Children.Add(cellBorder);

                colIndex++;
            }

            rowIndex++;
            isHeader = false; // Only first row is header
        }

        return grid;
    }

    private string ExtractTableCellText(Markdig.Extensions.Tables.TableCell cell)
    {
        var result = new StringBuilder();
        // TableCell contains blocks, typically a single ParagraphBlock
        foreach (var block in cell)
        {
            if (block is ParagraphBlock para && para.Inline != null)
            {
                foreach (var inline in para.Inline)
                {
                    if (inline is LiteralInline literal)
                    {
                        result.Append(literal.Content.ToString());
                    }
                    else if (inline is EmphasisInline emphasis)
                    {
                        var emphasisText = ExtractText(emphasis);
                        // Add markdown formatting markers for visual representation
                        if (emphasis.DelimiterCount == 2 && (emphasis.DelimiterChar == '*' || emphasis.DelimiterChar == '_'))
                            result.Append($"**{emphasisText}**");
                        else if (emphasis.DelimiterCount == 1 && (emphasis.DelimiterChar == '*' || emphasis.DelimiterChar == '_'))
                            result.Append($"*{emphasisText}*");
                        else if (emphasis.DelimiterChar == '~')
                            result.Append($"~~{emphasisText}~~");
                        else
                            result.Append(emphasisText);
                    }
                    else if (inline is CodeInline code)
                    {
                        result.Append($"`{code.Content}`");
                    }
                    else if (inline is LinkInline link)
                    {
                        result.Append(ExtractText(link));
                    }
                    else
                    {
                        result.Append(inline.ToString());
                    }
                }
            }
        }
        return result.ToString();
    }

    private string ExtractText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline == null) return string.Empty;

        var result = new StringBuilder();
        foreach (var item in inline)
        {
            result.Append(item switch
            {
                TaskList => string.Empty, // Skip task list markers
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => $"`{code.Content}`",
                EmphasisInline emphasis => ExtractEmphasisText(emphasis),
                MathInline math => $"${math.Content}$",  // Include math inline
                FootnoteLink footnote => $"[{footnote.Index + 1}]",  // Include footnote refs
                LineBreakInline => "\n",
                LinkInline link => ExtractText(link as ContainerInline),
                ContainerInline container => ExtractText(container), // Recursively handle containers
                _ => string.Empty  // Skip unknown types instead of calling ToString()
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
