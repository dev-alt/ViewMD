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
        if (minimizeButton != null)
        {
            minimizeButton.Click += (_, _) => ((Window)this.VisualRoot).WindowState = WindowState.Minimized;
        }

        var maximizeButton = this.FindControl<Button>("MaximizeButton");
        if (maximizeButton != null)
        {
            maximizeButton.Click += (_, _) =>
            {
                var window = (Window)this.VisualRoot;
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };
        }

        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += (_, _) => ((Window)this.VisualRoot).Close();
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ((Window)this.VisualRoot).BeginMoveDrag(e);
        }
    }
}