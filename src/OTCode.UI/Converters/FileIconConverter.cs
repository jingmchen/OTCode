// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Windows.Data;

namespace OTCode.UI.Converters;

public sealed class FileIconConverter : IValueConverter
{
    private const int Name = 0;
    private const int Extension = 1;
    private const int IsDirectory = 2;
    private const int IsExpanded = 3;
    
    private FileIconConverter() {}
    public static readonly FileIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}