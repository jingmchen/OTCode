// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using OTCode.Core.Domains.FileExplorer;
using OTCode.Core.Options.FileExplorer;
using OTCode.UI.Utils;

namespace OTCode.Infrastructure.Services;

public sealed class FileExplorerService : IFileExplorerService
{
    private readonly IFileWatcherService _watcher;
    private string _rootPath = "";
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

        SanitizeValidate(Options);

        if (Options.Service.EnableFileWatcher)
            _watcher.Changed += OnWatcherChanged;
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void LoadDirectory(string rootPath)
    {
        //
    }

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
        var items = new List<FileExplorerItem>();

        DirectoryInfo[] dirs;
        FileInfo[] files;

        try
        {
            var root = new DirectoryInfo(path);
            dirs = [.. root.GetDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)];
            files = [.. root.GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)];
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return items;
        }
        
        var applyFolderFilters = !Options.Service.FolderNameFilter.IsWhitelist || parent is null;

        foreach (var dir in dirs)
        {
            try
            {
                if (applyFolderFilters && !Options.Service.FolderNameFilter.Passes(dir.Name))
                    continue;

                if (!Options.Service.ShowHiddenFiles && dir.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;

                var item = FileExplorerItemFactory.FromPath(dir.FullName, parent);

                // Junctions / symlinks can point back up the tree resulting in infinite loop
                if (!dir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    foreach (var c in BuildTree(dir.FullName, item))
                        item.Children.Add(c);

                items.Add(item);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                continue;
            }
        }

        foreach (var file in files)
        {
            try
            {
                if (!Options.Service.ShowHiddenFiles && file.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;

                if (!Options.Service.FileExtensionFilter.Passes(file.Extension))
                    continue;

                items.Add(FileExplorerItemFactory.FromPath(file.FullName, parent));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                continue;
            }
        }

        return items;
    }

    private FileExplorerItem CreateFileExplorerItem(FileExplorerItem? parent, string name, bool isFile)
    {
        SuppressWatcher();

        try
        {
            var fullPath = DirectoryHelper.GetUniquePath(PathOf(parent), name, isFile);

            if (isFile)
                File.WriteAllText(fullPath, "");
            else
                Directory.CreateDirectory(fullPath);
            
            var item = FileExplorerItemFactory.FromPath(fullPath, parent);
            InsertSorted(ChildrenOf(parent), item);
            ItemCreated?.Invoke(this, item);

            return item;
        }
        finally
        {
            ResumeWatcher();
        }
    }

    private static void RelocateFileExplorerItem(FileExplorerItem item, string newPath)
    {
        if (item.IsDirectory)
            Directory.Move(item.FullPath, newPath);
        else
            File.Move(item.FullPath, newPath);

        item.FullPath = newPath;
        item.Name = Path.GetFileName(newPath);

        item.Extension = item.IsFile
            ? Path.GetExtension(newPath).ToLowerInvariant()
            : "";

        if (item.IsDirectory)
            UpdateDescendantPaths(item);
    }

    private FileExplorerItem BuildSingleItem(string path, FileExplorerItem? parent)
    {
        var item = FileExplorerItemFactory.FromPath(path, parent);

        if (item.IsDirectory)
            foreach (var child in BuildTree(path, item))
                item.Children.Add(child);

        return item;
    }

    private static void SetExpandedRecursive(FileExplorerItem item, bool setExpanded)
    {
        if (!item.IsDirectory)
            return;
        item.IsExpanded = setExpanded;
        foreach (var child in item.Children)
            SetExpandedRecursive(child, setExpanded);
    }

    private static void CollectExpandedPaths(IEnumerable<FileExplorerItem> items, HashSet<string> paths)
    {
        foreach (var item in items)
        {
            if (!item.IsDirectory || !item.IsExpanded)
                continue;
            paths.Add(item.FullPath);
            CollectExpandedPaths(item.Children, paths);
        }
    }

    private static void RestoreExpandedPaths(IEnumerable<FileExplorerItem> items, HashSet<string> paths)
    {
        foreach (var item in items)
        {
            if (!item.IsDirectory)
                continue;
            if (paths.Contains(item.FullPath))
            {
                item.IsExpanded = true;
                RestoreExpandedPaths(item.Children, paths);
            }
        }
    }

    private static bool IsAncestor(FileExplorerItem ancestor, FileExplorerItem candidate)
    {
        var current = candidate.Parent;
        while (current is not null)
        {
            if (current == ancestor)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private static void UpdateDescendantPaths(FileExplorerItem item)
    {
        foreach (var child in item.Children)
        {
            child.FullPath = Path.Combine(item.FullPath, child.Name);
            UpdateDescendantPaths(child);
        }
    }

    // Watcher
    private void OnWatcherChanged(object? sender, FileSystemEventArgs e)
    {
        if (Volatile.Read(ref _internalOpCount) > 0)
            return;
        
        DispatcherHelper.PostOnUIThread(() =>
        {
            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectExpandedPaths(RootItems, expandedPaths);
            RebuildRoot();
            RestoreExpandedPaths(RootItems, expandedPaths);
            ExplorerRefreshed?.Invoke(this, EventArgs.Empty);
        });
    }

    private void SuppressWatcher()
    {
        if (Interlocked.Increment(ref _internalOpCount) == 1)
            _watcher.StopWatching();
    }

    private void ResumeWatcher()
    {
        if (Interlocked.Decrement(ref _internalOpCount) == 0
            && Options.Service.EnableFileWatcher
            && !string.IsNullOrWhiteSpace(_rootPath))
        {
            _watcher.StartWatching(_rootPath);
        }
    }

    // Options
    private static FileExplorerOptions SanitizeValidate(FileExplorerOptions options)
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
        
        if (!service.NewFileExt.StartsWith('.'))
            throw new ArgumentException(
                $"{nameof(service.NewFileExt)} must include a leading dot.");
             
        if (service.MaxSearchResults < 1)
            throw new ArgumentException(
                $"{nameof(service.MaxSearchResults)} must be at least 1.");

        if (service.NewFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            service.NewFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("New file/folder names contain invalid path characters.");

        if (service.RootPath is not null && string.IsNullOrWhiteSpace(service.RootPath))
            throw new ArgumentException($"{nameof(service.RootPath)} must be null (no auto-load) or a non-blank path.");
        
        if (panel.MinWidth > panel.MaxWidth)
            throw new ArgumentException(
                $"{nameof(panel.MinWidth)} cannot be greater than {nameof(panel.MaxWidth)}.");
        
        if (panel.Width < panel.MinWidth || panel.Width > panel.MaxWidth)
            panel.Width = Math.Clamp(panel.Width, panel.MinWidth, panel.MaxWidth);
        
        return options;
    }

    // Helpers
    private ObservableCollection<FileExplorerItem> ChildrenOf(FileExplorerItem? folder)
        => folder?.Children ?? RootItems;

    private string PathOf(FileExplorerItem? folder)
        => folder?.FullPath ?? _rootPath;
}