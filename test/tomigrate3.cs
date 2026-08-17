// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MacroCanvas.Core.Abstractions.FileExplorer;
using MacroCanvas.Core.Utils;

namespace MacroCanvas.Core.Models.FileExplorer;

public sealed partial class FileExplorerItem : ObservableObject
{
    // ─── OBSERVABLE PROPERTIES - IDENTITY ──────
    [ObservableProperty] public partial string Name {get; set;} = "";
    [ObservableProperty] public partial string FullPath {get; set;} = "";
    [ObservableProperty] public partial string Extension {get; set;} = "";

    // ─── OBSERVABLE PROPERTIES - UI STATE ──────
    [ObservableProperty] public partial bool IsHoveredOver {get; set;}
    [ObservableProperty] public partial bool IsSelected {get; set;}
    [ObservableProperty] public partial bool IsBeingEdited {get; set;}
    [ObservableProperty] public partial bool IsExpanded {get; set;}
    [ObservableProperty] public partial bool IsCut {get; set;}
    [ObservableProperty] public partial bool IsBeingDraggedOver {get; set;}

    // ─── FILESYSTEM METADATA ───────────────────
    public bool IsDirectory {get; init;}
    public bool IsFile => !IsDirectory;
    public long Size {get; init;}
    public DateTime CreatedAt {get; init;}
    public DateTime LastModifiedAt {get; init;}
    public bool IsHidden {get; init;}
    public bool IsSymLink {get; init;}
    public bool IsReadOnly {get; init;}

    // ─── HIERARCHY ─────────────────────────────
    public ObservableCollection<FileExplorerItem> Children {get;} = [];
    public FileExplorerItem? Parent {get; set;}

    // ─── ICONS (OPTIONAL) ──────────────────────
    public IFileIconProvider? IconProvider {get; init;}
    public string Icon => IconProvider?.GetIcon(Name, Extension, IsDirectory, IsExpanded) ?? "";

    // ─── UI ────────────────────────────────────
    public string FormattedSize => !IsDirectory
        ? FormatterHelper.FormatBytesToString(Size)
        : "";
    
    public string Tooltip => IsDirectory
        ? $"{FullPath}\nModified: {LastModifiedAt:yyyy-MM-dd HH:mm}"
        : $"{FullPath}\nSize: {FormattedSize}\nModified: {LastModifiedAt:yyyy-MM-dd HH:mm}";
    
    // ─── PROPERTY CHANGES ──────────────────────
    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Icon));
    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(Icon));
    partial void OnExtensionChanged(string value) => OnPropertyChanged(nameof(Icon));
    partial void OnFullPathChanged(string value) => OnPropertyChanged(nameof(Tooltip));
}