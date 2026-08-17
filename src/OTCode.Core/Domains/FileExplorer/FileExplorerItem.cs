// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Domains.FileExplorer;

public sealed record FileExplorerItem
{
    public required string Name {get; set;}
    public required string FullPath {get; set;}
    public string Extension {get; set;} = "";

    public bool IsHoveredOver {get; set;}
    public bool IsSelected {get; set;}
    public bool IsBeingEdited {get; set;}
    public bool IsExpanded {get; set;}
    public bool IsCut {get; set;}
    public bool IsBeingDraggedOver {get; set;}

    public bool IsDirectory {get; set;}
    public bool IsFile => !IsDirectory;
    public long Size {get; set;}

    public DateTime CreatedAt {get; set;}
    public DateTime LastModifiedAt {get; set;}

    public bool IsHidden {get; set;}
    public bool IsSymLink {get; set;}
    public bool IsReadOnly {get; set;}
}