// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows.Data;

namespace OTCode.UI.Converters;

public sealed class FileIconConverter : IValueConverter
{
    public double TrueOpacity {get;} = 0.4;
    public double FalseOpacity {get;} = 1.0;

    private FileIconConverter() {}
    public static readonly FileIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b
            ? (b ? TrueOpacity : FalseOpacity)
            : DependencyProperty.UnsetValue;
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}