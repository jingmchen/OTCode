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
        set
        {
            if (SetField(ref field, value))
                OnPropertyChanged(nameof(Icon));
        }
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
        set
        {
            if (SetField(ref field, value))
                OnPropertyChanged(nameof(Icon));
        }
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
        set
        {
            if (SetField(ref field, value))
                OnPropertyChanged(nameof(Icon));
        }
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
}