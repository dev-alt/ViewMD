using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace MarkdownViewer.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window != null)
            {
                var result = await ShowMessageBoxAsync(window, title, message, MessageBoxButtons.YesNo);
                return result == MessageBoxResult.Yes;
            }
        }
        return false;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window != null)
            {
                await ShowMessageBoxAsync(window, title, message, MessageBoxButtons.Ok);
            }
        }
    }

    private async Task<MessageBoxResult> ShowMessageBoxAsync(Window parent, string title, string message, MessageBoxButtons buttons)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        var result = MessageBoxResult.None;

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };

        // Message text
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 14
        });

        // Button panel
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        if (buttons == MessageBoxButtons.YesNo)
        {
            var yesButton = new Button
            {
                Content = "Yes",
                Width = 80,
                Height = 32
            };
            yesButton.Click += (_, _) =>
            {
                result = MessageBoxResult.Yes;
                dialog.Close();
            };

            var noButton = new Button
            {
                Content = "No",
                Width = 80,
                Height = 32
            };
            noButton.Click += (_, _) =>
            {
                result = MessageBoxResult.No;
                dialog.Close();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            // Set Yes as default
            yesButton.IsDefault = true;
        }
        else
        {
            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 32,
                IsDefault = true
            };
            okButton.Click += (_, _) =>
            {
                result = MessageBoxResult.Ok;
                dialog.Close();
            };

            buttonPanel.Children.Add(okButton);
        }

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(parent);
        return result;
    }

    private enum MessageBoxButtons
    {
        Ok,
        YesNo
    }

    private enum MessageBoxResult
    {
        None,
        Ok,
        Yes,
        No
    }
}
