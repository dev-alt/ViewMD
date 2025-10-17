using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownViewer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MarkdownViewer.Services;

public class ExportService : IExportService
{
    private readonly IMarkdownService _markdownService;

    public ExportService(IMarkdownService markdownService)
    {
        _markdownService = markdownService;
        // Set QuestPDF license (Community edition)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<bool> ExportToHtmlAsync(Models.MarkdownDocument document, string outputPath)
    {
        try
        {
            var html = await GenerateStandaloneHtmlAsync(document.Content, false);
            await File.WriteAllTextAsync(outputPath, html);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExportToPdfAsync(Models.MarkdownDocument document, string outputPath, bool isDarkTheme = false)
    {
        try
        {
            // Parse markdown
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .Build();

            var markdownDocument = Markdown.Parse(document.Content, pipeline);

            // Generate PDF
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12).FontColor(isDarkTheme ? "#E0E0E0" : "#333333"));

                    page.Header()
                        .Text(document.Title ?? "Markdown Document")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(isDarkTheme ? "#FFFFFF" : "#000000");

                    page.Content()
                        .Column(column =>
                        {
                            foreach (var block in markdownDocument)
                            {
                                RenderBlockToPdf(column, block, isDarkTheme);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            }).GeneratePdf(outputPath);

            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RenderBlockToPdf(ColumnDescriptor column, Block block, bool isDarkTheme)
    {
        var textColor = isDarkTheme ? "#E0E0E0" : "#333333";
        var headingColor = isDarkTheme ? "#FFFFFF" : "#000000";

        switch (block)
        {
            case HeadingBlock heading:
                var fontSize = heading.Level switch
                {
                    1 => 24f,
                    2 => 20f,
                    3 => 18f,
                    4 => 16f,
                    5 => 14f,
                    _ => 12f
                };
                column.Item()
                    .PaddingVertical(8)
                    .Text(ExtractText(heading.Inline))
                    .FontSize(fontSize)
                    .Bold()
                    .FontColor(headingColor);
                break;

            case ParagraphBlock paragraph:
                column.Item()
                    .PaddingVertical(4)
                    .Text(ExtractText(paragraph.Inline))
                    .FontSize(12)
                    .FontColor(textColor);
                break;

            case CodeBlock codeBlock:
                var codeLines = new System.Collections.Generic.List<string>();
                foreach (var line in codeBlock.Lines)
                {
                    codeLines.Add(line.ToString() ?? string.Empty);
                }
                var code = string.Join("\n", codeLines);
                column.Item()
                    .Background(isDarkTheme ? "#2D2D2D" : "#F5F5F5")
                    .Border(1)
                    .BorderColor(isDarkTheme ? "#404040" : "#E0E0E0")
                    .Padding(12)
                    .Text(code)
                    .FontFamily("Courier New")
                    .FontSize(10)
                    .FontColor(textColor);
                break;

            case ListBlock list:
                int itemNumber = 1;
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    foreach (var itemBlock in item)
                    {
                        if (itemBlock is ParagraphBlock para)
                        {
                            var prefix = list.IsOrdered ? $"{itemNumber}. " : "• ";
                            column.Item().Row(row =>
                            {
                                row.ConstantItem(30).Text(prefix);
                                row.RelativeItem().Text(ExtractText(para.Inline));
                            });
                        }
                    }
                    if (list.IsOrdered) itemNumber++;
                }
                break;

            case QuoteBlock quote:
                column.Item()
                    .BorderLeft(4)
                    .BorderColor(isDarkTheme ? "#505050" : "#CCCCCC")
                    .Background(isDarkTheme ? "#2A2A2A" : "#F9F9F9")
                    .Padding(12)
                    .Column(quoteColumn =>
                    {
                        foreach (var quoteBlock in quote)
                        {
                            if (quoteBlock is ParagraphBlock para)
                            {
                                quoteColumn.Item()
                                    .Text(ExtractText(para.Inline))
                                    .Italic()
                                    .FontColor(textColor);
                            }
                        }
                    });
                break;

            case ThematicBreakBlock:
                column.Item().LineHorizontal(1).LineColor(isDarkTheme ? "#404040" : "#E0E0E0");
                break;
        }
    }

    private string ExtractText(ContainerInline? inline)
    {
        if (inline == null) return string.Empty;

        var result = "";
        foreach (var item in inline)
        {
            result += item switch
            {
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => $"`{code.Content}`",
                EmphasisInline emphasis => ExtractText(emphasis),
                LineBreakInline => "\n",
                LinkInline link => ExtractText(link as ContainerInline),
                _ => item.ToString()
            };
        }
        return result;
    }

    public async Task<string> GenerateStandaloneHtmlAsync(string markdownContent, bool isDarkTheme)
    {
        var renderedHtml = await _markdownService.RenderToHtmlAsync(markdownContent);
        return _markdownService.GeneratePreviewHtml(renderedHtml, isDarkTheme);
    }
}
