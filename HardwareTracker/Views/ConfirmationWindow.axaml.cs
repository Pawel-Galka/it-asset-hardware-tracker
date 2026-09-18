using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HardwareTracker.Views;

/// <summary>
/// Generic modal dialog view for confirmations and alerts, returning a boolean result upon dismissal.
/// </summary>
public sealed partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
    {
        InitializeComponent();
    }

    // Direct result assignment in code-behind is standard practice for generic modal dialogs to bypass ViewModel coupling.
    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}