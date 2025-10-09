using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MarkdownViewer.Models;

namespace MarkdownViewer.Services;

public class FileService : IFileService
{
    private readonly List<string> _recentFiles = new();
    private const int MaxRecentFiles = 10;

    public async Task<MarkdownDocument?> OpenFileAsync(string? path = null)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (!File.Exists(path))
            return null;

        try
        {
            var content = await File.ReadAllTextAsync(path);
            var fileName = Path.GetFileNameWithoutExtension(path);

            await AddToRecentFilesAsync(path);

            return new MarkdownDocument
            {
                FilePath = path,
                Content = content,
                Title = fileName,
                LastModified = File.GetLastWriteTime(path),
                IsDirty = false
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SaveFileAsync(MarkdownDocument document)
    {
        if (string.IsNullOrEmpty(document.FilePath))
            return false;

        try
        {
            await File.WriteAllTextAsync(document.FilePath, document.Content);
            await AddToRecentFilesAsync(document.FilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveFileAsAsync(MarkdownDocument document, string path)
    {
        try
        {
            await File.WriteAllTextAsync(path, document.Content);
            await AddToRecentFilesAsync(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<MarkdownDocument> CreateNewDocumentAsync()
    {
        var document = new MarkdownDocument
        {
            Title = "Untitled",
            Content = string.Empty,
            IsDirty = false
        };
        return Task.FromResult(document);
    }

    public Task<List<string>> GetRecentFilesAsync()
    {
        return Task.FromResult(_recentFiles.ToList());
    }

    public Task AddToRecentFilesAsync(string path)
    {
        _recentFiles.Remove(path);
        _recentFiles.Insert(0, path);

        if (_recentFiles.Count > MaxRecentFiles)
        {
            _recentFiles.RemoveAt(_recentFiles.Count - 1);
        }

        return Task.CompletedTask;
    }
}
