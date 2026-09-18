using System.Collections.Generic;

namespace HardwareTracker.Models;

/// <summary>
/// Represents a tracked physical hardware asset and its assignment lifecycle.
/// </summary>
public class Asset
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public int? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }

    // Read-only initialization prevents breaking EF Core change tracker navigation references.
    public ICollection<AssignmentHistory> AssignmentHistories { get; } = new List<AssignmentHistory>();
}