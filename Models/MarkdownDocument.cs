using System;

namespace MarkdownViewer.Models;

public record MarkdownDocument
{
    public string FilePath { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsDirty { get; init; } = false;
    public DateTime LastModified { get; init; } = DateTime.Now;
    public string Title { get; init; } = "Untitled";
    public bool IsNewDocument => string.IsNullOrEmpty(FilePath);
}
