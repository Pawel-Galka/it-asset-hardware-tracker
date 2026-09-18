using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace HardwareTracker.Converters;

/// <summary>
/// Converts multiple numeric status counts into proportional star-based grid column definitions.
/// </summary>
public sealed class StatusRatioConverter : IMultiValueConverter
{
    public static readonly StatusRatioConverter Instance = new();

    // Minimum positive weight prevents zero-sum grid measurement collapse while preserving visual ratios.
    private const double Epsilon = 0.0001;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 4)
        {
            return new ColumnDefinitions
            {
                new(1, GridUnitType.Star),
                new(1, GridUnitType.Star),
                new(1, GridUnitType.Star),
                new(1, GridUnitType.Star)
            };
        }

        // Direct instantiation avoids string allocations and eliminates culture-specific decimal parsing bugs.
        return new ColumnDefinitions
        {
            new(GetWeight(values[0]), GridUnitType.Star),
            new(GetWeight(values[1]), GridUnitType.Star),
            new(GetWeight(values[2]), GridUnitType.Star),
            new(GetWeight(values[3]), GridUnitType.Star)
        };
    }

    private static double GetWeight(object? value)
    {
        return value is int count && count > 0 ? count : Epsilon;
    }
}