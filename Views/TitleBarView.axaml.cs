using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MarkdownViewer.Models;

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
        // Enable title bar dragging
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += TitleBar_PointerPressed;
        }

        // File menu items - manually execute commands on PointerPressed
        // (workaround for MenuItem Click event not firing on Windows)
        var newMenuItem = this.FindControl<MenuItem>("NewMenuItem");
        if (newMenuItem != null)
        {
            newMenuItem.PointerPressed += (s, e) =>
            {
                if (newMenuItem.Command != null && newMenuItem.Command.CanExecute(null))
                {
                    newMenuItem.Command.Execute(null);
                }
            };
        }

        var openMenuItem = this.FindControl<MenuItem>("OpenMenuItem");
        if (openMenuItem != null)
        {
            openMenuItem.PointerPressed += (s, e) =>
            {
                if (openMenuItem.Command != null && openMenuItem.Command.CanExecute(null))
                {
                    openMenuItem.Command.Execute(null);
                }
            };
        }

        // Save commands
        var saveMenuItem = this.FindControl<MenuItem>("SaveMenuItem");
        if (saveMenuItem != null)
        {
            saveMenuItem.PointerPressed += (s, e) =>
            {
                if (saveMenuItem.Command != null && saveMenuItem.Command.CanExecute(null))
                {
                    saveMenuItem.Command.Execute(null);
                }
            };
        }

        var saveAsMenuItem = this.FindControl<MenuItem>("SaveAsMenuItem");
        if (saveAsMenuItem != null)
        {
            saveAsMenuItem.PointerPressed += (s, e) =>
            {
                if (saveAsMenuItem.Command != null && saveAsMenuItem.Command.CanExecute(null))
                {
                    saveAsMenuItem.Command.Execute(null);
                }
            };
        }

        var clearRecentMenuItem = this.FindControl<MenuItem>("ClearRecentMenuItem");
        if (clearRecentMenuItem != null)
        {
            clearRecentMenuItem.PointerPressed += (s, e) =>
            {
                if (clearRecentMenuItem.Command != null && clearRecentMenuItem.Command.CanExecute(null))
                {
                    clearRecentMenuItem.Command.Execute(null);
                }
            };
        }

        // Editor commands
        var boldMenuItem = this.FindControl<MenuItem>("BoldMenuItem");
        if (boldMenuItem != null)
        {
            boldMenuItem.PointerPressed += (s, e) =>
            {
                if (boldMenuItem.Command != null && boldMenuItem.Command.CanExecute(null))
                {
                    boldMenuItem.Command.Execute(null);
                }
            };
        }

        var italicMenuItem = this.FindControl<MenuItem>("ItalicMenuItem");
        if (italicMenuItem != null)
        {
            italicMenuItem.PointerPressed += (s, e) =>
            {
                if (italicMenuItem.Command != null && italicMenuItem.Command.CanExecute(null))
                {
                    italicMenuItem.Command.Execute(null);
                }
            };
        }

        var linkMenuItem = this.FindControl<MenuItem>("LinkMenuItem");
        if (linkMenuItem != null)
        {
            linkMenuItem.PointerPressed += (s, e) =>
            {
                if (linkMenuItem.Command != null && linkMenuItem.Command.CanExecute(null))
                {
                    linkMenuItem.Command.Execute(null);
                }
            };
        }

        var imageMenuItem = this.FindControl<MenuItem>("ImageMenuItem");
        if (imageMenuItem != null)
        {
            imageMenuItem.PointerPressed += (s, e) =>
            {
                if (imageMenuItem.Command != null && imageMenuItem.Command.CanExecute(null))
                {
                    imageMenuItem.Command.Execute(null);
                }
            };
        }

        var codeBlockMenuItem = this.FindControl<MenuItem>("CodeBlockMenuItem");
        if (codeBlockMenuItem != null)
        {
            codeBlockMenuItem.PointerPressed += (s, e) =>
            {
                if (codeBlockMenuItem.Command != null && codeBlockMenuItem.Command.CanExecute(null))
                {
                    codeBlockMenuItem.Command.Execute(null);
                }
            };
        }

        var tableMenuItem = this.FindControl<MenuItem>("TableMenuItem");
        if (tableMenuItem != null)
        {
            tableMenuItem.PointerPressed += (s, e) =>
            {
                if (tableMenuItem.Command != null && tableMenuItem.Command.CanExecute(null))
                {
                    tableMenuItem.Command.Execute(null);
                }
            };
        }

        // View commands
        var toggleReadModeMenuItem = this.FindControl<MenuItem>("ToggleReadModeMenuItem");
        if (toggleReadModeMenuItem != null)
        {
            toggleReadModeMenuItem.PointerPressed += (s, e) =>
            {
                if (toggleReadModeMenuItem.Command != null && toggleReadModeMenuItem.Command.CanExecute(null))
                {
                    toggleReadModeMenuItem.Command.Execute(null);
                }
            };
        }

        // Theme commands
        var glassLightThemeItem = this.FindControl<MenuItem>("GlassLightThemeItem");
        if (glassLightThemeItem != null)
        {
            glassLightThemeItem.PointerPressed += (s, e) =>
            {
                if (glassLightThemeItem.Command != null && glassLightThemeItem.Command.CanExecute(null))
                {
                    glassLightThemeItem.Command.Execute(null);
                }
            };
        }

        var glassDarkThemeItem = this.FindControl<MenuItem>("GlassDarkThemeItem");
        if (glassDarkThemeItem != null)
        {
            glassDarkThemeItem.PointerPressed += (s, e) =>
            {
                if (glassDarkThemeItem.Command != null && glassDarkThemeItem.Command.CanExecute(null))
                {
                    glassDarkThemeItem.Command.Execute(null);
                }
            };
        }

        var acrylicLightThemeItem = this.FindControl<MenuItem>("AcrylicLightThemeItem");
        if (acrylicLightThemeItem != null)
        {
            acrylicLightThemeItem.PointerPressed += (s, e) =>
            {
                if (acrylicLightThemeItem.Command != null && acrylicLightThemeItem.Command.CanExecute(null))
                {
                    acrylicLightThemeItem.Command.Execute(null);
                }
            };
        }

        var acrylicDarkThemeItem = this.FindControl<MenuItem>("AcrylicDarkThemeItem");
        if (acrylicDarkThemeItem != null)
        {
            acrylicDarkThemeItem.PointerPressed += (s, e) =>
            {
                if (acrylicDarkThemeItem.Command != null && acrylicDarkThemeItem.Command.CanExecute(null))
                {
                    acrylicDarkThemeItem.Command.Execute(null);
                }
            };
        }

        var pureDarkThemeItem = this.FindControl<MenuItem>("PureDarkThemeItem");
        if (pureDarkThemeItem != null)
        {
            pureDarkThemeItem.PointerPressed += (s, e) =>
            {
                if (pureDarkThemeItem.Command != null && pureDarkThemeItem.Command.CanExecute(null))
                {
                    pureDarkThemeItem.Command.Execute(null);
                }
            };
        }

        var oceanBreezeThemeItem = this.FindControl<MenuItem>("OceanBreezeThemeItem");
        if (oceanBreezeThemeItem != null)
        {
            oceanBreezeThemeItem.PointerPressed += (s, e) =>
            {
                if (oceanBreezeThemeItem.Command != null && oceanBreezeThemeItem.Command.CanExecute(null))
                {
                    oceanBreezeThemeItem.Command.Execute(null);
                }
            };
        }

        var forestCanopyThemeItem = this.FindControl<MenuItem>("ForestCanopyThemeItem");
        if (forestCanopyThemeItem != null)
        {
            forestCanopyThemeItem.PointerPressed += (s, e) =>
            {
                if (forestCanopyThemeItem.Command != null && forestCanopyThemeItem.Command.CanExecute(null))
                {
                    forestCanopyThemeItem.Command.Execute(null);
                }
            };
        }

        var sunsetGlowThemeItem = this.FindControl<MenuItem>("SunsetGlowThemeItem");
        if (sunsetGlowThemeItem != null)
        {
            sunsetGlowThemeItem.PointerPressed += (s, e) =>
            {
                if (sunsetGlowThemeItem.Command != null && sunsetGlowThemeItem.Command.CanExecute(null))
                {
                    sunsetGlowThemeItem.Command.Execute(null);
                }
            };
        }

        var midnightPurpleThemeItem = this.FindControl<MenuItem>("MidnightPurpleThemeItem");
        if (midnightPurpleThemeItem != null)
        {
            midnightPurpleThemeItem.PointerPressed += (s, e) =>
            {
                if (midnightPurpleThemeItem.Command != null && midnightPurpleThemeItem.Command.CanExecute(null))
                {
                    midnightPurpleThemeItem.Command.Execute(null);
                }
            };
        }

        var roseGoldThemeItem = this.FindControl<MenuItem>("RoseGoldThemeItem");
        if (roseGoldThemeItem != null)
        {
            roseGoldThemeItem.PointerPressed += (s, e) =>
            {
                if (roseGoldThemeItem.Command != null && roseGoldThemeItem.Command.CanExecute(null))
                {
                    roseGoldThemeItem.Command.Execute(null);
                }
            };
        }

        var arcticMintThemeItem = this.FindControl<MenuItem>("ArcticMintThemeItem");
        if (arcticMintThemeItem != null)
        {
            arcticMintThemeItem.PointerPressed += (s, e) =>
            {
                if (arcticMintThemeItem.Command != null && arcticMintThemeItem.Command.CanExecute(null))
                {
                    arcticMintThemeItem.Command.Execute(null);
                }
            };
        }

        // Tab commands
        var closeTabMenuItem = this.FindControl<MenuItem>("CloseTabMenuItem");
        if (closeTabMenuItem != null)
        {
            closeTabMenuItem.PointerPressed += (s, e) =>
            {
                if (closeTabMenuItem.Command != null && closeTabMenuItem.Command.CanExecute(null))
                {
                    closeTabMenuItem.Command.Execute(null);
                }
            };
        }

        var closeOtherTabsMenuItem = this.FindControl<MenuItem>("CloseOtherTabsMenuItem");
        if (closeOtherTabsMenuItem != null)
        {
            closeOtherTabsMenuItem.PointerPressed += (s, e) =>
            {
                if (closeOtherTabsMenuItem.Command != null && closeOtherTabsMenuItem.Command.CanExecute(null))
                {
                    closeOtherTabsMenuItem.Command.Execute(null);
                }
            };
        }

        var closeAllTabsMenuItem = this.FindControl<MenuItem>("CloseAllTabsMenuItem");
        if (closeAllTabsMenuItem != null)
        {
            closeAllTabsMenuItem.PointerPressed += (s, e) =>
            {
                if (closeAllTabsMenuItem.Command != null && closeAllTabsMenuItem.Command.CanExecute(null))
                {
                    closeAllTabsMenuItem.Command.Execute(null);
                }
            };
        }

        // Export commands
        var copyHtmlMenuItem = this.FindControl<MenuItem>("CopyHtmlMenuItem");
        if (copyHtmlMenuItem != null)
        {
            copyHtmlMenuItem.PointerPressed += (s, e) =>
            {
                if (copyHtmlMenuItem.Command != null && copyHtmlMenuItem.Command.CanExecute(null))
                {
                    copyHtmlMenuItem.Command.Execute(null);
                }
            };
        }

        var exportHtmlMenuItem = this.FindControl<MenuItem>("ExportHtmlMenuItem");
        if (exportHtmlMenuItem != null)
        {
            exportHtmlMenuItem.PointerPressed += (s, e) =>
            {
                if (exportHtmlMenuItem.Command != null && exportHtmlMenuItem.Command.CanExecute(null))
                {
                    exportHtmlMenuItem.Command.Execute(null);
                }
            };
        }

        var exportPdfMenuItem = this.FindControl<MenuItem>("ExportPdfMenuItem");
        if (exportPdfMenuItem != null)
        {
            exportPdfMenuItem.PointerPressed += (s, e) =>
            {
                if (exportPdfMenuItem.Command != null && exportPdfMenuItem.Command.CanExecute(null))
                {
                    exportPdfMenuItem.Command.Execute(null);
                }
            };
        }

        // Exit command
        var exitMenuItem = this.FindControl<MenuItem>("ExitMenuItem");
        if (exitMenuItem != null)
        {
            exitMenuItem.PointerPressed += (s, e) =>
            {
                if (exitMenuItem.Command != null && exitMenuItem.Command.CanExecute(null))
                {
                    exitMenuItem.Command.Execute(null);
                }
            };
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

    public void ApplyTheme(AppTheme theme)
    {
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(theme.GetTitleBarColor()));
        }

        // Update app title text color
        var appTitle = titleBar?.FindControl<StackPanel>("AppTitle");
        if (appTitle != null)
        {
            foreach (var child in appTitle.Children)
            {
                if (child is TextBlock tb)
                {
                    tb.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(theme.GetForegroundColor()));
                }
            }
        }
    }
}