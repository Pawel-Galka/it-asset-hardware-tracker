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
/// Orchestrates employee roster management, employment lifecycle actions, and form validation state.
/// </summary>
public sealed partial class EmployeesViewModel : ObservableValidator
{
    private readonly IAssetService _assetService;
    private readonly IDialogService _dialogService;
    private readonly List<Employee> _allEmployees = [];
    private int? _editingEmployeeId;

    [ObservableProperty]
    [Required(ErrorMessage = "First name is required.")]
    private string _firstName = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Last name is required.")]
    private string _lastName = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    private string _email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Department is required.")]
    private string _department = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _showInactive = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private bool _isEditing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    public string FormTitle => IsEditing ? "Edit Employee" : "Add New Employee";
    public string SaveButtonText => IsEditing ? "Save Changes" : "Add Employee";

    public ObservableCollection<Employee> DisplayedEmployees { get; } = [];

    public EmployeesViewModel(IAssetService assetService, IDialogService dialogService)
    {
        _assetService = assetService;
        _dialogService = dialogService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadEmployeesAsync();
    }

    public async Task LoadEmployeesAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            _allEmployees.Clear();
            var list = await _assetService.GetAllEmployeesAsync();
            _allEmployees.AddRange(list);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load employees: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnShowInactiveChanged(bool value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        DisplayedEmployees.Clear();
        var query = ShowInactive 
            ? _allEmployees 
            : _allEmployees.Where(e => e.IsActive);

        foreach (var employee in query)
        {
            DisplayedEmployees.Add(employee);
        }
    }

    [RelayCommand]
    public void StartEdit(Employee employee)
    {
        _editingEmployeeId = employee.Id;
        FirstName = employee.FirstName;
        LastName = employee.LastName;
        Email = employee.Email;
        Department = employee.Department;
        ErrorMessage = null;
        ClearErrors();
        IsEditing = true;
    }

    [RelayCommand]
    public void CancelEdit()
    {
        _editingEmployeeId = null;
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        Department = string.Empty;
        ErrorMessage = null;
        ClearErrors();
        IsEditing = false;
    }

    private bool CanSave() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        ValidateAllProperties();

        if (HasErrors)
        {
            ErrorMessage = "Please fill in all employee fields correctly.";
            return;
        }

        IsBusy = true;

        try
        {
            var isUnique = await _assetService.IsEmailUniqueAsync(Email, _editingEmployeeId);
            if (!isUnique)
            {
                ErrorMessage = "An employee with this email address already exists.";
                return;
            }

            if (IsEditing && _editingEmployeeId.HasValue)
            {
                var updatedEmployee = new Employee
                {
                    Id = _editingEmployeeId.Value,
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email.Trim().ToLowerInvariant(),
                    Department = Department.Trim()
                };

                await _assetService.UpdateEmployeeAsync(updatedEmployee);
                CancelEdit();
            }
            else
            {
                var newEmployee = new Employee
                {
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email.Trim().ToLowerInvariant(),
                    Department = Department.Trim()
                };

                await _assetService.AddEmployeeAsync(newEmployee);
                CancelEdit();
            }

            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred while saving: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OffboardEmployeeAsync(Employee employee)
    {
        if (IsBusy || !employee.IsActive)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Offboard Employee",
            $"Are you sure you want to offboard {employee.FullName}? All assigned hardware will be unassigned and returned to inventory, and employment will be marked as terminated.",
            confirmText: "Offboard",
            isDestructive: true);

        if (confirmed)
        {
            IsBusy = true;
            try
            {
                await _assetService.OffboardEmployeeAsync(employee.Id);
                if (IsEditing && _editingEmployeeId == employee.Id)
                {
                    CancelEdit();
                }
                await LoadEmployeesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to offboard employee: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    public async Task RehireEmployeeAsync(Employee employee)
    {
        if (IsBusy || employee.IsActive)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Rehire Employee",
            $"Rehire {employee.FullName}? Their employment status will be set to Active with today's date, making them eligible for hardware assignments again.",
            confirmText: "Rehire",
            isDestructive: false);

        if (confirmed)
        {
            IsBusy = true;
            try
            {
                await _assetService.RehireEmployeeAsync(employee.Id);
                await LoadEmployeesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to rehire employee: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    public async Task DeleteEmployeeAsync(Employee employee)
    {
        if (IsBusy || employee is null)
        {
            return;
        }

        var hasAssets = await _assetService.HasAssignedAssetsAsync(employee.Id);
        if (hasAssets)
        {
            await _dialogService.ShowAlertAsync(
                "Action Blocked",
                $"Cannot delete {employee.FullName} because this employee still has active hardware assigned. Please offboard the employee or reassign their devices first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Employee",
            $"Permanently delete record for {employee.FullName}? Historical hardware assignments will retain this name as a snapshot, but the employee record will be destroyed.",
            confirmText: "Delete",
            isDestructive: true);

        if (confirmed)
        {
            IsBusy = true;
            try
            {
                await _assetService.DeleteEmployeeAsync(employee.Id);
                if (IsEditing && _editingEmployeeId == employee.Id)
                {
                    CancelEdit();
                }
                await LoadEmployeesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete employee: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}