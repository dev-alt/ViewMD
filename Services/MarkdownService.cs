using System.Threading;
using System.Threading.Tasks;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Tables;

namespace MarkdownViewer.Services;

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseAutoLinks()
            .UseGenericAttributes()
            .UseDefinitionLists()
            .UseFootnotes()
            .UseAbbreviations()
            .UsePipeTables()
            .UseGridTables()
            .UseTaskLists()
            .UseAutoIdentifiers()
            .UseMediaLinks()
            .UseSmartyPants()
            .UseMathematics()
            .UseDiagrams()
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
        var textColor = isDarkTheme ? "#d4d4d4" : "#000000";
        var borderColor = isDarkTheme ? "#3e3e3e" : "#e0e0e0";
        var codeBlockBg = isDarkTheme ? "#2d2d2d" : "#f5f5f5";
        var linkColor = isDarkTheme ? "#4FC3F7" : "#0066cc";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica', 'Arial', sans-serif;
            line-height: 1.6;
            color: {textColor};
            background-color: {bgColor};
            padding: 20px;
            margin: 0;
            max-width: 900px;
            margin-left: auto;
            margin-right: auto;
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
            overflow: auto;
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
