using Avalonia;
using Avalonia.Media;
using AvRichTextBox;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;
using System.Linq;

namespace MarkdownViewer.Services;

public class MarkdownToFlowDocumentConverter
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownToFlowDocumentConverter(MarkdownPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public FlowDocument Convert(string markdownText, bool isDarkTheme)
    {
        var flowDoc = new FlowDocument();

        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return flowDoc;
        }

        var document = Markdown.Parse(markdownText, _pipeline);
        var textColor = isDarkTheme ? Color.Parse("#E0E0E0") : Color.Parse("#333333");
        var mutedColor = isDarkTheme ? Color.Parse("#888888") : Color.Parse("#666666");

        foreach (var block in document)
        {
            var paragraph = ConvertBlock(block, textColor, mutedColor, isDarkTheme);
            if (paragraph != null)
            {
                flowDoc.Blocks.Add(paragraph);
            }
        }

        return flowDoc;
    }

    private Paragraph? ConvertBlock(Markdig.Syntax.Block block, Color textColor, Color mutedColor, bool isDarkTheme)
    {
        return block switch
        {
            HeadingBlock heading => ConvertHeading(heading, textColor, isDarkTheme),
            ParagraphBlock paragraph => ConvertParagraph(paragraph, textColor),
            CodeBlock codeBlock => ConvertCodeBlock(codeBlock, textColor, isDarkTheme),
            ListBlock list => ConvertList(list, textColor, isDarkTheme),
            QuoteBlock quote => ConvertQuote(quote, textColor, mutedColor, isDarkTheme),
            _ => null
        };
    }

    private Paragraph ConvertHeading(HeadingBlock heading, Color textColor, bool isDarkTheme)
    {
        var paragraph = new Paragraph();
        var fontSize = heading.Level switch
        {
            1 => 32.0,
            2 => 28.0,
            3 => 24.0,
            4 => 20.0,
            5 => 18.0,
            _ => 16.0
        };

        var runs = ConvertInlines(heading.Inline, textColor);
        foreach (var run in runs)
        {
            run.FontSize = fontSize;
            run.FontWeight = FontWeight.Bold;
            paragraph.Inlines.Add(run);
        }

        return paragraph;
    }

    private Paragraph ConvertParagraph(ParagraphBlock paragraphBlock, Color textColor)
    {
        var para = new Paragraph();
        var runs = ConvertInlines(paragraphBlock.Inline, textColor);

        foreach (var run in runs)
        {
            para.Inlines.Add(run);
        }

        return para;
    }

    private Paragraph ConvertCodeBlock(CodeBlock codeBlock, Color textColor, bool isDarkTheme)
    {
        var paragraph = new Paragraph();
        var bgColor = isDarkTheme ? Color.Parse("#2D2D2D") : Color.Parse("#F5F5F5");

        foreach (var line in codeBlock.Lines)
        {
            var run = new EditableRun(line.ToString() + "\n")
            {
                FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                FontSize = 13,
                Foreground = new SolidColorBrush(textColor),
                Background = new SolidColorBrush(bgColor)
            };
            paragraph.Inlines.Add(run);
        }

        return paragraph;
    }

    private Paragraph ConvertList(ListBlock list, Color textColor, bool isDarkTheme)
    {
        var paragraph = new Paragraph();
        int itemNumber = 1;

        foreach (var item in list.OfType<ListItemBlock>())
        {
            foreach (var itemBlock in item)
            {
                if (itemBlock is ParagraphBlock para)
                {
                    var prefix = list.IsOrdered ? $"{itemNumber}. " : "• ";
                    var prefixRun = new EditableRun(prefix)
                    {
                        FontSize = 14,
                        Foreground = new SolidColorBrush(textColor)
                    };
                    paragraph.Inlines.Add(prefixRun);

                    var runs = ConvertInlines(para.Inline, textColor);
                    foreach (var run in runs)
                    {
                        paragraph.Inlines.Add(run);
                    }

                    paragraph.Inlines.Add(new EditableRun("\n"));
                }
            }
            if (list.IsOrdered) itemNumber++;
        }

        return paragraph;
    }

    private Paragraph ConvertQuote(QuoteBlock quote, Color textColor, Color mutedColor, bool isDarkTheme)
    {
        var paragraph = new Paragraph
        {
            BorderBrush = new SolidColorBrush(isDarkTheme ? Color.Parse("#505050") : Color.Parse("#CCCCCC")),
            BorderThickness = new Thickness(4, 0, 0, 0),
            Background = new SolidColorBrush(isDarkTheme ? Color.Parse("#2A2A2A") : Color.Parse("#F9F9F9"))
        };

        foreach (var block in quote)
        {
            if (block is ParagraphBlock para)
            {
                var quotePrefix = new EditableRun("> ")
                {
                    FontSize = 14,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(mutedColor)
                };
                paragraph.Inlines.Add(quotePrefix);

                var runs = ConvertInlines(para.Inline, textColor);
                foreach (var run in runs)
                {
                    run.FontStyle = FontStyle.Italic;
                    run.Foreground = new SolidColorBrush(mutedColor);
                    paragraph.Inlines.Add(run);
                }

                paragraph.Inlines.Add(new EditableRun("\n"));
            }
        }

        return paragraph;
    }

    private List<EditableRun> ConvertInlines(ContainerInline? inline, Color textColor)
    {
        var runs = new List<EditableRun>();
        if (inline == null) return runs;

        foreach (var item in inline)
        {
            runs.AddRange(ConvertInline(item, textColor));
        }

        return runs;
    }

    private List<EditableRun> ConvertInline(Inline inline, Color textColor)
    {
        var runs = new List<EditableRun>();

        switch (inline)
        {
            case LiteralInline literal:
                runs.Add(new EditableRun(literal.Content.ToString())
                {
                    FontSize = 14,
                    Foreground = new SolidColorBrush(textColor)
                });
                break;

            case EmphasisInline emphasis when emphasis.DelimiterChar == '*' && emphasis.DelimiterCount == 2:
                // Bold
                foreach (var run in ConvertInlines(emphasis, textColor))
                {
                    run.FontWeight = FontWeight.Bold;
                    runs.Add(run);
                }
                break;

            case EmphasisInline emphasis when emphasis.DelimiterChar == '*' && emphasis.DelimiterCount == 1:
                // Italic
                foreach (var run in ConvertInlines(emphasis, textColor))
                {
                    run.FontStyle = FontStyle.Italic;
                    runs.Add(run);
                }
                break;

            case EmphasisInline emphasis when emphasis.DelimiterChar == '=' && emphasis.DelimiterCount == 2:
                // Highlight/Mark
                foreach (var run in ConvertInlines(emphasis, textColor))
                {
                    run.Background = new SolidColorBrush(Color.Parse("#FFF3CD"));
                    run.Foreground = Brushes.Black;
                    runs.Add(run);
                }
                break;

            case EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2:
                // Strikethrough
                foreach (var run in ConvertInlines(emphasis, textColor))
                {
                    run.TextDecorations = Avalonia.Media.TextDecorations.Strikethrough;
                    runs.Add(run);
                }
                break;

            case CodeInline code:
                runs.Add(new EditableRun($"`{code.Content}`")
                {
                    FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                    FontSize = 13,
                    Background = new SolidColorBrush(Color.Parse("#F5F5F5")),
                    Foreground = new SolidColorBrush(textColor)
                });
                break;

            case LinkInline link when !link.IsImage:
                var linkRuns = ConvertInlines(link, textColor);
                foreach (var run in linkRuns)
                {
                    run.Foreground = new SolidColorBrush(Color.Parse("#0066CC"));
                    run.TextDecorations = Avalonia.Media.TextDecorations.Underline;
                    runs.Add(run);
                }
                break;

            case LineBreakInline:
                // Use EditableLineBreak for line breaks
                runs.Add(new EditableRun("\n"));
                break;

            case ContainerInline container:
                runs.AddRange(ConvertInlines(container, textColor));
                break;
        }

        return runs;
    }
}
