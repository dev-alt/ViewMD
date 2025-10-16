# Avalonia Accelerate License Information

## IMPORTANT: WebView Requires Paid License

The WebView component is **NOT** included in the Community Edition (free tier).

### What's Included Where:

**Community Edition (FREE):**
- ✅ Visual Studio extension
- ✅ Dev Tools application
- ✅ Parcel packaging tool
- ❌ **WebView component (requires paid license)**
- ❌ MediaPlayer component
- ❌ Markdown viewer component

**Business/Enterprise License (PAID):**
- ✅ All Community Edition tools
- ✅ **WebView component**
- ✅ MediaPlayer component
- ✅ Markdown viewer component
- ✅ Technical support

## Licensing Options for WebView

### Steps to Get Your License:

1. Visit **https://avaloniaui.net/accelerate**
2. Sign up for the **Community Edition** (FREE)
3. Get your license key from the portal
4. You can also use the **30-day free trial** to evaluate all features

## Setting Up Your License Key

**IMPORTANT:** Never commit your license key to source control!

### Option 1: Environment Variable (Recommended)

Set the environment variable before building:

**Windows (PowerShell):**
```powershell
$env:AVALONIA_LICENSE_KEY="your_license_key_here"
dotnet build
dotnet run
```

**Windows (Command Prompt):**
```cmd
set AVALONIA_LICENSE_KEY=your_license_key_here
dotnet build
dotnet run
```

**Linux/macOS:**
```bash
export AVALONIA_LICENSE_KEY="your_license_key_here"
dotnet build
dotnet run
```

### Option 2: User-Specific Settings File (Permanent)

Create a file `Directory.Build.props` in your **user profile** (NOT in the project):

**Windows:** `C:\Users\YourName\Directory.Build.props`
**Linux/macOS:** `~/.dotnet/Directory.Build.props`

Add this content:
```xml
<Project>
  <ItemGroup>
    <AvaloniaUILicenseKey Include="your_license_key_here" />
  </ItemGroup>
</Project>
```

This will apply to all your projects without exposing the key in source control.

## What You Get With the License

- **WebView component** for rendering markdown with perfect browser-like text selection
- Full Mermaid diagram support
- KaTeX math rendering
- Syntax highlighting
- All for FREE with Community Edition!

## Troubleshooting

If you see error `AVLIC0001: No valid AvaloniaUI subscription keys found`:
1. Make sure you've set the `AVALONIA_LICENSE_KEY` environment variable
2. Restart your terminal/IDE after setting the environment variable
3. Verify the key is correct (starts with `avln_`)

## More Information

- Avalonia Accelerate: https://avaloniaui.net/accelerate
- Documentation: https://docs.avaloniaui.net/accelerate
- Community Edition Details: https://avaloniaui.net/blog/building-a-sustainable-future-for-avalonia
