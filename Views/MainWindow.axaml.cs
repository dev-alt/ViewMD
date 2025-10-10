using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using MarkdownViewer.ViewModels;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Input;
using System.IO;
using Avalonia;

namespace MarkdownViewer.Views;

public partial class MainWindow : Window
{
    private Border? _statusBar;
    private Border? _toolbarBorder;

    public MainWindow()
    {
        InitializeComponent();
        _statusBar = this.FindControl<Border>("StatusBar");
        _toolbarBorder = this.FindControl<Border>("ToolbarBorder");
        SetupViewModel();

        // Drag-and-drop support
        this.AddHandler(DragDrop.DragEnterEvent, OnDragOver);
        this.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        this.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        this.AddHandler(DragDrop.DropEvent, OnDrop);

        // Also attach to TabControl area for direct drops
        var tabs = this.FindControl<TabControl>("MainTabs");
        if (tabs != null)
        {
            tabs.AddHandler(DragDrop.DragEnterEvent, OnDragOver);
            tabs.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            tabs.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            tabs.AddHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    private void SetupViewModel()
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ShowOpenFileDialogAsync = ShowOpenFileDialogAsync;
            viewModel.ShowSaveFileDialogAsync = ShowSaveFileDialogAsync;

            // Apply light theme styling
            ApplyLightTheme();
        }
    }

    private void ApplyLightTheme()
    {
        if (_statusBar != null)
        {
            _statusBar.Background = new SolidColorBrush(Color.Parse("#F0F0F0"));
            foreach (var child in _statusBar.GetVisualDescendants())
            {
                if (child is TextBlock tb)
                {
                    tb.Foreground = Brushes.Black;
                }
            }
        }

        // Apply toolbar theme
        if (_toolbarBorder != null)
        {
            _toolbarBorder.Background = new SolidColorBrush(Color.Parse("#F3F3F3"));
            _toolbarBorder.BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
            _toolbarBorder.BorderThickness = new Thickness(0, 0, 0, 1);
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        SetupViewModel();
    }

    private async Task<string?> ShowOpenFileDialogAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Markdown File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Markdown Files")
                {
                    Patterns = new[] { "*.md", "*.markdown" }
                },
                new FilePickerFileType("Text Files")
                {
                    Patterns = new[] { "*.txt" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private async Task<string?> ShowSaveFileDialogAsync(string? extension = "md")
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Markdown File",
            DefaultExtension = extension,
            FileTypeChoices = extension == "html"
                ? new[]
                {
                    new FilePickerFileType("HTML Files")
                    {
                        Patterns = new[] { "*.html", "*.htm" }
                    }
                }
                : new[]
                {
                    new FilePickerFileType("Markdown Files")
                    {
                        Patterns = new[] { "*.md", "*.markdown" }
                    }
                }
        });

        return file?.Path.LocalPath;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Only allow if the payload includes at least one .md file
        bool allow = false;
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                allow = files.Any(f => f.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                                     || f.Name.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)
                                     || f.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
#pragma warning disable CS0618
            var names = e.Data.GetFileNames();
#pragma warning restore CS0618
            if (names != null)
            {
                allow = names.Any(p => p.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                                    || p.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)
                                    || p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                                    || Directory.Exists(p));
            }
        }

        e.DragEffects = allow ? DragDropEffects.Copy : DragDropEffects.None;

        var overlay = this.FindControl<Border>("DropOverlay");
        if (overlay != null)
        {
            overlay.IsVisible = allow;
        }
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        var overlay = this.FindControl<Border>("DropOverlay");
        if (overlay != null)
        {
            overlay.IsVisible = false;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
    int opened = 0;
    int skipped = 0;
    try
        {
            // Prefer cross-platform Avalonia API
            var storageFiles = e.Data.GetFiles();
        var pathList = new System.Collections.Generic.List<string>();
        if (storageFiles != null)
        {
            foreach (var f in storageFiles)
            {
                var p = f.Path.LocalPath;
                if (!string.IsNullOrWhiteSpace(p)) pathList.Add(p);
            }
        }

#pragma warning disable CS0618
        var names = e.Data.GetFileNames();
#pragma warning restore CS0618
        if (names != null)
        {
            foreach (var n in names)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                if (File.Exists(n)) pathList.Add(n);
                else if (Directory.Exists(n))
                {
                    // Recursively include .md/.markdown/.txt from folder
                    var files = Directory.EnumerateFiles(n, "*.*", SearchOption.AllDirectories)
                        .Where(fp => fp.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                                  || fp.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)
                                  || fp.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
                    pathList.AddRange(files);
                }
            }
        }

        string[] paths = pathList.Distinct().ToArray();

        if (paths.Length == 0) return;

        // Filter to supported files
        var mdPaths = paths.Where(p => p.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    || p.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)
                    || p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (mdPaths.Length == 0) return;

            if (DataContext is MainViewModel vm)
            {
                // Open first file in active window, others in new tabs (existing behavior adds tabs)
                foreach (var path in mdPaths)
                {
                    await vm.OpenFileFromPathAsync(path);
                    opened++;
                }
            }
            skipped = paths.Length - opened;
        }
        catch
        {
            // ignore
        }
        finally
        {
            var overlay = this.FindControl<Border>("DropOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = false;
            }
            // Show a simple summary in the status bar via view model
            if (DataContext is MainViewModel vm)
            {
                vm.StatusText = opened > 0
                    ? $"Opened {opened} file(s){(skipped > 0 ? $", skipped {skipped}" : string.Empty)}"
                    : "No supported files found";
            }
            e.Handled = true;
        }
    }
}