using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MarkdownViewer.ViewModels;
using MarkdownViewer.Views;
using MarkdownViewer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarkdownViewer;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<IMarkdownService, MarkdownService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<EditorViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<DocumentViewModel>(sp =>
            new DocumentViewModel(
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<PreviewViewModel>()));

        return services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            var mainViewModel = Services!.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // If the app was started with a file path argument (e.g., via file association), try to open it.
            // The args are available via desktop.Args when using StartWithClassicDesktopLifetime(args) in Program.Main.
            if (desktop.Args != null && desktop.Args.Length > 0)
            {
                // Try to normalize arguments (supports plain paths and file:// URIs)
                string? NormalizeArg(string arg)
                {
                    if (string.IsNullOrWhiteSpace(arg)) return null;
                    arg = arg.Trim('"');
                    if (Uri.TryCreate(arg, UriKind.Absolute, out var uri) && uri.IsFile)
                    {
                        return uri.LocalPath;
                    }
                    return arg;
                }

                var normalized = desktop.Args.Select(NormalizeArg)
                                             .Where(a => !string.IsNullOrWhiteSpace(a))
                                             .ToArray();

                if (normalized.Length > 0)
                {
                    // Open the first file in the main window
                    var first = normalized[0]!;
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try { await mainViewModel.OpenFileFromPathAsync(first); } catch { }
                    });

                    // For additional files, open separate windows for now
                    for (int i = 1; i < normalized.Length; i++)
                    {
                        var path = normalized[i]!;
                        // Create a new window with its own VM
                        var vm = Services!.GetRequiredService<MainViewModel>();
                        var wnd = new MainWindow { DataContext = vm };
                        wnd.Show();
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            try { await vm.OpenFileFromPathAsync(path); } catch { }
                        });
                    }
                }
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}