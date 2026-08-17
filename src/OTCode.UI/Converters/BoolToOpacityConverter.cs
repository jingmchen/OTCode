// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.UI.Converters;

public sealed class BoolToOpacityConverter : IValueConverter
{
    private BoolToOpacityConverter() {}
    public static readonly BoolToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b
            ? (b ? 0.4 : 1.0)
            : AvaloniaProperty.UnsetValue;
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}