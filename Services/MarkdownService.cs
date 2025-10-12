using System.Threading;
using System.Threading.Tasks;
using Markdig;

namespace MarkdownViewer.Services;

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()  // Includes most GFM features
            .UseEmojiAndSmiley()       // :emoji: syntax
            .UseAutoLinks()            // Auto-detect URLs
            .UseGenericAttributes()    // {#id .class} syntax
            .UseDefinitionLists()      // Term : Definition
            .UseFootnotes()            // [^1] references
            .UseAbbreviations()        // *[HTML]: Full name
            .UsePipeTables()           // | tables |
            .UseGridTables()           // +---+---+ tables
            .UseTaskLists()            // - [ ] and - [x]
            .UseAutoIdentifiers()      // Auto-generate heading IDs
            .UseMediaLinks()           // Audio/video support
            .UseSmartyPants()          // Smart quotes and dashes
            .UseMathematics()          // $math$ and $$math$$
            .UseDiagrams()             // Mermaid diagrams
            .UseYamlFrontMatter()      // --- metadata ---
            .UseEmphasisExtras()       // ^super^, ~sub~, ==mark==, ++ins++
            .UseCustomContainers()     // ::: note ::: for callouts/alerts
            .UseFigures()              // ^^ caption syntax
            .UseCitations()            // [@ref] citations
            // Note: GitHub-style alerts (> [!NOTE]) require Markdig 0.38+
            // Currently using custom containers (::: note :::) as alternative
            .Build();
    }

    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return Markdown.ToHtml(markdown, _pipeline);
    }

    public Task<string> RenderToHtmlAsync(string markdown, CancellationToken ct = default)
    {
        return Task.Run(() => RenderToHtml(markdown), ct);
    }

    public string GeneratePreviewHtml(string markdownHtml, bool isDarkTheme)
    {
        var theme = isDarkTheme ? "dark" : "light";
        var bgColor = isDarkTheme ? "#1e1e1e" : "#ffffff";
        var textColor = isDarkTheme ? "#e0e0e0" : "#000000";
        var borderColor = isDarkTheme ? "#404040" : "#e0e0e0";
        var codeBlockBg = isDarkTheme ? "#2d2d2d" : "#f5f5f5";
        var linkColor = isDarkTheme ? "#58a6ff" : "#0066cc";
        var mutedTextColor = isDarkTheme ? "#888888" : "#6a737d";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{
            box-sizing: border-box;
        }}

        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica', 'Arial', sans-serif;
            line-height: 1.6;
            color: {textColor};
            background-color: {bgColor};
            padding: 2rem;
            margin: 0;
            max-width: min(900px, 100%);
            margin-left: auto;
            margin-right: auto;
        }}

        @media (max-width: 768px) {{
            body {{
                padding: 1rem;
                font-size: 14px;
            }}
        }}

        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}

        h1 {{ font-size: 2em; border-bottom: 1px solid {borderColor}; padding-bottom: 0.3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid {borderColor}; padding-bottom: 0.3em; }}
        h3 {{ font-size: 1.25em; }}
        h4 {{ font-size: 1em; }}
        h5 {{ font-size: 0.875em; }}
        h6 {{ font-size: 0.85em; color: #6a737d; }}

        a {{
            color: {linkColor};
            text-decoration: none;
        }}

        a:hover {{
            text-decoration: underline;
        }}

        p {{
            margin-top: 0;
            margin-bottom: 16px;
        }}

        blockquote {{
            margin: 0;
            padding: 0 1em;
            color: #6a737d;
            border-left: 0.25em solid {borderColor};
        }}

        code {{
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            font-size: 85%;
            padding: 0.2em 0.4em;
            background-color: {codeBlockBg};
            border-radius: 3px;
        }}

        pre {{
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            font-size: 85%;
            line-height: 1.45;
            background-color: {codeBlockBg};
            border-radius: 3px;
            padding: 16px;
            overflow-x: auto;
            overflow-y: hidden;
            max-width: 100%;
        }}

        pre code {{
            background-color: transparent;
            padding: 0;
            border: none;
        }}

        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 16px;
            display: block;
            overflow-x: auto;
        }}

        table th, table td {{
            padding: 6px 13px;
            border: 1px solid {borderColor};
        }}

        table th {{
            font-weight: 600;
            background-color: {codeBlockBg};
        }}

        table tr:nth-child(even) {{
            background-color: {codeBlockBg};
        }}

        ul, ol {{
            margin-top: 0;
            margin-bottom: 16px;
            padding-left: 2em;
        }}

        li + li {{
            margin-top: 0.25em;
        }}

        ul.contains-task-list {{
            list-style: none;
            padding-left: 0;
        }}

        .task-list-item {{
            list-style-type: none;
        }}

        .task-list-item input[type=""checkbox""] {{
            margin-right: 0.5em;
        }}

        hr {{
            height: 0.25em;
            padding: 0;
            margin: 24px 0;
            background-color: {borderColor};
            border: 0;
        }}

        img {{
            max-width: 100%;
            height: auto;
            display: block;
            margin: 1rem 0;
        }}

        kbd {{
            display: inline-block;
            padding: 3px 5px;
            font-size: 11px;
            line-height: 10px;
            color: {textColor};
            vertical-align: middle;
            background-color: {codeBlockBg};
            border: solid 1px {borderColor};
            border-bottom-color: {borderColor};
            border-radius: 3px;
            box-shadow: inset 0 -1px 0 {borderColor};
        }}

        mark {{
            background-color: #fff3cd;
            color: #000;
            padding: 0.2em;
        }}

        del {{
            text-decoration: line-through;
        }}

        /* Superscript and subscript */
        sup, sub {{
            font-size: 0.75em;
            line-height: 0;
            position: relative;
            vertical-align: baseline;
        }}

        sup {{
            top: -0.5em;
        }}

        sub {{
            bottom: -0.25em;
        }}

        /* Inserted text */
        ins {{
            text-decoration: underline;
            text-decoration-color: {linkColor};
            text-decoration-thickness: 2px;
        }}

        /* Figures and captions */
        figure {{
            margin: 1.5rem 0;
            text-align: center;
        }}

        figure img {{
            display: inline-block;
            margin: 0.5rem auto;
        }}

        figcaption {{
            font-size: 0.9em;
            color: {mutedTextColor};
            font-style: italic;
            margin-top: 0.5rem;
        }}

        /* Definition lists */
        dl {{
            margin-bottom: 16px;
        }}

        dt {{
            font-weight: 600;
            margin-top: 16px;
            font-size: 1em;
        }}

        dd {{
            margin-left: 2em;
            margin-bottom: 8px;
            color: {mutedTextColor};
        }}

        /* Custom containers */
        .custom-container {{
            padding: 1rem;
            margin: 1rem 0;
            border-left: 4px solid {borderColor};
            background-color: {codeBlockBg};
            border-radius: 4px;
        }}

        .custom-container.note {{
            border-left-color: #0969da;
            background-color: {(isDarkTheme ? "#1f2d3d" : "#ddf4ff")};
        }}

        .custom-container.tip {{
            border-left-color: #1a7f37;
            background-color: {(isDarkTheme ? "#1f2d24" : "#d1f5d3")};
        }}

        .custom-container.important {{
            border-left-color: #8250df;
            background-color: {(isDarkTheme ? "#2d2440" : "#f0e7ff")};
        }}

        .custom-container.warning {{
            border-left-color: #d29922;
            background-color: {(isDarkTheme ? "#3d2f1f" : "#fff8dc")};
        }}

        .custom-container.caution {{
            border-left-color: #cf222e;
            background-color: {(isDarkTheme ? "#3d1f24" : "#ffe8ea")};
        }}

        /* Citations */
        .citation {{
            font-size: 0.9em;
            color: {linkColor};
            text-decoration: none;
            vertical-align: super;
        }}

        .citation:hover {{
            text-decoration: underline;
        }}

        /* Footnotes section */
        .footnotes {{
            margin-top: 2rem;
            padding-top: 1rem;
            border-top: 1px solid {borderColor};
            font-size: 0.9em;
        }}

        .footnotes ol {{
            padding-left: 1.5rem;
        }}

        /* Abbreviations */
        abbr[title] {{
            text-decoration: underline dotted;
            cursor: help;
            border-bottom: 1px dotted {mutedTextColor};
        }}

        /* Mermaid diagrams */
        .mermaid {{
            background-color: {bgColor};
            text-align: center;
        }}
    </style>
    <script src=""https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js""></script>
    <script>
        mermaid.initialize({{
            startOnLoad: true,
            theme: '{theme}'
        }});
    </script>
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.css"">
    <script defer src=""https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.js""></script>
    <script defer src=""https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/contrib/auto-render.min.js""
        onload=""renderMathInElement(document.body);""></script>
</head>
<body>
{markdownHtml}
</body>
</html>";
    }

    public MarkdownPipeline GetPipeline() => _pipeline;
}
