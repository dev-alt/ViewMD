namespace MarkdownViewer.Models;

public record EditorState
{
    public int CursorLine { get; init; }
    public int CursorColumn { get; init; }
    public int WordCount { get; init; }
    public int CharCount { get; init; }
    public int SelectionStart { get; init; }
    public int SelectionLength { get; init; }
}
