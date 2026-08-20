// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Domains.INPC;

namespace OTCode.Core.Domains.FileExplorer;

public sealed class FileExplorerItem : NotifyPropertyChangedBase
{
    // Identity
    public required string Name
    {
        get;
        set => SetField(ref field, value);
    }

    public required string FullPath
    {
        get;
        set
        {
            if (SetField(ref field, value))
                OnPropertyChanged(nameof(Tooltip));
        }
    }

    public string Extension
    {
        get;
        set => SetField(ref field, value);
    } = "";

    // UI state
    public bool IsHovered
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsSelected
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsBeingEdited
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsExpanded
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsCut
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsDragOver
    {
        get;
        set => SetField(ref field, value);
    }

    // FIle system metadata
    public bool IsDirectory {get; init;}
    public bool IsFile => !IsDirectory;
    public long Size {get; init;}
    public DateTime CreatedAt {get; init;}
    public DateTime LastModifiedAt {get; init;}
    public bool IsHidden {get; init;}
    public bool IsSymLink {get; init;}
    public bool IsReadOnly {get; init;}

    // Hierarchy
    public ObservableCollection<FileExplorerItem> Children {get; set;} = [];
    public FileExplorerItem? Parent {get; set;}

    // Convenience
    public string FormattedSize => IsDirectory
        ? ""
        : Format(Size);

    public string Tooltip => IsDirectory
        ? $"{FullPath}\nModified: {LastModifiedAt:yyyy-MM-dd HH:mm}"
        : $"{FullPath}\nSize: {FormattedSize}\nModified: {LastModifiedAt:yyyy-MM-dd HH:mm}";
    
    // Helpers
    private static string Format(long bytes) => bytes switch
    {
        < 1_024L => FormattableString.Invariant($"{bytes} B"),
        < 1_048_576L => FormattableString.Invariant($"{bytes / (double)1_024L:F1} KB"),
        < 1_073_741_824L => FormattableString.Invariant($"{bytes / (double)1_048_576L:F1} MB"),
        _ => FormattableString.Invariant($"{bytes / (double)1_073_741_824L:F2} GB")
    };
}