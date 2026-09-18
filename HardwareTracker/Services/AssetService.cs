using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HardwareTracker.Data;
using HardwareTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HardwareTracker.Services;

/// <summary>
/// Orchestrates asset tracking operations, employee custody lifecycles, and timeline audit logging.
/// </summary>
public sealed class AssetService(IDbContextFactory<AppDbContext> contextFactory) : IAssetService
{
    public async Task<List<Asset>> GetAllAssetsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Assets
            .Include(a => a.AssignedEmployee)
            .Include(a => a.AssignmentHistories.OrderByDescending(h => h.AssignedDate).ThenByDescending(h => h.Id))
                .ThenInclude(h => h.Employee)
            .OrderBy(a => a.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Employees
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Employee>> GetActiveEmployeesAsync(int? includeEmployeeId = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Employees
            .Where(e => e.TerminationDate == null || (includeEmployeeId.HasValue && e.Id == includeEmployeeId.Value))
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> IsSerialNumberUniqueAsync(string serialNumber, int? currentAssetId = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var normalized = serialNumber.Trim().ToLowerInvariant();
        return !await context.Assets.AnyAsync(a =>
            a.SerialNumber.ToLower() == normalized &&
            (!currentAssetId.HasValue || a.Id != currentAssetId.Value));
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? currentEmployeeId = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var normalized = email.Trim().ToLowerInvariant();
        return !await context.Employees.AnyAsync(e =>
            e.Email.ToLower() == normalized &&
            (!currentEmployeeId.HasValue || e.Id != currentEmployeeId.Value));
    }

    public async Task<bool> HasAssignedAssetsAsync(int employeeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Assets.AnyAsync(a => a.AssignedEmployeeId == employeeId);
    }

    public async Task SaveAssetAsync(Asset asset, int? newEmployeeId)
    {
        if (asset.Status is AssetStatus.InService or AssetStatus.Disposed && newEmployeeId.HasValue)
        {
            throw new InvalidOperationException("Assets marked as InService or Disposed cannot be assigned to an employee.");
        }

        if (asset.Status == AssetStatus.Assigned && !newEmployeeId.HasValue)
        {
            throw new InvalidOperationException("Assets marked as Assigned must have a designated employee.");
        }

        await using var context = await contextFactory.CreateDbContextAsync();

        Employee? targetEmployee = null;
        if (newEmployeeId.HasValue)
        {
            // Verify employee existence and ensure hardware cannot be assigned to offboarded staff.
            targetEmployee = await context.Employees.FirstOrDefaultAsync(e => e.Id == newEmployeeId.Value && e.TerminationDate == null)
                ?? throw new InvalidOperationException($"Active employee with ID {newEmployeeId.Value} not found.");
        }

        var employeeName = targetEmployee?.FullName ?? string.Empty;
        var now = DateTime.UtcNow;

        if (asset.Id == 0)
        {
            asset.SerialNumber = asset.SerialNumber.Trim();
            asset.Model = asset.Model.Trim();
            asset.Category = asset.Category.Trim();
            asset.AssignedEmployeeId = newEmployeeId;

            if (newEmployeeId.HasValue)
            {
                asset.Status = AssetStatus.Assigned;
                asset.AssignmentHistories.Add(new AssignmentHistory
                {
                    Status = AssetStatus.Assigned,
                    EmployeeId = newEmployeeId.Value,
                    EmployeeName = employeeName,
                    AssignedDate = now
                });
            }
            else if (asset.Status == AssetStatus.InService)
            {
                asset.AssignmentHistories.Add(new AssignmentHistory
                {
                    Status = AssetStatus.InService,
                    EmployeeName = "Service",
                    AssignedDate = now
                });
            }
            else if (asset.Status == AssetStatus.Disposed)
            {
                asset.AssignmentHistories.Add(new AssignmentHistory
                {
                    Status = AssetStatus.Disposed,
                    EmployeeName = "Disposed",
                    AssignedDate = now,
                    ReturnedDate = now
                });
            }
            else
            {
                asset.Status = AssetStatus.Available;
                asset.AssignmentHistories.Add(new AssignmentHistory
                {
                    Status = AssetStatus.Available,
                    EmployeeName = "Inventory",
                    AssignedDate = now
                });
            }

            await context.Assets.AddAsync(asset);
        }
        else
        {
            var existingAsset = await context.Assets
                .Include(a => a.AssignmentHistories)
                .FirstOrDefaultAsync(a => a.Id == asset.Id)
                ?? throw new InvalidOperationException($"Asset with ID {asset.Id} not found.");

            existingAsset.SerialNumber = asset.SerialNumber.Trim();
            existingAsset.Model = asset.Model.Trim();
            existingAsset.Category = asset.Category.Trim();

            var oldStatus = existingAsset.Status;
            var newStatus = asset.Status;
            var oldEmployeeId = existingAsset.AssignedEmployeeId;

            var activeHistory = existingAsset.AssignmentHistories
                .FirstOrDefault(h => h.ReturnedDate == null);

            if (oldStatus == AssetStatus.Assigned && newStatus == AssetStatus.Assigned)
            {
                if (oldEmployeeId != newEmployeeId)
                {
                    if (activeHistory != null)
                    {
                        activeHistory.ReturnedDate = now;
                    }

                    existingAsset.AssignmentHistories.Add(new AssignmentHistory
                    {
                        AssetId = existingAsset.Id,
                        Status = AssetStatus.Assigned,
                        EmployeeId = newEmployeeId!.Value,
                        EmployeeName = employeeName,
                        AssignedDate = now
                    });

                    existingAsset.AssignedEmployeeId = newEmployeeId;
                }
            }
            else if (oldStatus != newStatus)
            {
                if (activeHistory != null)
                {
                    activeHistory.ReturnedDate = now;
                }

                if (newStatus == AssetStatus.Assigned && newEmployeeId.HasValue)
                {
                    existingAsset.AssignmentHistories.Add(new AssignmentHistory
                    {
                        AssetId = existingAsset.Id,
                        Status = AssetStatus.Assigned,
                        EmployeeId = newEmployeeId.Value,
                        EmployeeName = employeeName,
                        AssignedDate = now
                    });
                    existingAsset.AssignedEmployeeId = newEmployeeId;
                    existingAsset.Status = AssetStatus.Assigned;
                }
                else if (newStatus == AssetStatus.InService)
                {
                    existingAsset.AssignmentHistories.Add(new AssignmentHistory
                    {
                        AssetId = existingAsset.Id,
                        Status = AssetStatus.InService,
                        EmployeeName = "Service",
                        AssignedDate = now
                    });
                    existingAsset.AssignedEmployeeId = null;
                    existingAsset.Status = AssetStatus.InService;
                }
                else if (newStatus == AssetStatus.Disposed)
                {
                    existingAsset.AssignmentHistories.Add(new AssignmentHistory
                    {
                        AssetId = existingAsset.Id,
                        Status = AssetStatus.Disposed,
                        EmployeeName = "Disposed",
                        AssignedDate = now,
                        ReturnedDate = now
                    });
                    existingAsset.AssignedEmployeeId = null;
                    existingAsset.Status = AssetStatus.Disposed;
                }
                else if (newStatus == AssetStatus.Available)
                {
                    existingAsset.AssignmentHistories.Add(new AssignmentHistory
                    {
                        AssetId = existingAsset.Id,
                        Status = AssetStatus.Available,
                        EmployeeName = "Inventory",
                        AssignedDate = now
                    });
                    existingAsset.AssignedEmployeeId = null;
                    existingAsset.Status = AssetStatus.Available;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAssetAsync(int assetId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var asset = await context.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null)
        {
            return;
        }

        // Prevent deletion of equipment actively in possession of personnel.
        if (asset.Status == AssetStatus.Assigned || asset.AssignedEmployeeId.HasValue)
        {
            throw new InvalidOperationException("Cannot delete an asset that is actively assigned to an employee. Unassign or decommission it first.");
        }

        context.Assets.Remove(asset);
        await context.SaveChangesAsync();
    }

    public async Task AddEmployeeAsync(Employee employee)
    {
        employee.FirstName = employee.FirstName.Trim();
        employee.LastName = employee.LastName.Trim();
        employee.Email = employee.Email.Trim().ToLowerInvariant();
        employee.Department = employee.Department.Trim();

        await using var context = await contextFactory.CreateDbContextAsync();
        await context.Employees.AddAsync(employee);
        await context.SaveChangesAsync();
    }

    public async Task UpdateEmployeeAsync(Employee employee)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var existing = await context.Employees.FirstOrDefaultAsync(e => e.Id == employee.Id)
            ?? throw new InvalidOperationException($"Employee with ID {employee.Id} not found.");

        existing.FirstName = employee.FirstName.Trim();
        existing.LastName = employee.LastName.Trim();
        existing.Email = employee.Email.Trim().ToLowerInvariant();
        existing.Department = employee.Department.Trim();

        await context.SaveChangesAsync();
    }

    public async Task OffboardEmployeeAsync(int employeeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        // Idempotency check prevents overwriting original termination date or generating duplicate return events.
        if (employee.TerminationDate.HasValue)
        {
            return;
        }

        var now = DateTime.UtcNow;
        employee.TerminationDate = now;

        var assignedAssets = await context.Assets
            .Include(a => a.AssignmentHistories)
            .Where(a => a.AssignedEmployeeId == employeeId)
            .ToListAsync();

        foreach (var asset in assignedAssets)
        {
            var activeHistory = asset.AssignmentHistories
                .FirstOrDefault(h => h.ReturnedDate == null);

            if (activeHistory is not null)
            {
                activeHistory.ReturnedDate = now;
            }

            asset.AssignmentHistories.Add(new AssignmentHistory
            {
                AssetId = asset.Id,
                Status = AssetStatus.Available,
                EmployeeName = "Inventory",
                AssignedDate = now
            });

            asset.AssignedEmployeeId = null;
            asset.Status = AssetStatus.Available;
        }

        await context.SaveChangesAsync();
    }

    public async Task RehireEmployeeAsync(int employeeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        if (!employee.TerminationDate.HasValue)
        {
            return;
        }

        employee.TerminationDate = null;
        employee.HireDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteEmployeeAsync(int employeeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var hasAssignedAssets = await context.Assets.AnyAsync(a => a.AssignedEmployeeId == employeeId);
        if (hasAssignedAssets)
        {
            throw new InvalidOperationException("Cannot delete an employee with actively assigned hardware. Reassign or offboard them first.");
        }

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee is not null)
        {
            context.Employees.Remove(employee);
            await context.SaveChangesAsync();
        }
    }
}