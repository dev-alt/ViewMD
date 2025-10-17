using System.Threading.Tasks;
using MarkdownViewer.Models;

namespace MarkdownViewer.Services;

public interface IExportService
{
    Task<bool> ExportToHtmlAsync(MarkdownDocument document, string outputPath);
    Task<bool> ExportToPdfAsync(MarkdownDocument document, string outputPath, bool isDarkTheme = false);
    Task<string> GenerateStandaloneHtmlAsync(string markdownContent, bool isDarkTheme);
}
