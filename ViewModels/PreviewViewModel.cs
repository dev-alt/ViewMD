using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownViewer.Services;

namespace MarkdownViewer.ViewModels;

public partial class PreviewViewModel : ViewModelBase
{
    [ObservableProperty] private string _htmlContent = string.Empty;
    [ObservableProperty] private double _scrollPosition;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private bool _isDarkTheme = false;

    private readonly IMarkdownService _markdownService;
    private CancellationTokenSource? _renderCts;

    public PreviewViewModel(IMarkdownService markdownService)
    {
        _markdownService = markdownService;
    }

    public async Task UpdatePreviewAsync(string markdown, CancellationToken ct = default)
    {
        _renderCts?.Cancel();
        _renderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            var html = await _markdownService.RenderToHtmlAsync(markdown, _renderCts.Token);
            HtmlContent = _markdownService.GeneratePreviewHtml(html, IsDarkTheme);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        Zoom = Math.Min(Zoom + 0.1, 3.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        Zoom = Math.Max(Zoom - 0.1, 0.5);
    }

    [RelayCommand]
    private void ResetZoom()
    {
        Zoom = 1.0;
    }

    [RelayCommand]
    private Task CopyHtmlAsync()
    {
        // Clipboard functionality would go here
        // For now, just return completed task
        return Task.CompletedTask;
    }
}
