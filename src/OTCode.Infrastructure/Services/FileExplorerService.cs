// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Domains.FileExplorer;
using OTCode.Core.Enums;
using OTCode.Core.Options.FileExplorer;
using OTCode.Core.Utils;
using OTCode.Infrastructure.Factories;
using OTCode.Infrastructure.Utils;

namespace OTCode.Infrastructure.Services;

public sealed class FileExplorerService : IFileExplorerService
{
    private readonly IFileWatcherService _watcher;
    private readonly IUIDispatcher _dispatcher;
    private string _rootPath = "";
    private int _internalOpCount;
    private bool _disposed;

    public ObservableCollection<FileExplorerItem> RootItems {get;} = [];
    public ObservableCollection<FileExplorerItem> SelectedItems {get;} = [];
    public FileExplorerClipboard Clipboard {get;} = new();
    public FileExplorerOptions Options {get;}

    public event EventHandler<FileExplorerItem>? ItemCreated;
    public event EventHandler<FileExplorerItem>? ItemRenamed;
    public event EventHandler<string>? ItemDeleted;
    public event EventHandler? ExplorerRefreshed;

    public FileExplorerService(
        IFileWatcherService watcher,
        IUIDispatcher dispatcher,
        FileExplorerOptions? options = null)
    {
        _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Options = options ?? new FileExplorerOptions();

        Options.SanitizeValidate();

        if (Options.Service.EnableFileWatcher)
            _watcher.Changed += OnWatcherChanged;
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void LoadDirectory(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _rootPath = Path.TrimEndingDirectorySeparator(rootPath);

        if (!Directory.Exists(_rootPath) && Options.Service.CreateRootIfMissing)
            Directory.CreateDirectory(_rootPath);

        RebuildRoot();

        if (Options.Service.EnableFileWatcher)
            _watcher.StartWatching(_rootPath);

        ExplorerRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public void LoadDirectory()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Options.Service.RootPath);
        LoadDirectory(Options.Service.RootPath);
    }

