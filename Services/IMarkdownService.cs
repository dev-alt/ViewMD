using System.Threading;
using System.Threading.Tasks;
using Markdig;

namespace MarkdownViewer.Services;

public interface IMarkdownService
{
    string RenderToHtml(string markdown);
    Task<string> RenderToHtmlAsync(string markdown, CancellationToken ct = default);
    string GeneratePreviewHtml(string markdownHtml, bool isDarkTheme);
    MarkdownPipeline GetPipeline();
}
