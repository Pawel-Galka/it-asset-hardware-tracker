using System.IO;
using System.Threading.Tasks;
using HardwareTracker.Models;

namespace HardwareTracker.Services;

/// <summary>
/// Abstracts window management, modal dialogs, and native file system storage pickers for Avalonia UI.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Displays the modal asset editor window for creating or updating hardware records.
    /// </summary>
    Task<bool> ShowAssetEditorAsync(Asset? assetToEdit = null);

    /// <summary>
    /// Opens a writable file stream via the platform save file dialog. Caller is responsible for disposing the stream.
    /// </summary>
    Task<Stream?> OpenSaveFileStreamAsync(string defaultFileName);

    /// <summary>
    /// Displays a modal confirmation prompt and returns true if confirmed by the user.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirm", bool isDestructive = false);

    /// <summary>
    /// Displays an informational alert modal dialog.
    /// </summary>
    Task ShowAlertAsync(string title, string message);

    /// <summary>
    /// Displays the modal employee and offboarding management window.
    /// </summary>
    Task ShowEmployeesManagerAsync();
}