namespace HardwareTracker.ViewModels;

/// <summary>
/// Immutable presentation model backing modal alert and confirmation dialogs.
/// </summary>
public sealed class ConfirmationViewModel(
    string title,
    string message,
    string confirmText = "Confirm",
    bool isAlert = false,
    bool isDestructive = false) : ViewModelBase
{
    public string Title { get; } = title;
    public string Message { get; } = message;
    public string ConfirmText { get; } = confirmText;
    public bool IsAlert { get; } = isAlert;

    // Derived flags govern visibility and semantic styling of primary modal actions in Avalonia views.
    public bool IsConfirmation => !IsAlert;
    public bool ShowDangerConfirm => IsConfirmation && isDestructive;
    public bool ShowAccentConfirm => IsConfirmation && !isDestructive;
}