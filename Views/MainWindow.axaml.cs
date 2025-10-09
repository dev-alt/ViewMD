using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.VisualTree;
using MarkdownViewer.ViewModels;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace MarkdownViewer.Views;

public partial class MainWindow : Window
{
    public static readonly FuncValueConverter<bool, string> ThemeConverter =
        new FuncValueConverter<bool, string>(isDark => isDark ? "Dark" : "Light");

    private Border? _statusBar;

    public MainWindow()
    {
        InitializeComponent();
        _statusBar = this.FindControl<Border>("StatusBar");
        SetupViewModel();
    }

    private void SetupViewModel()
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ShowOpenFileDialogAsync = ShowOpenFileDialogAsync;
            viewModel.ShowSaveFileDialogAsync = ShowSaveFileDialogAsync;

            // Subscribe to theme changes
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(viewModel.IsDarkTheme))
                {
                    ApplyThemeToStatusBar(viewModel.IsDarkTheme);
                }
            };

            // Apply initial theme
            ApplyThemeToStatusBar(viewModel.IsDarkTheme);
        }
    }

    private void ApplyThemeToStatusBar(bool isDarkTheme)
    {
        if (_statusBar == null) return;

        if (isDarkTheme)
        {
            _statusBar.Background = new SolidColorBrush(Color.Parse("#2D2D2D"));
            // Update all TextBlocks in status bar
            foreach (var child in _statusBar.GetVisualDescendants())
            {
                if (child is TextBlock tb)
                {
                    tb.Foreground = new SolidColorBrush(Color.Parse("#D4D4D4"));
                }
            }
        }
        else
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
}