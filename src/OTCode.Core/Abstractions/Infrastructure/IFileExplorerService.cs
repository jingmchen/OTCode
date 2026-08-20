// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using OTCode.Core.Domains.FileExplorer;
using OTCode.Core.Options.FileExplorer;

namespace OTCode.Core.Abstractions.Infrastructure;

public interface IFileExplorerService
{
    ObservableCollection<FileExplorerItem> RootItems {get;}
    ObservableCollection<FileExplorerItem> SelectedItems {get;}
    FileExplorerClipboard Clipboard {get;}
    FileExplorerOptions Options {get;}

    event EventHandler<FileExplorerItem>? ItemCreated;
    event EventHandler<FileExplorerItem>? ItemRenamed;
    event EventHandler<string>? ItemDeleted;
    event EventHandler? ExplorerRefreshed;

    void LoadDirectory(string rootPath);
    void RefreshDirectory();

    FileExplorerItem CreateFile(FileExplorerItem? parent, string name);
    FileExplorerItem CreateFolder(FileExplorerItem? parent, string name);
    void RenameItem(FileExplorerItem item, string newName);
    Task DeleteItemAsync(FileExplorerItem item, CancellationToken ct = default);
    Task DeleteMultipleItemsAsync(IEnumerable<FileExplorerItem> items, CancellationToken ct = default);

    void ExpandItem(FileExplorerItem item);
    void CollapseItem(FileExplorerItem item);
    void ExpandAll(FileExplorerItem item);
    void CollapseAll(FileExplorerItem item);
    
    bool CanDrop(FileExplorerItem item, FileExplorerItem? targetFolder);
    Task MoveItemAsync(FileExplorerItem item, FileExplorerItem? targetFolder, CancellationToken ct = default);
    void ClipboardCopyItems(IEnumerable<FileExplorerItem> items);
    void ClipboardCutItems(IEnumerable<FileExplorerItem> items);
    Task ClipboardPasteItemsAsync(FileExplorerItem? targetFolder, CancellationToken ct = default);
}