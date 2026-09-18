using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace HardwareTracker.Converters;

/// <summary>
/// Converts a boolean status into a visual brush representation for UI elements.
/// </summary>
public sealed class ActiveStatusToBrushConverter : IValueConverter
{
    public static readonly ActiveStatusToBrushConverter Instance = new();

    // Cache immutable brushes to prevent heap allocations and color parsing during UI virtualization.
    private static readonly IImmutableSolidColorBrush ActiveBrush = new ImmutableSolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IImmutableSolidColorBrush InactiveBrush = new ImmutableSolidColorBrush(Color.Parse("#757575"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? ActiveBrush : InactiveBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(ActiveStatusToBrushConverter)} supports one-way binding only.");
    }
}