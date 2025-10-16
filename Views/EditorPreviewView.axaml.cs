using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Input;
using MarkdownViewer.ViewModels;
using MarkdownViewer.Controls;
using MarkdownViewer.Services;
using AvRichTextBox;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Abbreviations;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Figures;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

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
    private RichTextBox? _previewRichTextBox;
    private DispatcherTimer? _renderTimer;
    private string _lastRenderedText = string.Empty;
    private MarkdownToFlowDocumentConverter? _markdownConverter;

    public EditorPreviewView()
    {
        InitializeComponent();
        SetupEditor();
        SetupPreview();
        SetupKeyboardHandling();
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
        _previewRichTextBox = this.FindControl<RichTextBox>("PreviewRichTextBox");
    }

    private void SetupEditor()
    {
        if (_editor == null) return;

        // Subscribe to text changes
        _editor.TextChanged += (_, text) =>
        {
            OnEditorTextChanged();
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

    private void SetupKeyboardHandling()
    {
        // Handle keyboard shortcuts for the preview panels
        this.KeyDown += OnPreviewKeyDown;
    }

    private async void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // Check for Ctrl+A (Select All) - copy all text to clipboard
        if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
        {
            // Extract all rendered text from the preview with proper formatting
            if (DataContext is DocumentViewModel vm)
            {
                var allRenderedText = ExtractAllRenderedText(vm.IsReadMode);
                if (!string.IsNullOrEmpty(allRenderedText))
                {
                    // Copy to clipboard
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(allRenderedText);

                        // Show a brief notification that text was copied
                        System.Diagnostics.Debug.WriteLine($"Copied {allRenderedText.Length} characters to clipboard");
                    }
                    e.Handled = true;
                }
            }
        }
    }

    private string ExtractAllRenderedText(bool isReadMode)
    {
        var target = isReadMode ? _previewContentFull : _previewContentRight;
        if (target == null) return string.Empty;

        var result = new StringBuilder();

        foreach (var child in target.Children)
        {
            ExtractTextFromControl(child, result);
        }

        return result.ToString();
    }

    private void ExtractTextFromControl(Control control, StringBuilder result)
    {
        switch (control)
        {
            case SelectableTextBlock textBlock:
                if (!string.IsNullOrWhiteSpace(textBlock.Text))
                {
                    result.AppendLine(textBlock.Text);
                }
                break;

            case TextBlock regularTextBlock:
                if (!string.IsNullOrWhiteSpace(regularTextBlock.Text))
                {
                    result.AppendLine(regularTextBlock.Text);
                }
                break;

            case Panel panel:
                foreach (var child in panel.Children)
                {
                    ExtractTextFromControl(child, result);
                }
                break;

            case Border border when border.Child != null:
                ExtractTextFromControl(border.Child, result);
                break;

            case ContentControl contentControl when contentControl.Content is Control contentAsControl:
                ExtractTextFromControl(contentAsControl, result);
                break;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is DocumentViewModel vm)
        {
            // Create markdown converter with Markdig pipeline
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .UseEmphasisExtras()
                .Build();
            _markdownConverter = new MarkdownToFlowDocumentConverter(pipeline);

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
        _lastRenderedText = markdownText;

        // Use RichTextBox for read mode, native rendering for edit mode
        if (isReadMode && _previewRichTextBox != null && _markdownConverter != null)
        {
            // RichTextBox rendering for read mode
            var flowDoc = _markdownConverter.Convert(markdownText, isDarkTheme);
            _previewRichTextBox.FlowDocument = flowDoc;
            return;
        }

        // Native rendering for edit mode (right pane)
        var target = isReadMode ? _previewContentFull : _previewContentRight;
        if (target == null)
            return;

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

        // Parse markdown using Markdig with ALL extensions enabled
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseAutoLinks()
            .UseGenericAttributes()
            .UseDefinitionLists()
            .UseFootnotes()
            .UseAbbreviations()
            .UsePipeTables()
            .UseGridTables()
            .UseTaskLists()
            .UseAutoIdentifiers()
            .UseMediaLinks()
            .UseSmartyPants()
            .UseMathematics()
            .UseDiagrams()
            .UseYamlFrontMatter()
            .UseEmphasisExtras()
            .UseCustomContainers()
            .UseFigures()
            .UseCitations()
            .Build();

        var document = Markdown.Parse(markdownText, pipeline);

        // Render each block element with rich formatting
        foreach (var block in document)
        {
            var element = RenderBlock(block, isDarkTheme);
            if (element != null)
            {
                target.Children.Add(element);
            }
        }
    }

    private Control? RenderBlock(Markdig.Syntax.Block block, bool isDarkTheme)
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
            DefinitionList defList => RenderDefinitionList(defList, textColor, mutedColor),
            Figure figure => RenderFigure(figure, textColor, mutedColor),
            FootnoteGroup footnoteGroup => RenderFootnoteGroup(footnoteGroup, textColor, isDarkTheme),
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

        // Use SelectableTextBlock with Inlines for proper multi-line text selection
        var textBlock = new SelectableTextBlock
        {
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(textColor)
        };

        RenderInlinesToSelectableTextBlock(textBlock, paragraph.Inline, textColor);
        return textBlock;
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

    // New method for rendering inlines to SelectableTextBlock.Inlines (supports multi-line selection)
    private void RenderInlinesToSelectableTextBlock(SelectableTextBlock textBlock, ContainerInline? inline, Color textColor)
    {
        if (inline == null) return;

        foreach (var item in inline)
        {
            switch (item)
            {
                case LiteralInline literal:
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = literal.Content.ToString()
                    });
                    break;

                case LinkInline link when link.IsImage:
                    // For inline images, we need to fall back to WrapPanel approach
                    // Skip inline images in TextBlock - they're handled at paragraph level
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = $"[Image: {link.Url ?? "No URL"}]",
                        FontStyle = FontStyle.Italic,
                        Foreground = new SolidColorBrush(Color.Parse("#888888"))
                    });
                    break;

                case LinkInline link:
                    // For links, we can use InlineUIContainer with HyperlinkButton
                    // But for better text selection, let's use styled Run with URL
                    var linkText = ExtractText(link);
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = linkText,
                        Foreground = new SolidColorBrush(Color.Parse("#0066CC")),
                        TextDecorations = Avalonia.Media.TextDecorations.Underline
                    });
                    break;

                // Special emphasis cases MUST come before general EmphasisInline case
                case EmphasisInline emphasis when emphasis.DelimiterChar == '=' && emphasis.DelimiterCount == 2:
                    // Mark/highlight text
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = ExtractText(emphasis),
                        Background = new SolidColorBrush(Color.Parse("#FFF3CD")),
                        Foreground = Brushes.Black
                    });
                    break;

                case EmphasisInline emphasis when emphasis.DelimiterChar == '+' && emphasis.DelimiterCount == 2:
                    // Inserted text (underline)
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = ExtractText(emphasis),
                        TextDecorations = Avalonia.Media.TextDecorations.Underline
                    });
                    break;

                case EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2:
                    // Strikethrough
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = ExtractText(emphasis),
                        TextDecorations = Avalonia.Media.TextDecorations.Strikethrough
                    });
                    break;

                case EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 1:
                    // Subscript
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 11,
                        BaselineAlignment = Avalonia.Media.BaselineAlignment.Subscript
                    });
                    break;

                case EmphasisInline emphasis when emphasis.DelimiterChar == '^' && emphasis.DelimiterCount == 1:
                    // Superscript
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 11,
                        BaselineAlignment = Avalonia.Media.BaselineAlignment.Superscript
                    });
                    break;

                case EmphasisInline emphasis:
                    // General bold/italic case
                    var run = new Avalonia.Controls.Documents.Run
                    {
                        Text = ExtractText(emphasis)
                    };
                    if (emphasis.DelimiterCount == 2 && emphasis.DelimiterChar == '*')
                        run.FontWeight = FontWeight.Bold;
                    else if (emphasis.DelimiterCount == 1)
                        run.FontStyle = FontStyle.Italic;
                    textBlock.Inlines?.Add(run);
                    break;

                case CodeInline code:
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = $"`{code.Content}`",
                        FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                        Background = new SolidColorBrush(Color.Parse("#F5F5F5"))
                    });
                    break;

                case LineBreakInline:
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.LineBreak());
                    break;

                case FootnoteLink footnoteLink:
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = $"[{footnoteLink.Index + 1}]",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.Parse("#0066CC")),
                        BaselineAlignment = Avalonia.Media.BaselineAlignment.Superscript
                    });
                    break;

                case AbbreviationInline abbreviation:
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = abbreviation.Abbreviation?.Label ?? abbreviation.ToString(),
                        TextDecorations = Avalonia.Media.TextDecorations.Underline
                    });
                    break;

                case MathInline mathInline:
                    textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                    {
                        Text = $"${mathInline.Content}$",
                        Foreground = new SolidColorBrush(Color.Parse("#8B008B")),
                        FontFamily = new FontFamily("Consolas,Courier New,monospace")
                    });
                    break;

                case ContainerInline container:
                    // Recursively render container inlines
                    RenderInlinesToSelectableTextBlock(textBlock, container, textColor);
                    break;

                default:
                    // Fallback - try to extract text
                    var text = item.ToString();
                    if (!string.IsNullOrWhiteSpace(text) && text != item.GetType().Name)
                    {
                        textBlock.Inlines?.Add(new Avalonia.Controls.Documents.Run
                        {
                            Text = text
                        });
                    }
                    break;
            }
        }
    }

    // Old method kept for list items and other special cases that need WrapPanel
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
                    // Mark/highlight text
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 14,
                        Foreground = Brushes.Black,
                        Background = new SolidColorBrush(Color.Parse("#FFF3CD"))
                    });
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '+' && emphasis.DelimiterCount == 2:
                    // Inserted text (underline)
                    var insertedText = new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        TextDecorations = Avalonia.Media.TextDecorations.Underline
                    };
                    panel.Children.Add(insertedText);
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2:
                    // Strikethrough
                    var strikeText = new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        TextDecorations = Avalonia.Media.TextDecorations.Strikethrough
                    };
                    panel.Children.Add(strikeText);
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 1:
                    // Subscript
                    var subText = new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(textColor),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, -4)
                    };
                    panel.Children.Add(subText);
                    break;
                case EmphasisInline emphasis when emphasis.DelimiterChar == '^' && emphasis.DelimiterCount == 1:
                    // Superscript
                    var supText = new SelectableTextBlock
                    {
                        Text = ExtractText(emphasis),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(textColor),
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, -4, 0, 0)
                    };
                    panel.Children.Add(supText);
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
                    // Render abbreviation with underline - AbbreviationInline is not a ContainerInline
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = abbreviation.Abbreviation?.Label ?? abbreviation.ToString(),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor),
                        TextDecorations = Avalonia.Media.TextDecorations.Underline
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
            language = fencedBlock.Info?.Trim().ToLowerInvariant();
        }

        // Special handling for Mermaid diagrams
        if (language == "mermaid")
        {
            var mermaidPanel = new StackPanel { Spacing = 12 };

            // Header with icon and title
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 0, 0, 8)
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = "📊",
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = "Mermaid Diagram",
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(isDarkTheme ? Color.Parse("#4FC3F7") : Color.Parse("#0288D1")),
                VerticalAlignment = VerticalAlignment.Center
            });

            mermaidPanel.Children.Add(headerPanel);

            // Create visual preview of the diagram
            var preview = RenderMermaidPreview(code.ToString(), isDarkTheme);
            if (preview != null)
            {
                mermaidPanel.Children.Add(preview);
            }

            // Diagram code with syntax highlighting
            var codeBorder = new Border
            {
                Background = new SolidColorBrush(isDarkTheme ? Color.Parse("#1E1E1E") : Color.Parse("#F8F8F8")),
                BorderBrush = new SolidColorBrush(isDarkTheme ? Color.Parse("#3F3F3F") : Color.Parse("#DDDDDD")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Child = new SelectableTextBlock
                {
                    Text = code.ToString().TrimEnd(),
                    FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(isDarkTheme ? Color.Parse("#D4D4D4") : Color.Parse("#333333")),
                    TextWrapping = TextWrapping.Wrap
                }
            };

            mermaidPanel.Children.Add(codeBorder);

            // Info message
            var infoPanel = new Border
            {
                Background = new SolidColorBrush(isDarkTheme ? Color.Parse("#1A2332") : Color.Parse("#E3F2FD")),
                BorderBrush = new SolidColorBrush(isDarkTheme ? Color.Parse("#2196F3") : Color.Parse("#2196F3")),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = "💡 For full interactive rendering, export to HTML (File → Export to HTML).",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(isDarkTheme ? Color.Parse("#90CAF9") : Color.Parse("#1565C0")),
                    TextWrapping = TextWrapping.Wrap
                }
            };

            mermaidPanel.Children.Add(infoPanel);

            return new Border
            {
                Background = new SolidColorBrush(isDarkTheme ? Color.Parse("#2A2A2A") : Brushes.White.Color),
                BorderBrush = new SolidColorBrush(isDarkTheme ? Color.Parse("#424242") : Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16),
                Child = mermaidPanel
            };
        }

        // Use syntax highlighter if language is specified
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
                // Use SelectableTextBlock with Inlines to support bold/italic within quotes
                var textBlock = new SelectableTextBlock
                {
                    FontSize = 14,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(textColor),
                    TextWrapping = TextWrapping.Wrap
                };
                RenderInlinesToSelectableTextBlock(textBlock, para.Inline, textColor);
                panel.Children.Add(textBlock);
            }
            else if (block is QuoteBlock nestedQuote)
            {
                // Handle nested blockquotes
                panel.Children.Add(RenderQuote(nestedQuote, textColor, isDarkTheme));
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
                // Use the main ExtractText method which handles all inline types
                result.Append(ExtractText(para.Inline));
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
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => $"`{code.Content}`",
                EmphasisInline emphasis => ExtractEmphasisText(emphasis),
                LineBreakInline => "\n",
                LinkInline link => ExtractText(link as ContainerInline) + $" ({link.Url})",
                FootnoteLink footnoteLink => $"[{footnoteLink.Index + 1}]",
                AbbreviationInline abbreviation => abbreviation.Abbreviation?.Label ?? string.Empty,
                MathInline mathInline => $"${mathInline.Content}$",
                TaskList => string.Empty, // TaskList is a marker, skip it
                ContainerInline container => ExtractText(container),
                _ => item.GetType().Name == item.ToString() ? string.Empty : item.ToString() // Skip type names
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

    private Control RenderDefinitionList(DefinitionList defList, Color textColor, Color mutedColor)
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 0, 0, 16) };

        foreach (var item in defList)
        {
            if (item is DefinitionItem defItem)
            {
                // Render the term (dt)
                foreach (var term in defItem.OfType<ParagraphBlock>().Take(1))
                {
                    var termText = ExtractText(term.Inline);
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = termText,
                        FontSize = 14,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(textColor),
                        Margin = new Thickness(0, 8, 0, 4)
                    });
                }

                // Render definitions (dd)
                foreach (var def in defItem.OfType<ParagraphBlock>().Skip(1))
                {
                    var defText = ExtractText(def.Inline);
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = defText,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(mutedColor),
                        Margin = new Thickness(32, 0, 0, 4),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }
        }

        return panel;
    }

    private Control RenderFigure(Figure figure, Color textColor, Color mutedColor)
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 16), HorizontalAlignment = HorizontalAlignment.Center };

        foreach (var block in figure)
        {
            if (block is ParagraphBlock para)
            {
                if (para.Inline?.FirstChild is LinkInline { IsImage: true } imageLink)
                {
                    // Render the image
                    panel.Children.Add(RenderImage(imageLink));
                }
                else
                {
                    // Render caption text
                    var captionText = ExtractText(para.Inline);
                    if (!string.IsNullOrWhiteSpace(captionText))
                    {
                        panel.Children.Add(new SelectableTextBlock
                        {
                            Text = captionText,
                            FontSize = 13,
                            FontStyle = FontStyle.Italic,
                            Foreground = new SolidColorBrush(mutedColor),
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap
                        });
                    }
                }
            }
        }

        return panel;
    }

    private Control RenderFootnoteGroup(FootnoteGroup footnoteGroup, Color textColor, bool isDarkTheme)
    {
        var borderColor = isDarkTheme ? Color.Parse("#404040") : Color.Parse("#E0E0E0");
        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 24, 0, 0)
        };

        // Add separator
        panel.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(borderColor),
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Add "Footnotes" heading
        panel.Children.Add(new SelectableTextBlock
        {
            Text = "Footnotes",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(textColor),
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Render each footnote
        int index = 1;
        foreach (var footnote in footnoteGroup.OfType<Footnote>())
        {
            var footnotePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 8) };

            // Add footnote number
            footnotePanel.Children.Add(new SelectableTextBlock
            {
                Text = $"[{index}]",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#0066CC")),
                VerticalAlignment = VerticalAlignment.Top
            });

            // Add footnote content
            var contentPanel = new StackPanel { Spacing = 4 };
            foreach (var block in footnote)
            {
                if (block is ParagraphBlock para)
                {
                    var text = ExtractText(para.Inline);
                    contentPanel.Children.Add(new SelectableTextBlock
                    {
                        Text = text,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(textColor),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }
            footnotePanel.Children.Add(contentPanel);

            panel.Children.Add(footnotePanel);
            index++;
        }

        return panel;
    }

    private Control? RenderMermaidPreview(string mermaidCode, bool isDarkTheme)
    {
        // Parse the Mermaid code to create a simple visual preview
        var lines = mermaidCode.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0) return null;

        var previewPanel = new StackPanel { Spacing = 8 };
        var boxColor = isDarkTheme ? Color.Parse("#4A90E2") : Color.Parse("#2196F3");
        var textOnBoxColor = Brushes.White.Color;

        // Detect diagram type
        var diagramType = lines[0].ToLowerInvariant();

        if (diagramType.Contains("sequencediagram"))
        {
            // Render sequence diagram preview
            var participants = new List<string>();
            foreach (var line in lines.Skip(1))
            {
                if (line.ToLowerInvariant().Contains("participant"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        participants.Add(parts[1]);
                    }
                }
            }

            if (participants.Count > 0)
            {
                var participantsPanel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
                foreach (var participant in participants)
                {
                    var box = new Border
                    {
                        Background = new SolidColorBrush(boxColor),
                        BorderBrush = new SolidColorBrush(isDarkTheme ? Color.Parse("#5BA3F5") : Color.Parse("#1976D2")),
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(12, 6, 12, 6),
                        Margin = new Thickness(0, 0, 8, 0),
                        Child = new TextBlock
                        {
                            Text = participant,
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = new SolidColorBrush(textOnBoxColor)
                        }
                    };
                    participantsPanel.Children.Add(box);
                }
                previewPanel.Children.Add(new TextBlock
                {
                    Text = "▼ Sequence Diagram Preview",
                    FontSize = 13,
                    FontWeight = FontWeight.Medium,
                    Foreground = new SolidColorBrush(isDarkTheme ? Color.Parse("#90CAF9") : Color.Parse("#1565C0")),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                previewPanel.Children.Add(participantsPanel);
            }
        }
        else if (diagramType.Contains("graph") || diagramType.Contains("flowchart"))
        {
            // Render flowchart/graph preview
            var nodes = new List<string>();
            foreach (var line in lines.Skip(1))
            {
                // Extract node names (simplified parsing)
                var matches = System.Text.RegularExpressions.Regex.Matches(line, @"([A-Z][a-zA-Z0-9]*)\[([^\]]+)\]");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 2)
                    {
                        nodes.Add(match.Groups[2].Value);
                    }
                }
            }

            if (nodes.Count > 0)
            {
                var nodesPanel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
                foreach (var node in nodes.Take(6)) // Limit to 6 nodes for preview
                {
                    var box = new Border
                    {
                        Background = new SolidColorBrush(boxColor),
                        BorderBrush = new SolidColorBrush(isDarkTheme ? Color.Parse("#5BA3F5") : Color.Parse("#1976D2")),
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12, 6, 12, 6),
                        Margin = new Thickness(0, 0, 8, 0),
                        Child = new TextBlock
                        {
                            Text = node,
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = new SolidColorBrush(textOnBoxColor)
                        }
                    };
                    nodesPanel.Children.Add(box);
                }
                previewPanel.Children.Add(new TextBlock
                {
                    Text = "▼ Flow Diagram Preview",
                    FontSize = 13,
                    FontWeight = FontWeight.Medium,
                    Foreground = new SolidColorBrush(isDarkTheme ? Color.Parse("#90CAF9") : Color.Parse("#1565C0")),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                previewPanel.Children.Add(nodesPanel);
            }
        }

        if (previewPanel.Children.Count > 0)
        {
            return new Border
            {
                Background = new SolidColorBrush(isDarkTheme ? Color.FromArgb(40, 26, 35, 126) : Color.Parse("#E8EAF6")),
                BorderBrush = new SolidColorBrush(boxColor),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 8),
                Child = previewPanel
            };
        }

        return null;
    }
}
