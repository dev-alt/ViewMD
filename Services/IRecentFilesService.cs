using System.Collections.Generic;

namespace MarkdownViewer.Services;

public interface IRecentFilesService
{
    IReadOnlyList<string> RecentFiles { get; }
    void AddRecentFile(string filePath);
    void ClearRecentFiles();
}
