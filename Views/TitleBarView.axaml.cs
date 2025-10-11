using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MarkdownViewer.Views;

public partial class TitleBarView : UserControl
{
    public TitleBarView()
    {
        InitializeComponent();

        // Wire up event handlers after the control is added to the visual tree
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Console.WriteLine("DEBUG: TitleBarView attached to visual tree");

        // Enable title bar dragging
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += TitleBar_PointerPressed;
        }

        // Debug hamburger menu
        var hamburgerMenuItem = this.FindControl<MenuItem>("HamburgerMenuItem");
        if (hamburgerMenuItem != null)
        {
            Console.WriteLine("DEBUG: Found HamburgerMenuItem");
            hamburgerMenuItem.PointerPressed += (s, e) => Console.WriteLine("DEBUG: HamburgerMenuItem PointerPressed!");
            hamburgerMenuItem.SubmenuOpened += (s, e) => Console.WriteLine("DEBUG: HamburgerMenuItem SubmenuOpened!");
        }

        // Debug menu item clicks
        var newMenuItem = this.FindControl<MenuItem>("NewMenuItem");
        if (newMenuItem != null)
        {
            Console.WriteLine($"DEBUG: Found NewMenuItem");
            Console.WriteLine($"DEBUG: NewMenuItem.Command is null: {newMenuItem.Command == null}");
            Console.WriteLine($"DEBUG: NewMenuItem.IsEnabled: {newMenuItem.IsEnabled}");
            Console.WriteLine($"DEBUG: NewMenuItem.IsEffectivelyEnabled: {newMenuItem.IsEffectivelyEnabled}");

            if (newMenuItem.Command != null)
            {
                Console.WriteLine($"DEBUG: NewMenuItem.Command.CanExecute(null): {newMenuItem.Command.CanExecute(null)}");
            }

            newMenuItem.Click += (s, e) =>
            {
                Console.WriteLine("DEBUG: NewMenuItem CLICKED!");
            };

            newMenuItem.PointerPressed += (s, e) =>
            {
                Console.WriteLine("DEBUG: NewMenuItem PointerPressed!");

                // Try to manually execute the command
                if (newMenuItem.Command != null && newMenuItem.Command.CanExecute(null))
                {
                    Console.WriteLine("DEBUG: Manually executing command!");
                    newMenuItem.Command.Execute(null);
                }
                else
                {
                    Console.WriteLine($"DEBUG: Command cannot execute! Command null: {newMenuItem.Command == null}, CanExecute: {newMenuItem.Command?.CanExecute(null)}");
                }
            };
        }
        else
        {
            Console.WriteLine("DEBUG: NewMenuItem NOT found!");
        }

        var openMenuItem = this.FindControl<MenuItem>("OpenMenuItem");
        if (openMenuItem != null)
        {
            Console.WriteLine($"DEBUG: Found OpenMenuItem");
            Console.WriteLine($"DEBUG: OpenMenuItem.Command is null: {openMenuItem.Command == null}");

            openMenuItem.Click += (s, e) =>
            {
                Console.WriteLine("DEBUG: OpenMenuItem CLICKED!");
            };

            openMenuItem.PointerPressed += (s, e) =>
            {
                Console.WriteLine("DEBUG: OpenMenuItem PointerPressed!");
            };
        }
        else
        {
            Console.WriteLine("DEBUG: OpenMenuItem NOT found!");
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

        // Debug DataContext
        Console.WriteLine($"DEBUG: TitleBarView DataContext type: {DataContext?.GetType().Name ?? "null"}");
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && this.VisualRoot is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }
}