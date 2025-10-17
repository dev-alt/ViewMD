namespace MarkdownViewer.Models;

public enum AppTheme
{
    GlassLight,
    GlassDark,
    AcrylicLight,
    AcrylicDark,
    PureDark
}

public static class AppThemeExtensions
{
    public static string GetDisplayName(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "Glass Light",
        AppTheme.GlassDark => "Glass Dark",
        AppTheme.AcrylicLight => "Acrylic Light",
        AppTheme.AcrylicDark => "Acrylic Dark",
        AppTheme.PureDark => "Pure Dark",
        _ => "Unknown"
    };

    public static string GetTintColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "#F8F9FA",
        AppTheme.GlassDark => "#1E1E1E",
        AppTheme.AcrylicLight => "#FFFFFF",
        AppTheme.AcrylicDark => "#2D2D30",
        AppTheme.PureDark => "#1E1E1E",
        _ => "#F8F9FA"
    };

    public static double GetTintOpacity(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => 0.9,
        AppTheme.GlassDark => 0.85,
        AppTheme.AcrylicLight => 0.75,
        AppTheme.AcrylicDark => 0.8,
        AppTheme.PureDark => 1.0,
        _ => 0.9
    };

    public static double GetMaterialOpacity(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => 0.7,
        AppTheme.GlassDark => 0.75,
        AppTheme.AcrylicLight => 0.6,
        AppTheme.AcrylicDark => 0.65,
        AppTheme.PureDark => 1.0,
        _ => 0.7
    };

    public static bool IsDarkTheme(this AppTheme theme) => theme switch
    {
        AppTheme.GlassDark => true,
        AppTheme.AcrylicDark => true,
        AppTheme.PureDark => true,
        _ => false
    };
}
