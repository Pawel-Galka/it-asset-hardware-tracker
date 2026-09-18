using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HardwareTracker.Models;

namespace HardwareTracker.Services;

/// <summary>
/// Exports domain records into standard CSV format with Excel compatibility and formula injection safeguards.
/// </summary>
public sealed class ExportService : IExportService
{
    // UTF-8 with BOM is required for Microsoft Excel on desktop platforms to correctly parse non-ASCII characters.
    private static readonly Encoding CsvEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public async Task ExportAssetsToCsvAsync(IEnumerable<Asset> assets, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(destination);

        await using var writer = new StreamWriter(destination, CsvEncoding, leaveOpen: true);

        await writer.WriteLineAsync("Id,SerialNumber,Model,Category,Status,AssignedEmployee");

        foreach (var asset in assets)
        {
            // Direct sequential writes eliminate intermediate string array allocations from string.Join.
            await writer.WriteAsync(asset.Id.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(EscapeCsv(asset.SerialNumber));
            await writer.WriteAsync(',');
            await writer.WriteAsync(EscapeCsv(asset.Model));
            await writer.WriteAsync(',');
            await writer.WriteAsync(EscapeCsv(asset.Category));
            await writer.WriteAsync(',');
            await writer.WriteAsync(asset.Status.ToString());
            await writer.WriteAsync(',');
            await writer.WriteLineAsync(EscapeCsv(asset.AssignedEmployee?.FullName ?? string.Empty));
        }

        await writer.FlushAsync();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sanitized = value;

        // Neutralize spreadsheet formula execution triggers (=, +, -, @) to prevent CSV injection vulnerabilities.
        if (sanitized.StartsWith('=') || sanitized.StartsWith('+') || sanitized.StartsWith('-') || sanitized.StartsWith('@'))
        {
            sanitized = "'" + sanitized;
        }

        if (sanitized.Contains(',') || sanitized.Contains('"') || sanitized.Contains('\n') || sanitized.Contains('\r'))
        {
            return $"\"{sanitized.Replace("\"", "\"\"")}\"";
        }

        return sanitized;
    }
}