using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareTracker.Models;
using HardwareTracker.Services;

namespace HardwareTracker.ViewModels;

/// <summary>
/// Main dashboard ViewModel managing asset inventory grids, metric summaries, and filter orchestrations.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAssetService _assetService;
    private readonly IDialogService _dialogService;
    private readonly IExportService _exportService;
    private readonly List<Asset> _allAssets = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadDataCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddAssetCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditAssetCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAssetCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportToCsvCommand))]
    [NotifyCanExecuteChangedFor(nameof(ManageEmployeesCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditAssetCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAssetCommand))]
    private Asset? _selectedAsset;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    [ObservableProperty]
    private string _selectedCategoryFilter = "All";

    [ObservableProperty]
    private int _totalAssetsCount;

    [ObservableProperty]
    private int _availableCount;

    [ObservableProperty]
    private int _assignedCount;

    [ObservableProperty]
    private int _inServiceCount;

    [ObservableProperty]
    private int _disposedCount;

    [ObservableProperty]
    private string _availablePercentage = "0%";

    [ObservableProperty]
    private string _assignedPercentage = "0%";

    [ObservableProperty]
    private string _inServicePercentage = "0%";

    [ObservableProperty]
    private string _disposedPercentage = "0%";

    public ObservableCollection<Asset> FilteredAssets { get; } = [];
    public ObservableCollection<string> StatusFilters { get; } = [];
    public ObservableCollection<string> CategoryFilters { get; } = [];

    public MainWindowViewModel(
        IAssetService assetService,
        IDialogService dialogService,
        IExportService exportService)
    {
        _assetService = assetService;
        _dialogService = dialogService;
        _exportService = exportService;

        InitializeFilterOptions();
        _ = LoadDataAsync();
    }

    private void InitializeFilterOptions()
    {
        StatusFilters.Add("All");
        foreach (var status in Enum.GetNames<AssetStatus>())
        {
            StatusFilters.Add(status);
        }

        CategoryFilters.Add("All");
    }

    private bool CanExecuteWhenIdle() => !IsLoading;
    private bool CanModifySelectedAsset() => !IsLoading && SelectedAsset is not null;

    [RelayCommand(CanExecute = nameof(CanExecuteWhenIdle))]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            _allAssets.Clear();

            var assets = await _assetService.GetAllAssetsAsync();
            _allAssets.AddRange(assets);

            UpdateMetrics();
            RefreshCategoryFilterOptions();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateMetrics()
    {
        TotalAssetsCount = _allAssets.Count;
        AvailableCount = _allAssets.Count(a => a.Status == AssetStatus.Available);
        AssignedCount = _allAssets.Count(a => a.Status == AssetStatus.Assigned);
        InServiceCount = _allAssets.Count(a => a.Status == AssetStatus.InService);
        DisposedCount = _allAssets.Count(a => a.Status == AssetStatus.Disposed);

        if (TotalAssetsCount > 0)
        {
            AvailablePercentage = $"{(double)AvailableCount / TotalAssetsCount * 100:0}%";
            AssignedPercentage = $"{(double)AssignedCount / TotalAssetsCount * 100:0}%";
            InServicePercentage = $"{(double)InServiceCount / TotalAssetsCount * 100:0}%";
            DisposedPercentage = $"{(double)DisposedCount / TotalAssetsCount * 100:0}%";
        }
        else
        {
            AvailablePercentage = "0%";
            AssignedPercentage = "0%";
            InServicePercentage = "0%";
            DisposedPercentage = "0%";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteWhenIdle))]
    public async Task AddAssetAsync()
    {
        var result = await _dialogService.ShowAssetEditorAsync();
        if (result)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelectedAsset))]
    public async Task EditAssetAsync()
    {
        if (SelectedAsset is null)
        {
            return;
        }

        var result = await _dialogService.ShowAssetEditorAsync(SelectedAsset);
        if (result)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelectedAsset))]
    public async Task DeleteAssetAsync()
    {
        if (SelectedAsset is null)
        {
            return;
        }

        // Prevent destructive action if the asset is currently in active possession.
        if (SelectedAsset.Status == AssetStatus.Assigned || SelectedAsset.AssignedEmployeeId.HasValue)
        {
            await _dialogService.ShowAlertAsync(
                "Action Blocked",
                $"Cannot delete {SelectedAsset.Model} ({SelectedAsset.SerialNumber}) because it is actively assigned to an employee. Reassign or return it first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Asset",
            $"Are you sure you want to permanently delete {SelectedAsset.Model} ({SelectedAsset.SerialNumber})? This will also remove its assignment history.",
            confirmText: "Delete",
            isDestructive: true);

        if (confirmed)
        {
            await _assetService.DeleteAssetAsync(SelectedAsset.Id);
            await LoadDataAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteWhenIdle))]
    public async Task ManageEmployeesAsync()
    {
        await _dialogService.ShowEmployeesManagerAsync();
        await LoadDataAsync();
    }

    [RelayCommand(CanExecute = nameof(CanExecuteWhenIdle))]
    public async Task ExportToCsvAsync()
    {
        if (FilteredAssets.Count == 0)
        {
            return;
        }

        var defaultFileName = $"Hardware_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var stream = await _dialogService.OpenSaveFileStreamAsync(defaultFileName);

        if (stream is null)
        {
            return;
        }

        await using (stream)
        {
            await _exportService.ExportAssetsToCsvAsync(FilteredAssets, stream);
        }
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SearchText = string.Empty;
        SelectedStatusFilter = "All";
        SelectedCategoryFilter = "All";
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryFilterChanged(string value) => ApplyFilter();

    private void RefreshCategoryFilterOptions()
    {
        var currentSelection = SelectedCategoryFilter;
        CategoryFilters.Clear();
        CategoryFilters.Add("All");

        var categories = _allAssets
            .Select(a => a.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c);

        foreach (var category in categories)
        {
            CategoryFilters.Add(category);
        }

        SelectedCategoryFilter = CategoryFilters.Contains(currentSelection) ? currentSelection : "All";
    }

    private void ApplyFilter()
    {
        var query = _allAssets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(a =>
                a.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.Model.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (a.AssignedEmployee != null && a.AssignedEmployee.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (SelectedStatusFilter != "All" && Enum.TryParse<AssetStatus>(SelectedStatusFilter, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (SelectedCategoryFilter != "All")
        {
            query = query.Where(a => string.Equals(a.Category, SelectedCategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        var targetSelectedId = SelectedAsset?.Id;
        var filteredList = query.ToList();

        FilteredAssets.Clear();
        foreach (var asset in filteredList)
        {
            FilteredAssets.Add(asset);
        }

        // Retain user selection by primary key identity across collection materializations.
        SelectedAsset = targetSelectedId.HasValue 
            ? FilteredAssets.FirstOrDefault(a => a.Id == targetSelectedId.Value) 
            : FilteredAssets.FirstOrDefault();
    }
}