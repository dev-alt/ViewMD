using System.Collections.Generic;
using System.Threading.Tasks;
using MarkdownViewer.Models;

namespace MarkdownViewer.Services;

public interface IFileService
{
    Task<MarkdownDocument?> OpenFileAsync(string? path = null);
    Task<bool> SaveFileAsync(MarkdownDocument document);
    Task<bool> SaveFileAsAsync(MarkdownDocument document, string path);
    Task<MarkdownDocument> CreateNewDocumentAsync();
    Task<List<string>> GetRecentFilesAsync();
    Task AddToRecentFilesAsync(string path);
}
