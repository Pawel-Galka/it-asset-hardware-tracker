using System;

namespace HardwareTracker.Models;

/// <summary>
/// Represents an immutable audit log entry tracking asset custody, service events, and lifecycle state transitions.
/// </summary>
public class AssignmentHistory
{
    public int Id { get; set; }

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>
    /// Historical snapshot of the assignee name to preserve audit trails even if the referenced employee record is purged.
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;

    public AssetStatus Status { get; set; } = AssetStatus.Assigned;
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }

    // Lightweight UI projections are kept on the domain model to avoid ViewModel wrapper allocations 
    // when binding high-volume timeline lists inside Avalonia DataTemplates.

    public string DisplayTitle => Status switch
    {
        AssetStatus.Available => "Returned to Inventory",
        AssetStatus.InService => "Maintenance / Service",
        AssetStatus.Disposed => "Asset Decommissioned",
        _ => EmployeeName
    };

    public string DisplaySubtitle => Status switch
    {
        AssetStatus.Available => ReturnedDate.HasValue
            ? $"In inventory: {AssignedDate:yyyy-MM-dd} - {ReturnedDate:yyyy-MM-dd}"
            : $"In inventory since: {AssignedDate:yyyy-MM-dd} (Available)",
        AssetStatus.InService => ReturnedDate.HasValue
            ? $"Service period: {AssignedDate:yyyy-MM-dd} - {ReturnedDate:yyyy-MM-dd}"
            : $"Service started: {AssignedDate:yyyy-MM-dd} (Ongoing)",
        AssetStatus.Disposed => $"Decommission date: {AssignedDate:yyyy-MM-dd}",
        _ => ReturnedDate.HasValue
            ? $"Period: {AssignedDate:yyyy-MM-dd} - {ReturnedDate:yyyy-MM-dd}"
            : $"Assigned: {AssignedDate:yyyy-MM-dd} (In use)"
    };

    public string? DisplayDepartment => Status == AssetStatus.Assigned && Employee != null
        ? $"Department: {Employee.Department}"
        : null;
}