    public void RefreshDirectory()
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
            return;
        LoadDirectory(_rootPath);
    }

    public FileExplorerItem CreateFile(FileExplorerItem? parent, string name)
        => CreateFileExplorerItem(parent, name, isFile: true);
    
    public FileExplorerItem CreateFolder(FileExplorerItem? parent, string name)
        => CreateFileExplorerItem(parent, name, isFile: false);
    
    public void RenameItem(FileExplorerItem item, string newName)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.Name)
            return;
        
        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return;
        
        var parentPath = Path.GetDirectoryName(item.FullPath);

        if (parentPath is null)
            return; // From official documentation, Path.GetDirectoryName("C:\") returns null
        
        var newFullPath = Path.Combine(parentPath, newName);

        if ((File.Exists(newFullPath) || Directory.Exists(newFullPath))
            && PathHelper.SamePath(newFullPath, item.FullPath))
            return;
        
        SuppressWatcher();

        try
        {
            RelocateOnDisk(item.FullPath, newFullPath, item.IsDirectory);
            RefreshItemPaths(item, newFullPath);
            item.IsBeingEdited = false;
            ItemRenamed?.Invoke(this, item);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public async Task DeleteItemAsync(FileExplorerItem item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var path = item.FullPath;
        var isDirectory = item.IsDirectory;
        var isFile = item.IsFile;

        SuppressWatcher();

        try
        {
            await Task.Run(() => DeleteEntry(path, isDirectory, isFile), ct);            
            ChildrenOf(item.Parent).Remove(item);
            SelectedItems.Remove(item);
            ItemDeleted?.Invoke(this, path);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public async Task DeleteMultipleItemsAsync(IEnumerable<FileExplorerItem> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var targets = items
            .Select(i => (item: i, path: i.FullPath, isDirectory: i.IsDirectory, isFile: i.IsFile))
            .ToList();

        SuppressWatcher();

        try
        {
            var deleted = new List<FileExplorerItem>(targets.Count);
            List<Exception>? failures = null;

            await Task.Run(() =>
            {
                foreach (var (item, path, isDirectory, isFile) in targets)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        DeleteEntry(path, isDirectory, isFile);
                        deleted.Add(item);
                    }
                    catch (Exception ex)
                    {
                        (failures ??= []).Add(ex);
                    }
                }
            }, ct);
            
            foreach (var item in deleted)
            {
                ChildrenOf(item.Parent).Remove(item);
                SelectedItems.Remove(item);
                ItemDeleted?.Invoke(this, item.FullPath);
            }

            if (failures is { Count: > 0 })
                throw new IOException($"{failures.Count} item(s) could not be deleted.", failures[0]);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void ExpandItem(FileExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsDirectory)
            item.IsExpanded = true;
    }

    public void CollapseItem(FileExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsDirectory)
            item.IsExpanded = false;
    }

    public void ExpandAll(FileExplorerItem item)
        => SetExpandedRecursive(item, setExpanded: true);

    public void CollapseAll(FileExplorerItem item)
        => SetExpandedRecursive(item, setExpanded: false);

    public bool CanDrop(FileExplorerItem item, FileExplorerItem? targetFolder)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (targetFolder is null)
            return true;
        
        if (!targetFolder.IsDirectory)
            return false;
        
        if (item == targetFolder)
            return false;
        
        return !IsAncestor(ancestor: item, candidate: targetFolder);
    }

    public async Task MoveItemAsync(FileExplorerItem item, FileExplorerItem? targetFolder, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!CanDrop(item, targetFolder))
            return;
        
        var targetPath = PathOf(targetFolder);
        var currentParent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(item.FullPath));

        if (PathHelper.SamePath(currentParent, targetPath))
            return;
        
        var source = item.FullPath;
        var name = item.Name;
        var isDirectory = item.IsDirectory;
        var isFile = item.IsFile;
        
        SuppressWatcher();

        try
        {
            var newPath = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var dest = DirectoryHelper.MakeUniquePath(targetPath, name, isFile: isFile);
                RelocateOnDisk(source, dest, isDirectory);
                return dest;
            }, ct);

            ApplyRelocationToTree(item, newPath, targetFolder);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void ClipboardCopyItems(IEnumerable<FileExplorerItem> items)
        => Clipboard.SetCopy(items);

    public void ClipboardCutItems(IEnumerable<FileExplorerItem> items)
        => Clipboard.SetCut(items);

    public async Task ClipboardPasteItemsAsync(FileExplorerItem? targetFolder, CancellationToken ct = default)
    {
        if (!Clipboard.HasItems)
            return;

        var operation = Clipboard.Operation;
        var snapshot = Clipboard.Snapshot();
        var targetPath = targetFolder?.FullPath ?? _rootPath;

        SuppressWatcher();

        try
        {
            List<Exception>? failures = null;

            if (operation == ClipboardOperation.Copy)
            {
                var newPaths = new List<string>(snapshot.Count);

                await Task.Run(() =>
                {
                    foreach (var item in snapshot)
                    {
                        ct.ThrowIfCancellationRequested();

                        try
                        {
                            // Pasting a folder into itself or its own subtree causes recursion loop
                            if (item.IsDirectory && PathHelper.IsUnder(targetPath, item.FullPath))
                                continue;

                            var newPath = DirectoryHelper.MakeUniquePath(targetPath, item.Name, isFile: item.IsFile);

                            if (item.IsDirectory)
                                CopyDirectory(item.FullPath, newPath, overwrite: false);
                            else
                                File.Copy(item.FullPath, newPath);

                            newPaths.Add(newPath);
                        }
                        catch (Exception ex)
                        {
                            (failures ??= []).Add(ex);
                        }
                    }
                }, ct);

                foreach (var newPath in newPaths)
                {
                    var copy = BuildSingleItem(newPath, targetFolder);
                    InsertSorted(ChildrenOf(targetFolder), copy);
                    ItemCreated?.Invoke(this, copy);
                }

                targetFolder?.IsExpanded = true;
            }
            else
            {
                var plan = new List<(
                    FileExplorerItem item,
                    string source,
                    string name,
                    bool isDirectory,
                    bool isFile)>();

                foreach (var item in snapshot)
                {
                    if (!CanDrop(item, targetFolder))
                        continue;

                    var currentParent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(item.FullPath));

                    if (PathHelper.SamePath(currentParent, targetPath))
                        continue;

                    plan.Add((item, item.FullPath, item.Name, item.IsDirectory, item.IsFile));
                }

                var moved = new List<(FileExplorerItem item, string newPath)>(plan.Count);

                await Task.Run(() =>
                {
                    foreach (var (item, source, name, isDirectory, isFile) in plan)
                    {
                        ct.ThrowIfCancellationRequested();
                        
                        try
                        {
                            var dest = DirectoryHelper.MakeUniquePath(targetPath, name, isFile: isFile);
                            RelocateOnDisk(source, dest, isDirectory);
                            moved.Add((item, dest));
                        }
                        catch (Exception ex)
                        {
                            (failures ??= []).Add(ex);
                        }
                    }
                }, ct);

                foreach (var (item, newPath) in moved)
                {
                    ApplyRelocationToTree(item, newPath, targetFolder);
                    item.IsCut = false;
                }

                Clipboard.SetNone();
            }

            if (failures is { Count: > 0 })
                throw new IOException($"{failures.Count} item(s) could not be pasted.", failures[0]);
        }
        finally
        {
            ResumeWatcher();
        }
    }

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
        
        if (Options.Service.AutoExpandRootOnOpen)
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

                // Junctions and symlinks can point back up the tree resulting in endless cycle recursion
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
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        SuppressWatcher();

        try
        {
            var fullPath = DirectoryHelper.MakeUniquePath(PathOf(parent), name, isFile);

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

    private FileExplorerItem BuildSingleItem(string path, FileExplorerItem? parent)
    {
        var item = FileExplorerItemFactory.FromPath(path, parent);

        if (item.IsDirectory)
            foreach (var child in BuildTree(path, item))
                item.Children.Add(child);

        return item;
    }

    private static void CopyDirectory(string source, string destination, bool overwrite = false)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite);

        foreach (var dir in Directory.GetDirectories(source))
        {
            if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0)
                continue;
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)), overwrite);
        }
    }

    private static void RelocateOnDisk(string source, string destination, bool isDirectory)
    {
        if (!isDirectory)
        {
            File.Move(source, destination);
            return;
        }

        if (SameRoot(source, destination))
            Directory.Move(source, destination);
        else
        {
            CopyDirectory(source, destination, overwrite: false);
            Directory.Delete(source, recursive: true);
        }
    }

    private static void DeleteEntry(string path, bool isDirectory, bool isFile)
    {
        if (isDirectory && Directory.Exists(path))
            Directory.Delete(path, recursive: true);
        else if (isFile && File.Exists(path))
            File.Delete(path);
    }

    private static void RefreshItemPaths(FileExplorerItem item, string newPath)
    {
        item.FullPath = newPath;
        item.Name = Path.GetFileName(newPath);
        item.Extension = item.IsFile ? Path.GetExtension(newPath).ToLowerInvariant() : "";

        if (item.IsDirectory)
            UpdateDescendantPaths(item);
    }

    private void ApplyRelocationToTree(FileExplorerItem item, string newPath, FileExplorerItem? targetFolder)
    {
        RefreshItemPaths(item, newPath);
        ChildrenOf(item.Parent).Remove(item);
        item.Parent = targetFolder;
        InsertSorted(ChildrenOf(targetFolder), item);
        targetFolder?.IsExpanded = true;
    }

    private static void InsertSorted(ObservableCollection<FileExplorerItem> collection, FileExplorerItem item)
    {
        var index = 0;

        for (; index < collection.Count; index++)
        {
            var existing = collection[index];

            if (item.IsDirectory && existing.IsFile)
                break;
            
            if (!item.IsDirectory && existing.IsDirectory)
                continue;
            
            if (item.IsDirectory == existing.IsDirectory
                && string.Compare(item.Name, existing.Name, StringComparison.OrdinalIgnoreCase) < 0)
                break;
        }

        collection.Insert(index, item);
    }

    private static void SetExpandedRecursive(FileExplorerItem item, bool setExpanded)
    {
        ArgumentNullException.ThrowIfNull(item);
        
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
        
        _dispatcher.Post(() =>
        {
            try
            {
                var expandedPaths = new HashSet<string>(PathHelper.Comparer);
                CollectExpandedPaths(RootItems, expandedPaths);
                RebuildRoot();
                RestoreExpandedPaths(RootItems, expandedPaths);
                ExplorerRefreshed?.Invoke(this, EventArgs.Empty);
            }
            catch { /* nothing to clean up- next tick reconciles the tree */ }
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

    // Helpers
    private static bool SameRoot(string a, string b)
        => PathHelper.SamePath(
            Path.GetPathRoot(Path.GetFullPath(a)),
            Path.GetPathRoot(Path.GetFullPath(b)));
    
    private ObservableCollection<FileExplorerItem> ChildrenOf(FileExplorerItem? folder)
        => folder?.Children ?? RootItems;

    private string PathOf(FileExplorerItem? folder)
        => folder?.FullPath ?? _rootPath;
}