using System.IO;
using System.Threading.Tasks;
using MarkdownViewer.Models;

namespace MarkdownViewer.Services;

public class ExportService : IExportService
{
    private readonly IMarkdownService _markdownService;

    public ExportService(IMarkdownService markdownService)
    {
        _markdownService = markdownService;
    }

    public async Task<bool> ExportToHtmlAsync(MarkdownDocument document, string outputPath)
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

    public async Task<string> GenerateStandaloneHtmlAsync(string markdownContent, bool isDarkTheme)
    {
        var renderedHtml = await _markdownService.RenderToHtmlAsync(markdownContent);
        return _markdownService.GeneratePreviewHtml(renderedHtml, isDarkTheme);
    }
}
