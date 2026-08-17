// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OTCode.UI.Converters;

public sealed class BoolToOpacityConverter : IValueConverter
{
    private BoolToOpacityConverter() {}
    public static readonly BoolToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b
            ? (b ? 0.4d : 1.0d)
            : DependencyProperty.UnsetValue;
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}