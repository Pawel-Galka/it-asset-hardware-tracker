namespace HardwareTracker.Models;

/// <summary>
/// Defines the operational lifecycle states of a hardware asset within the organization.
/// </summary>
public enum AssetStatus
{
    /// <summary>
    /// The asset is in inventory, fully functional, and ready for deployment.
    /// </summary>
    Available = 0,

    /// <summary>
    /// The asset is currently issued to an employee.
    /// </summary>
    Assigned = 1,

    /// <summary>
    /// The asset is undergoing diagnostics, repair, or vendor maintenance.
    /// </summary>
    InService = 2,

    /// <summary>
    /// The asset has reached end-of-life and is decommissioned, recycled, or scrapped.
    /// </summary>
    Disposed = 3
}