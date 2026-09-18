using System.Collections.Generic;
using System.Threading.Tasks;
using HardwareTracker.Models;

namespace HardwareTracker.Services;

/// <summary>
/// Defines the data access and business workflow contracts for asset tracking and employee custody management.
/// </summary>
public interface IAssetService
{
    /// <summary>
    /// Retrieves all assets including current assignee, ordered assignment timeline, and audit snapshots.
    /// </summary>
    Task<List<Asset>> GetAllAssetsAsync();

    /// <summary>
    /// Retrieves all registered employees sorted alphabetically by last name.
    /// </summary>
    Task<List<Employee>> GetAllEmployeesAsync();

    /// <summary>
    /// Retrieves active employees eligible for hardware assignments, optionally retaining a specified inactive employee.
    /// </summary>
    Task<List<Employee>> GetActiveEmployeesAsync(int? includeEmployeeId = null);

    /// <summary>
    /// Verifies if a serial number is unique across all assets, excluding the given asset ID during updates.
    /// </summary>
    Task<bool> IsSerialNumberUniqueAsync(string serialNumber, int? currentAssetId = null);

    /// <summary>
    /// Verifies if an email address is unique across all employees, excluding the given employee ID during updates.
    /// </summary>
    Task<bool> IsEmailUniqueAsync(string email, int? currentEmployeeId = null);

    /// <summary>
    /// Determines whether the specified employee currently holds custody of any active hardware assets.
    /// </summary>
    Task<bool> HasAssignedAssetsAsync(int employeeId);

    /// <summary>
    /// Persists a new or existing asset, synchronizing lifecycle transitions and creating immutable audit history entries.
    /// </summary>
    Task SaveAssetAsync(Asset asset, int? newEmployeeId);

    /// <summary>
    /// Removes an unassigned asset from persistence.
    /// </summary>
    Task DeleteAssetAsync(int assetId);

    /// <summary>
    /// Registers a new employee in the organization.
    /// </summary>
    Task AddEmployeeAsync(Employee employee);

    /// <summary>
    /// Updates employee identity and departmental placement details.
    /// </summary>
    Task UpdateEmployeeAsync(Employee employee);

    /// <summary>
    /// Terminates an employee's tenure and automatically returns all custodial assets back to inventory.
    /// </summary>
    Task OffboardEmployeeAsync(int employeeId);

    /// <summary>
    /// Reinstates a terminated employee record with a renewed hire timestamp.
    /// </summary>
    Task RehireEmployeeAsync(int employeeId);

    /// <summary>
    /// Removes an unassigned employee record from persistence.
    /// </summary>
    Task DeleteEmployeeAsync(int employeeId);
}