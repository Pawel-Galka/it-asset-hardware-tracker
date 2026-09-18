using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareTracker.Models;
using HardwareTracker.Services;

namespace HardwareTracker.ViewModels;

/// <summary>
/// Manages asset creation, metadata edits, and bidirectional employee assignment state synchronization.
/// </summary>
public sealed partial class AssetEditViewModel : ObservableValidator
{
    private readonly IAssetService _assetService;
    private readonly int? _assetId;
    private bool _isSynchronizingState;
    private bool _hasAttemptedSave;

    [ObservableProperty]
    [Required(ErrorMessage = "Serial Number is required.")]
    [MinLength(3, ErrorMessage = "Serial Number must be at least 3 characters.")]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Model is required.")]
    private string _model = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Category is required.")]
    private string? _category;

    [ObservableProperty]
    private AssetStatus _status = AssetStatus.Available;

    [ObservableProperty]
    [CustomValidation(typeof(AssetEditViewModel), nameof(ValidateEmployeeAssignment))]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private string? _generalError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    private bool _isSaving;

    public bool CanEdit => !IsLoading && !IsSaving;

    // Static read-only collections eliminate redundant collection allocation and observer overhead for constant data.
    public static IReadOnlyList<string> Categories { get; } =
    [
        "Laptop",
        "Desktop",
        "Monitor",
        "Smartphone",
        "Peripheral",
        "Network Device",
        "Server",
        "Other"
    ];

    public static IReadOnlyList<AssetStatus> StatusList { get; } = Enum.GetValues<AssetStatus>();

    public ObservableCollection<Employee> Employees { get; } = [];

    public event Action<bool>? RequestClose;

    public AssetEditViewModel(IAssetService assetService, Asset? assetToEdit = null)
    {
        _assetService = assetService;

        if (assetToEdit is not null)
        {
            _assetId = assetToEdit.Id;
            _serialNumber = assetToEdit.SerialNumber;
            _model = assetToEdit.Model;
            _category = assetToEdit.Category;
            _status = assetToEdit.Status;
        }

        _ = InitializeAsync(assetToEdit?.AssignedEmployeeId);
    }

    private async Task InitializeAsync(int? assignedEmployeeId)
    {
        IsLoading = true;
        GeneralError = null;

        try
        {
            var employeeList = await _assetService.GetActiveEmployeesAsync(assignedEmployeeId);
            Employees.Clear();

            foreach (var employee in employeeList)
            {
                Employees.Add(employee);
            }

            if (assignedEmployeeId.HasValue)
            {
                // Suppress event cycles while populating controls from existing entity data.
                _isSynchronizingState = true;
                SelectedEmployee = Employees.FirstOrDefault(e => e.Id == assignedEmployeeId.Value);
                _isSynchronizingState = false;
            }
        }
        catch (Exception ex)
        {
            GeneralError = $"Failed to load active employees: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSerialNumberChanged(string value)
    {
        if (_hasAttemptedSave)
        {
            ValidateProperty(value, nameof(SerialNumber));
        }
    }

    partial void OnModelChanged(string value)
    {
        if (_hasAttemptedSave)
        {
            ValidateProperty(value, nameof(Model));
        }
    }

    partial void OnCategoryChanged(string? value)
    {
        if (_hasAttemptedSave)
        {
            ValidateProperty(value, nameof(Category));
        }
    }

    partial void OnSelectedEmployeeChanged(Employee? value)
    {
        if (_isSynchronizingState)
        {
            return;
        }

        _isSynchronizingState = true;
        if (value is not null && Status != AssetStatus.Assigned)
        {
            Status = AssetStatus.Assigned;
        }
        else if (value is null && Status == AssetStatus.Assigned)
        {
            Status = AssetStatus.Available;
        }
        _isSynchronizingState = false;

        if (_hasAttemptedSave)
        {
            ValidateProperty(value, nameof(SelectedEmployee));
        }
    }

    partial void OnStatusChanged(AssetStatus value)
    {
        if (_isSynchronizingState)
        {
            return;
        }

        _isSynchronizingState = true;
        if (value != AssetStatus.Assigned && SelectedEmployee is not null)
        {
            SelectedEmployee = null;
        }
        _isSynchronizingState = false;

        if (_hasAttemptedSave)
        {
            ValidateProperty(SelectedEmployee, nameof(SelectedEmployee));
        }
    }

    public static ValidationResult? ValidateEmployeeAssignment(Employee? employee, ValidationContext context)
    {
        var instance = (AssetEditViewModel)context.ObjectInstance;

        if (instance.Status == AssetStatus.Assigned && employee is null)
        {
            return new ValidationResult("An employee must be selected when status is set to Assigned.");
        }

        if (instance.Status != AssetStatus.Assigned && employee is not null)
        {
            return new ValidationResult("Employees can only be assigned when status is Assigned.");
        }

        return ValidationResult.Success;
    }

    [RelayCommand]
    public void ClearEmployee()
    {
        SelectedEmployee = null;
    }

    private bool CanSave() => CanEdit;

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task SaveAsync()
    {
        if (!CanEdit)
        {
            return;
        }

        _hasAttemptedSave = true;
        GeneralError = null;
        ValidateAllProperties();

        if (HasErrors)
        {
            GeneralError = "Please fill in all required fields correctly.";
            return;
        }

        IsSaving = true;

        try
        {
            var isUnique = await _assetService.IsSerialNumberUniqueAsync(SerialNumber, _assetId);
            if (!isUnique)
            {
                GeneralError = "Serial Number already exists in the database.";
                return;
            }

            var asset = new Asset
            {
                Id = _assetId ?? 0,
                SerialNumber = SerialNumber.Trim(),
                Model = Model.Trim(),
                Category = Category!.Trim(),
                Status = Status
            };

            await _assetService.SaveAssetAsync(asset, SelectedEmployee?.Id);
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            GeneralError = $"An error occurred while saving: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}