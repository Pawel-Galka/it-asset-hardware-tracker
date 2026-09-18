using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using HardwareTracker.Models;
using HardwareTracker.ViewModels;
using HardwareTracker.Views;

namespace HardwareTracker.Services;

/// <summary>
/// Handles desktop window navigation, modal dialog cascades, and storage provider interactions.
/// </summary>
public sealed class DialogService(IAssetService assetService) : IDialogService
{
    public async Task<bool> ShowAssetEditorAsync(Asset? assetToEdit = null)
    {
        var owner = GetActiveWindow();
        if (owner is null)
        {
            return false;
        }

        var viewModel = new AssetEditViewModel(assetService, assetToEdit);
        var window = new AssetEditWindow
        {
            DataContext = viewModel
        };

        return await window.ShowDialog<bool>(owner);
    }

    public async Task<Stream?> OpenSaveFileStreamAsync(string defaultFileName)
    {
        var owner = GetActiveWindow();
        if (owner is null)
        {
            return null;
        }

        var storageProvider = owner.StorageProvider;
        if (!storageProvider.CanSave)
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Assets to CSV",
            SuggestedFileName = defaultFileName,
            DefaultExtension = "csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV Document (*.csv)")
                {
                    Patterns = ["*.csv"]
                }
            ]
        });

        if (file is null)
        {
            return null;
        }

        return await file.OpenWriteAsync();
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirm", bool isDestructive = false)
    {
        var owner = GetActiveWindow();
        if (owner is null)
        {
            return false;
        }

        var dialog = new ConfirmationWindow
        {
            DataContext = new ConfirmationViewModel(title, message, confirmText, isAlert: false, isDestructive: isDestructive)
        };

        return await dialog.ShowDialog<bool>(owner);
    }

    public async Task ShowAlertAsync(string title, string message)
    {
        var owner = GetActiveWindow();
        if (owner is null)
        {
            return;
        }

        var dialog = new ConfirmationWindow
        {
            DataContext = new ConfirmationViewModel(title, message, isAlert: true)
        };

        await dialog.ShowDialog<bool>(owner);
    }

    public async Task ShowEmployeesManagerAsync()
    {
        var owner = GetActiveWindow();
        if (owner is null)
        {
            return;
        }

        var viewModel = new EmployeesViewModel(assetService, this);
        var window = new EmployeesWindow
        {
            DataContext = viewModel
        };

        await window.ShowDialog(owner);
    }

    // Resolves the active top-level window to preserve correct modal ownership and prevent desktop z-order deadlocks.
    private static Window? GetActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        return desktop.Windows.LastOrDefault(w => w.IsActive) ?? desktop.MainWindow;
    }
}