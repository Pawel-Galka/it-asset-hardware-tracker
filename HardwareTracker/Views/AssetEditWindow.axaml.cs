using System;
using Avalonia.Controls;
using HardwareTracker.ViewModels;

namespace HardwareTracker.Views;

/// <summary>
/// Code-behind managing modal window lifecycle and ViewModel close event synchronization.
/// </summary>
public partial class AssetEditWindow : Window
{
    private AssetEditViewModel? _boundViewModel;

    public AssetEditWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Detach prior handler to prevent event reference retention when DataContext is rebound.
        if (_boundViewModel is not null)
        {
            _boundViewModel.RequestClose -= OnRequestClose;
            _boundViewModel = null;
        }

        if (DataContext is AssetEditViewModel viewModel)
        {
            _boundViewModel = viewModel;
            _boundViewModel.RequestClose += OnRequestClose;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Guard against memory leaks by releasing the strong reference held by the event subscription.
        if (_boundViewModel is not null)
        {
            _boundViewModel.RequestClose -= OnRequestClose;
            _boundViewModel = null;
        }
    }

    private void OnRequestClose(bool result)
    {
        Close(result);
    }
}