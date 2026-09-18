using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HardwareTracker.Models;

namespace HardwareTracker.Services;

/// <summary>
/// Provides document export capabilities for reporting and data portability.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Serializes an asset collection into an RFC 4180 compliant CSV stream.
    /// </summary>
    Task ExportAssetsToCsvAsync(IEnumerable<Asset> assets, Stream destination);
}