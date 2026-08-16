// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Win32;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Domains.FilePicker;
using OTCode.UI.Common;

namespace OTCode.UI.Services;

public sealed class FilePickerService : IFilePickerService
{
    public Task<string?> PickFileAsync(string title, IReadOnlyList<FileFilter> filters)
    {
        if (UIWindow.Active is not { } owner)
            throw new InvalidOperationException("File Picker invoked with no active application window.");
        
        var dialog = new OpenFileDialog
        {
            Title = title,
            Multiselect = false,
            Filter = BuildFilter(filters)
        };

        string? result = dialog.ShowDialog(owner) == true
            ? dialog.FileName
            : null;
        
        return Task.FromResult(result);
    }

    public Task<string?> PickFolderAsync(string title)
    {
        if (UIWindow.Active is not { } owner)
            throw new InvalidOperationException("File Picker invoked with no active application window.");
        
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        string? result = dialog.ShowDialog(owner) == true
            ? dialog.FolderName
            : null;
        
        return Task.FromResult(result);
    }

    private static string BuildFilter(IReadOnlyList<FileFilter>? filters)
    {
        if (filters is not {Count: > 0})
            return "All files (*.*)|*.*";
        
        return string.Join("|", filters.Select(f =>
        {
            var patterns = string.Join(";", f.Patterns);
            return $"{f.Name} ({patterns})|{patterns}";
        }));
    }
}