using Avalonia.Controls;
using Avalonia.Input;

namespace MarkdownViewer.Views;

public partial class TitleBarView : UserControl
{
    public TitleBarView()
    {
        InitializeComponent();

        // Enable title bar dragging
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += TitleBar_PointerPressed;
        }

        // Window control buttons
        var minimizeButton = this.FindControl<Button>("MinimizeButton");
        if (minimizeButton != null && this.VisualRoot is Window window)
        {
            minimizeButton.Click += (_, _) => window.WindowState = WindowState.Minimized;
        }

        var maximizeButton = this.FindControl<Button>("MaximizeButton");
        if (maximizeButton != null && this.VisualRoot is Window windowMax)
        {
            maximizeButton.Click += (_, _) =>
            {
                windowMax.WindowState = windowMax.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };
        }

        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null && this.VisualRoot is Window windowClose)
        {
            closeButton.Click += (_, _) => windowClose.Close();
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && this.VisualRoot is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }
}