using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using HardwareTracker.Models;

namespace HardwareTracker.Converters;

/// <summary>
/// Maps domain asset lifecycle states to corresponding indicator brushes.
/// </summary>
public sealed class StatusToBrushConverter : IValueConverter
{
    public static readonly StatusToBrushConverter Instance = new();

    // Cache immutable instances to avoid repeated hex parsing and allocation overhead during view updates.
    private static readonly IImmutableSolidColorBrush AvailableBrush = new ImmutableSolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IImmutableSolidColorBrush AssignedBrush = new ImmutableSolidColorBrush(Color.Parse("#1565C0"));
    private static readonly IImmutableSolidColorBrush InServiceBrush = new ImmutableSolidColorBrush(Color.Parse("#EF6C00"));
    private static readonly IImmutableSolidColorBrush DisposedBrush = new ImmutableSolidColorBrush(Color.Parse("#757575"));
    private static readonly IImmutableSolidColorBrush FallbackBrush = new ImmutableSolidColorBrush(Colors.Gray);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AssetStatus status)
        {
            return FallbackBrush;
        }

        return status switch
        {
            AssetStatus.Available => AvailableBrush,
            AssetStatus.Assigned => AssignedBrush,
            AssetStatus.InService => InServiceBrush,
            AssetStatus.Disposed => DisposedBrush,
            _ => FallbackBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(StatusToBrushConverter)} supports one-way binding only.");
    }
}