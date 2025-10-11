using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MarkdownViewer.Services;

public class RecentFilesService : IRecentFilesService
{
    private readonly List<string> _recentFiles = [];
    private readonly string _recentFilesPath;
    private const int MaxRecentFiles = 10;

    public IReadOnlyList<string> RecentFiles => _recentFiles.AsReadOnly();

    public RecentFilesService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "ViewMD");
        Directory.CreateDirectory(appFolder);
        _recentFilesPath = Path.Combine(appFolder, "recent-files.json");
        LoadRecentFiles();
    }

    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        // Remove if already exists
        _recentFiles.Remove(filePath);

        // Add to the beginning
        _recentFiles.Insert(0, filePath);

        // Keep only the most recent files
        if (_recentFiles.Count > MaxRecentFiles)
        {
            _recentFiles.RemoveRange(MaxRecentFiles, _recentFiles.Count - MaxRecentFiles);
        }

        SaveRecentFiles();
    }

    public void ClearRecentFiles()
    {
        _recentFiles.Clear();
        SaveRecentFiles();
    }

    private void LoadRecentFiles()
    {
        try
        {
            if (File.Exists(_recentFilesPath))
            {
                var json = File.ReadAllText(_recentFilesPath);
                var files = JsonSerializer.Deserialize<List<string>>(json);
                if (files != null)
                {
                    // Only add files that still exist
                    _recentFiles.AddRange(files.Where(File.Exists));
                }
            }
        }
        catch
        {
            // Ignore errors loading recent files
        }
    }

    private void SaveRecentFiles()
    {
        try
        {
            var json = JsonSerializer.Serialize(_recentFiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_recentFilesPath, json);
        }
        catch
        {
            // Ignore errors saving recent files
        }
    }
}
