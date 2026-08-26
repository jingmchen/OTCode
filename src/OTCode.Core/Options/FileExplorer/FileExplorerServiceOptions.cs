// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Domains.FileExplorer;

namespace OTCode.Core.Options.FileExplorer;

public sealed class FileExplorerServiceOptions
{
    public FileExplorerFilter FileExtensionFilter {get; set;} = new()
    {
        Entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {".gitignore", ".editorconfig"}
    };

    public FileExplorerFilter FolderNameFilter {get; set;} = new()
    {
        Entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"build", "bin", "obj"}
    };
    
    public bool ShowHiddenFiles {get; set;}
    public string NewFileName {get; set;} = "NewFile";
    public string NewFileExt {get; set;} = ".txt";
    public string NewFolderName {get; set;} = "NewFolder";
    public string? RootPath {get; set;}
    public bool AutoExpandRootOnOpen {get; set;} = true;
    public bool CreateRootIfMissing {get; set;} = true;
    public bool EnableFileWatcher {get; set;} = true;
}