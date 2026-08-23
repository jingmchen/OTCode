// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Services;

namespace OTCode.UI.Converters;

public sealed class FileIconConverter : IMultiValueConverter
{
    public IResourceUriProvider Uri {get; set;} = new ResourceUriProvider();
    private readonly ConcurrentDictionary<string, ImageBrush?> _cache =
        new(StringComparer.OrdinalIgnoreCase);
    
    private static readonly Dictionary<string, string> CategoryByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // code — languages, markup, styles, scripts
            { ".cs", "code" }, { ".csproj", "code" }, { ".sln", "code" }, { ".razor", "code" },
            { ".axaml", "code" }, { ".xaml", "code" }, { ".html", "code" }, { ".htm", "code" },
            { ".css", "code" }, { ".scss", "code" }, { ".less", "code" }, { ".js", "code" },
            { ".mjs", "code" }, { ".ts", "code" }, { ".tsx", "code" }, { ".jsx", "code" },
            { ".vue", "code" }, { ".svelte", "code" }, { ".py", "code" }, { ".rb", "code" },
            { ".go", "code" }, { ".rs", "code" }, { ".cpp", "code" }, { ".c", "code" },
            { ".h", "code" }, { ".java", "code" }, { ".kt", "code" }, { ".swift", "code" },
            { ".dart", "code" }, { ".php", "code" }, { ".sql", "code" }, { ".r", "code" },
            { ".lua", "code" }, { ".sh", "code" }, { ".bash", "code" }, { ".zsh", "code" },
            { ".ps1", "code" }, { ".bat", "code" }, { ".cmd", "code" },
            // image
            { ".png", "image" }, { ".jpg", "image" }, { ".jpeg", "image" }, { ".gif", "image" },
            { ".svg", "image" }, { ".ico", "image" }, { ".webp", "image" }, { ".bmp", "image" },
            // archive
            { ".zip", "archive" }, { ".tar", "archive" }, { ".gz", "archive" }, { ".rar", "archive" }, { ".7z", "archive" },
            // config — settings + dotfiles keyed by whole name
            { ".ini", "config" }, { ".env", "config" }, { ".editorconfig", "config" }, { ".toml", "config" },
            { ".gitignore", "config" }, { ".gitattributes", "config" }, { ".lock", "config" }
        };
    
    private FileIconConverter() {}
    public static readonly FileIconConverter Instance = new();

    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 4)
            return null;

        var name = values[0] as string ?? string.Empty;
        var extension = values[1] as string ?? string.Empty;
        var isDirectory = values[2] is true;
        var isExpanded = values[3] is true;

        var key = GetIconKey(name, extension, isDirectory, isExpanded);
        return Load(key) ?? Load(GetIconKey(string.Empty, string.Empty, isDirectory, isExpanded: false));
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
    
    public static string GetIconKey(string name, string extension, bool isDirectory, bool isExpanded)
    {
        if (isDirectory)
            return isExpanded ? "folder-open" : "folder";
        
        var key = !string.IsNullOrEmpty(extension) ? extension : name;
        return CategoryByExtension.TryGetValue(key, out var category) ? category : "file";
    }

    private ImageBrush? Load(string key) =>
        _cache.GetOrAdd(key, k =>
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(
                    string.Format(CultureInfo.InvariantCulture, Uri.IconTemplate, k),
                    UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                var brush = new ImageBrush(image) { Stretch = Stretch.Uniform };
                RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);
                brush.Freeze();
                return brush;
            }
            catch
            {
                return null;
            }
        });
}