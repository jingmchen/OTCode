// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using OTCode.Core.Domains.FileExplorer;
using OTCode.Core.Options.FileExplorer;
using OTCode.UI.Utils;

namespace OTCode.UI.Services;

public sealed class FileExplorerService : IFileExplorerService
{
    private readonly IFileWatcherService _watcher;
    private int _internalOpCount;
    private bool _disposed;

    public ObservableCollection<FileExplorerItem> RootItems {get;}
    public ObservableCollection<FileExplorerItem> SelectedItems {get;}
    public FileExplorerClipboard Clipboard {get;}
    public FileExplorerOptions Options {get;}

    public event EventHandler<FileExplorerItem>? ItemCreated;
    public event EventHandler<FileExplorerItem>? ItemRenamed;
    public event EventHandler<string>? ItemDeleted;
    public event EventHandler? ExplorerRefreshed;

    public FileExplorerService(IFileWatcherService watcher, FileExplorerOptions options? = null)
    {
        _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
        Options = options ?? new FileExplorerOptions();

        Sanitize(Options);
    }

    // ─── PUBLIC METHODS ────────────────────────
    void LoadDirectory(string rootPath);
    void RefreshDirectory();
    FileExplorerItem CreateFile(FileExplorerItem? parent, string name);
    FileExplorerItem CreateFolder(FileExplorerItem? parent, string name);
    void RenameItem(FileExplorerItem item, string newName);
    void DeleteItem(FileExplorerItem item);
    void DeleteMultipleItems(IEnumerable<FileExplorerItem> items);
    void ExpandItem(FileExplorerItem item);
    void CollapseItem(FileExplorerItem item);
    void ExpandAll(FileExplorerItem item);
    void CollapseAll(FileExplorerItem item);
    bool CanDrop(FileExplorerItem item, FileExplorerItem? targetFolder);
    void MoveItem(FileExplorerItem item, FileExplorerItem? targetFolder);
    void ClipboardCopyItems(IEnumerable<FileExplorerItem> items);
    void ClipboardCutItems(IEnumerable<FileExplorerItem> items);
    void ClipboardPasteItems(FileExplorerItem? targetFolder);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _watcher.Changed -= OnWatcherChanged;
        _watcher.Dispose();
    }

    // ─── PRIVATE METHODS ───────────────────────
    // Core
    private void RebuildRoot()
    {
        RootItems.Clear();
        SelectedItems.Clear();

        foreach (var item in BuildTree(_rootPath, parent: null))
            RootItems.Add(item);
        
        if (Options.AutoExpandRootOnOpen)
            foreach (var root in RootItems)
                root.IsExpanded = true;
    }

    private List<FileExplorerItem> BuildTree(string path, FileExplorerItem? parent)
    {
        var items = new 
    }

    // Watcher
    private void OnWatcherChanged(object? sender, FileSystemEventArgs e)
    {
        if (Volatile.Read(ref _internalOpCount) > 0)
            return;
        
        DispatcherHelper.PostOnUIThread(() =>
        {
            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        });
    }

    // Options
    private static FileExplorerOptions Sanitize(FileExplorerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Service ??= new();
        options.Panel ??= new();

        var service = options.Service;
        var panel = options.Panel;

        if (string.IsNullOrWhiteSpace(service.NewFileName))
            service.NewFileName = "NewFile";
        
        if (string.IsNullOrWhiteSpace(service.NewFolderName))
            service.NewFolderName = "NewFolder";

        if (panel.MinWidth < panel.MaxWidth)
            panel.MinWidth = panel.MaxWidth;
        
        if (panel.Width < panel.MinWidth || panel.Width > panel.MaxWidth)
            panel.Width = Math.Clamp(panel.Width, panel.MinWidth, panel.MaxWidth);
        
        return options;
    }
}