using System.Threading.Tasks;

namespace MarkdownViewer.Services;

public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <returns>True if user clicked Yes, false otherwise</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows an information dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    Task ShowMessageAsync(string title, string message);
}
