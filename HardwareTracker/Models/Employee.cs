using System;

namespace HardwareTracker.Models;

/// <summary>
/// Represents an employee record, tracking departmental placement, tenure lifecycle, and hardware custody.
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public DateTime? TerminationDate { get; set; }

    // Lightweight computed projections avoid extra ViewModel wrappers for high-density Avalonia views.
    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive => TerminationDate == null;
    public string StatusText => IsActive ? "Active" : "Terminated";
    public string HireDateDisplay => HireDate.ToString("yyyy-MM-dd");
    public string TerminationDateDisplay => TerminationDate.HasValue ? TerminationDate.Value.ToString("yyyy-MM-dd") : "—";
}