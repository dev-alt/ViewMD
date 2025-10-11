using System.Collections.Generic;

namespace MarkdownViewer.Models;

public record AppSettings
{
    public ThemeMode Theme { get; init; } = ThemeMode.Light;
    public bool AutoSave { get; init; } = true;
    public int AutoSaveInterval { get; init; } = 30;
    public bool ShowLineNumbers { get; init; } = true;
    public bool SyncScroll { get; init; } = true;
    public string FontFamily { get; init; } = "Consolas";
    public int FontSize { get; init; } = 14;
    public List<string> RecentFiles { get; init; } = [];
    public string LastOpenPath { get; init; } = string.Empty;
    public double SplitterPosition { get; init; } = 0.5;
}

public enum ThemeMode
{
    Light,
    Dark,
    Auto
}
