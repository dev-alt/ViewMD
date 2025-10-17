namespace MarkdownViewer.Models;

public enum AppTheme
{
    // Original themes
    GlassLight,
    GlassDark,
    AcrylicLight,
    AcrylicDark,
    PureDark,

    // New colorful themes
    OceanBreeze,
    ForestCanopy,
    SunsetGlow,
    MidnightPurple,
    RoseGold,
    ArcticMint
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
        AppTheme.OceanBreeze => "Ocean Breeze",
        AppTheme.ForestCanopy => "Forest Canopy",
        AppTheme.SunsetGlow => "Sunset Glow",
        AppTheme.MidnightPurple => "Midnight Purple",
        AppTheme.RoseGold => "Rose Gold",
        AppTheme.ArcticMint => "Arctic Mint",
        _ => "Unknown"
    };

    public static string GetTintColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "#F8F9FA",
        AppTheme.GlassDark => "#1E1E1E",
        AppTheme.AcrylicLight => "#FFFFFF",
        AppTheme.AcrylicDark => "#2D2D30",
        AppTheme.PureDark => "#1E1E1E",
        AppTheme.OceanBreeze => "#E0F2FE",
        AppTheme.ForestCanopy => "#ECFDF5",
        AppTheme.SunsetGlow => "#FEF3E2",
        AppTheme.MidnightPurple => "#2D1B4E",
        AppTheme.RoseGold => "#FFF1F2",
        AppTheme.ArcticMint => "#F0FDFA",
        _ => "#F8F9FA"
    };

    public static double GetTintOpacity(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => 0.9,
        AppTheme.GlassDark => 0.85,
        AppTheme.AcrylicLight => 0.75,
        AppTheme.AcrylicDark => 0.8,
        AppTheme.PureDark => 1.0,
        AppTheme.OceanBreeze => 0.85,
        AppTheme.ForestCanopy => 0.85,
        AppTheme.SunsetGlow => 0.85,
        AppTheme.MidnightPurple => 0.9,
        AppTheme.RoseGold => 0.85,
        AppTheme.ArcticMint => 0.85,
        _ => 0.9
    };

    public static double GetMaterialOpacity(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => 0.7,
        AppTheme.GlassDark => 0.75,
        AppTheme.AcrylicLight => 0.6,
        AppTheme.AcrylicDark => 0.65,
        AppTheme.PureDark => 1.0,
        AppTheme.OceanBreeze => 0.7,
        AppTheme.ForestCanopy => 0.7,
        AppTheme.SunsetGlow => 0.7,
        AppTheme.MidnightPurple => 0.8,
        AppTheme.RoseGold => 0.7,
        AppTheme.ArcticMint => 0.7,
        _ => 0.7
    };

    public static bool IsDarkTheme(this AppTheme theme) => theme switch
    {
        AppTheme.GlassDark => true,
        AppTheme.AcrylicDark => true,
        AppTheme.PureDark => true,
        AppTheme.MidnightPurple => true,
        _ => false
    };

    // UI Element Colors
    public static string GetTitleBarColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "#FCFCFC",
        AppTheme.GlassDark => "#1E1E1E",
        AppTheme.AcrylicLight => "#FFFFFF",
        AppTheme.AcrylicDark => "#2D2D30",
        AppTheme.PureDark => "#1A1A1A",
        AppTheme.OceanBreeze => "#E0F2FE",
        AppTheme.ForestCanopy => "#ECFDF5",
        AppTheme.SunsetGlow => "#FEF3E2",
        AppTheme.MidnightPurple => "#2D1B4E",
        AppTheme.RoseGold => "#FFF1F2",
        AppTheme.ArcticMint => "#F0FDFA",
        _ => "#FCFCFC"
    };

    public static string GetTabBackgroundColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "#F8F9FA",
        AppTheme.GlassDark => "#252525",
        AppTheme.AcrylicLight => "#F5F5F5",
        AppTheme.AcrylicDark => "#2D2D30",
        AppTheme.PureDark => "#1E1E1E",
        AppTheme.OceanBreeze => "#BAE6FD",
        AppTheme.ForestCanopy => "#D1FAE5",
        AppTheme.SunsetGlow => "#FED7AA",
        AppTheme.MidnightPurple => "#4C1D95",
        AppTheme.RoseGold => "#FECDD3",
        AppTheme.ArcticMint => "#CCFBF1",
        _ => "#F8F9FA"
    };

    public static string GetAccentColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "#4D9DE0",
        AppTheme.GlassDark => "#5DADE2",
        AppTheme.AcrylicLight => "#0078D4",
        AppTheme.AcrylicDark => "#0078D4",
        AppTheme.PureDark => "#5DADE2",
        AppTheme.OceanBreeze => "#0EA5E9",
        AppTheme.ForestCanopy => "#10B981",
        AppTheme.SunsetGlow => "#F59E0B",
        AppTheme.MidnightPurple => "#A78BFA",
        AppTheme.RoseGold => "#F43F5E",
        AppTheme.ArcticMint => "#14B8A6",
        _ => "#4D9DE0"
    };

    public static string GetForegroundColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassDark => "#E0E0E0",
        AppTheme.AcrylicDark => "#E0E0E0",
        AppTheme.PureDark => "#E0E0E0",
        AppTheme.MidnightPurple => "#E0E0E0",
        _ => "#2C3E50"
    };

    public static string GetToolbarBackgroundColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "#FAFBFC",
        AppTheme.GlassDark => "#2A2A2A",
        AppTheme.AcrylicLight => "#F9F9F9",
        AppTheme.AcrylicDark => "#333333",
        AppTheme.PureDark => "#252525",
        AppTheme.OceanBreeze => "#F0F9FF",
        AppTheme.ForestCanopy => "#F0FDF4",
        AppTheme.SunsetGlow => "#FFFBEB",
        AppTheme.MidnightPurple => "#3B2667",
        AppTheme.RoseGold => "#FFF5F7",
        AppTheme.ArcticMint => "#F0FDFA",
        _ => "#FAFBFC"
    };

    public static string GetBorderColor(this AppTheme theme) => theme switch
    {
        AppTheme.GlassLight => "#E0E0E0",
        AppTheme.GlassDark => "#3A3A3A",
        AppTheme.AcrylicLight => "#D0D0D0",
        AppTheme.AcrylicDark => "#3F3F3F",
        AppTheme.PureDark => "#3A3A3A",
        AppTheme.OceanBreeze => "#BAE6FD",
        AppTheme.ForestCanopy => "#A7F3D0",
        AppTheme.SunsetGlow => "#FDE68A",
        AppTheme.MidnightPurple => "#7C3AED",
        AppTheme.RoseGold => "#FECDD3",
        AppTheme.ArcticMint => "#5EEAD4",
        _ => "#E0E0E0"
    };
}
