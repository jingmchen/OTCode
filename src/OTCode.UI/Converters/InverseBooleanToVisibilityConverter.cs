// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OTCode.UI.Converters;

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    private InverseBooleanToVisibilityConverter() {}
    public static readonly InverseBooleanToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}