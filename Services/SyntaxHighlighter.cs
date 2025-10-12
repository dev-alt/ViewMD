using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarkdownViewer.Services;

public static class SyntaxHighlighter
{
    private static readonly Dictionary<string, LanguageDefinition> Languages = new()
    {
        ["csharp"] = new LanguageDefinition
        {
            Keywords = new[] { "class", "public", "private", "protected", "internal", "static", "void", "int", "string", "bool", "double", "float", "var", "const", "readonly", "if", "else", "for", "foreach", "while", "do", "switch", "case", "break", "continue", "return", "new", "this", "base", "null", "true", "false", "namespace", "using", "async", "await", "task", "override", "virtual", "abstract" },
            StringPattern = @"(@""(?:""""|[^""])*""|""(?:\\.|[^\\""])*"")",
            CommentPattern = @"(//.*?$|/\*.*?\*/)",
            NumberPattern = @"\b\d+\.?\d*\b"
        },
        ["cs"] = new LanguageDefinition { Keywords = new[] { "class", "public", "private", "protected", "internal", "static", "void", "int", "string", "bool", "double", "float", "var", "const", "readonly", "if", "else", "for", "foreach", "while", "do", "switch", "case", "break", "continue", "return", "new", "this", "base", "null", "true", "false", "namespace", "using", "async", "await", "task", "override", "virtual", "abstract" }, StringPattern = @"(@""(?:""""|[^""])*""|""(?:\\.|[^\\""])*"")", CommentPattern = @"(//.*?$|/\*.*?\*/)", NumberPattern = @"\b\d+\.?\d*\b" },
        ["javascript"] = new LanguageDefinition
        {
            Keywords = new[] { "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "return", "class", "extends", "constructor", "this", "new", "null", "undefined", "true", "false", "async", "await", "import", "export", "default", "from" },
            StringPattern = @"(`(?:\\.|[^\\`])*`|'(?:\\.|[^\\'])*'|""(?:\\.|[^\\""])*"")",
            CommentPattern = @"(//.*?$|/\*.*?\*/)",
            NumberPattern = @"\b\d+\.?\d*\b"
        },
        ["js"] = new LanguageDefinition { Keywords = new[] { "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "return", "class", "extends", "constructor", "this", "new", "null", "undefined", "true", "false", "async", "await", "import", "export", "default", "from" }, StringPattern = @"(`(?:\\.|[^\\`])*`|'(?:\\.|[^\\'])*'|""(?:\\.|[^\\""])*"")", CommentPattern = @"(//.*?$|/\*.*?\*/)", NumberPattern = @"\b\d+\.?\d*\b" },
        ["python"] = new LanguageDefinition
        {
            Keywords = new[] { "def", "class", "if", "elif", "else", "for", "while", "break", "continue", "return", "import", "from", "as", "try", "except", "finally", "with", "lambda", "yield", "None", "True", "False", "and", "or", "not", "in", "is", "pass", "raise", "assert", "del", "global", "nonlocal", "async", "await" },
            StringPattern = @"("""""".*?""""""|'''.*?'''|""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*')",
            CommentPattern = @"#.*?$",
            NumberPattern = @"\b\d+\.?\d*\b"
        },
        ["py"] = new LanguageDefinition { Keywords = new[] { "def", "class", "if", "elif", "else", "for", "while", "break", "continue", "return", "import", "from", "as", "try", "except", "finally", "with", "lambda", "yield", "None", "True", "False", "and", "or", "not", "in", "is", "pass", "raise", "assert", "del", "global", "nonlocal", "async", "await" }, StringPattern = @"("""""".*?""""""|'''.*?'''|""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*')", CommentPattern = @"#.*?$", NumberPattern = @"\b\d+\.?\d*\b" },
        ["java"] = new LanguageDefinition
        {
            Keywords = new[] { "public", "private", "protected", "class", "interface", "enum", "extends", "implements", "void", "int", "boolean", "String", "double", "float", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "return", "new", "this", "super", "null", "true", "false", "static", "final", "abstract", "try", "catch", "finally", "throw", "throws" },
            StringPattern = @"""(?:\\.|[^\\""])*""",
            CommentPattern = @"(//.*?$|/\*.*?\*/)",
            NumberPattern = @"\b\d+\.?\d*\b"
        },
        ["typescript"] = new LanguageDefinition
        {
            Keywords = new[] { "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "return", "class", "interface", "type", "extends", "implements", "constructor", "this", "new", "null", "undefined", "true", "false", "async", "await", "import", "export", "default", "from", "as", "public", "private", "protected", "readonly" },
            StringPattern = @"(`(?:\\.|[^\\`])*`|'(?:\\.|[^\\'])*'|""(?:\\.|[^\\""])*"")",
            CommentPattern = @"(//.*?$|/\*.*?\*/)",
            NumberPattern = @"\b\d+\.?\d*\b"
        },
        ["ts"] = new LanguageDefinition { Keywords = new[] { "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "return", "class", "interface", "type", "extends", "implements", "constructor", "this", "new", "null", "undefined", "true", "false", "async", "await", "import", "export", "default", "from", "as", "public", "private", "protected", "readonly" }, StringPattern = @"(`(?:\\.|[^\\`])*`|'(?:\\.|[^\\'])*'|""(?:\\.|[^\\""])*"")", CommentPattern = @"(//.*?$|/\*.*?\*/)", NumberPattern = @"\b\d+\.?\d*\b" },
        ["html"] = new LanguageDefinition
        {
            Keywords = new[] { "div", "span", "p", "a", "img", "table", "tr", "td", "th", "ul", "ol", "li", "h1", "h2", "h3", "h4", "h5", "h6", "head", "body", "html", "script", "style", "link", "meta", "title" },
            StringPattern = @"""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*'",
            CommentPattern = @"<!--.*?-->",
            NumberPattern = @"\b\d+\.?\d*\b"
        },
        ["css"] = new LanguageDefinition
        {
            Keywords = new[] { "color", "background", "margin", "padding", "border", "width", "height", "display", "position", "flex", "grid", "font", "text", "align", "justify" },
            StringPattern = @"""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*'",
            CommentPattern = @"/\*.*?\*/",
            NumberPattern = @"\b\d+\.?\d*(px|em|rem|%|vh|vw)?\b"
        }
    };

    public static TextBlock CreateHighlightedBlock(string code, string? language, bool isDarkTheme)
    {
        var textBlock = new TextBlock
        {
            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
            FontSize = 13,
            TextWrapping = TextWrapping.NoWrap
        };

        var lang = language?.ToLower().Trim() ?? "";

        if (!Languages.TryGetValue(lang, out var langDef))
        {
            // No highlighting, just plain text
            textBlock.Text = code;
            textBlock.Foreground = new SolidColorBrush(isDarkTheme ? Color.Parse("#D4D4D4") : Color.Parse("#333333"));
            return textBlock;
        }

        var inlines = new List<Avalonia.Controls.Documents.InlineUIContainer>();
        var lines = code.Split('\n');

        foreach (var line in lines)
        {
            if (textBlock.Inlines != null)
            {
                HighlightLine(line, langDef, isDarkTheme, textBlock.Inlines);
                if (line != lines[^1])
                {
                    textBlock.Inlines.Add(new Avalonia.Controls.Documents.Run("\n"));
                }
            }
        }

        return textBlock;
    }

    private static void HighlightLine(string line, LanguageDefinition langDef, bool isDarkTheme, Avalonia.Controls.Documents.InlineCollection inlines)
    {
        var keywordColor = isDarkTheme ? Color.Parse("#569CD6") : Color.Parse("#0000FF");
        var stringColor = isDarkTheme ? Color.Parse("#CE9178") : Color.Parse("#A31515");
        var commentColor = isDarkTheme ? Color.Parse("#6A9955") : Color.Parse("#008000");
        var numberColor = isDarkTheme ? Color.Parse("#B5CEA8") : Color.Parse("#098658");
        var defaultColor = isDarkTheme ? Color.Parse("#D4D4D4") : Color.Parse("#333333");

        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        // Find all comments, strings, numbers, and keywords with their positions
        var tokens = new List<Token>();

        // Comments (highest priority)
        foreach (Match match in Regex.Matches(line, langDef.CommentPattern, RegexOptions.Multiline))
        {
            tokens.Add(new Token { Start = match.Index, End = match.Index + match.Length, Color = commentColor, Type = TokenType.Comment });
        }

        // Strings (second priority)
        foreach (Match match in Regex.Matches(line, langDef.StringPattern))
        {
            // Don't highlight if inside a comment
            if (!tokens.Any(t => t.Type == TokenType.Comment && match.Index >= t.Start && match.Index < t.End))
            {
                tokens.Add(new Token { Start = match.Index, End = match.Index + match.Length, Color = stringColor, Type = TokenType.String });
            }
        }

        // Numbers
        foreach (Match match in Regex.Matches(line, langDef.NumberPattern))
        {
            if (!tokens.Any(t => (t.Type == TokenType.Comment || t.Type == TokenType.String) && match.Index >= t.Start && match.Index < t.End))
            {
                tokens.Add(new Token { Start = match.Index, End = match.Index + match.Length, Color = numberColor, Type = TokenType.Number });
            }
        }

        // Keywords
        foreach (var keyword in langDef.Keywords)
        {
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            foreach (Match match in Regex.Matches(line, pattern, RegexOptions.IgnoreCase))
            {
                if (!tokens.Any(t => match.Index >= t.Start && match.Index < t.End))
                {
                    tokens.Add(new Token { Start = match.Index, End = match.Index + match.Length, Color = keywordColor, Type = TokenType.Keyword });
                }
            }
        }

        // Sort tokens by position
        tokens = tokens.OrderBy(t => t.Start).ToList();

        // Build the highlighted line
        int currentPos = 0;
        foreach (var token in tokens)
        {
            // Add text before token
            if (token.Start > currentPos)
            {
                inlines.Add(new Avalonia.Controls.Documents.Run(line.Substring(currentPos, token.Start - currentPos))
                {
                    Foreground = new SolidColorBrush(defaultColor)
                });
            }

            // Add token
            inlines.Add(new Avalonia.Controls.Documents.Run(line.Substring(token.Start, token.End - token.Start))
            {
                Foreground = new SolidColorBrush(token.Color)
            });

            currentPos = token.End;
        }

        // Add remaining text
        if (currentPos < line.Length)
        {
            inlines.Add(new Avalonia.Controls.Documents.Run(line.Substring(currentPos))
            {
                Foreground = new SolidColorBrush(defaultColor)
            });
        }
    }

    private class LanguageDefinition
    {
        public required string[] Keywords { get; set; }
        public required string StringPattern { get; set; }
        public required string CommentPattern { get; set; }
        public required string NumberPattern { get; set; }
    }

    private class Token
    {
        public int Start { get; set; }
        public int End { get; set; }
        public Color Color { get; set; }
        public TokenType Type { get; set; }
    }

    private enum TokenType
    {
        Keyword,
        String,
        Comment,
        Number
    }
}
