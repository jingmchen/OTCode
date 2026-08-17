// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using Avalonia.Threading;
using MacroCanvas.Core.Abstractions.FileExplorer;
using MacroCanvas.Core.Enums;
using MacroCanvas.Core.Factories;
using MacroCanvas.Core.Models.FileExplorer;
using MacroCanvas.Core.Options;

namespace MacroCanvas.UI.Services;

public sealed class FileExplorerService : IFileExplorerService
{
    public ObservableCollection<FileExplorerItem> RootItems {get;} = [];
    public ObservableCollection<FileExplorerItem> SelectedItems {get;} = [];
    public FileExplorerClipboard Clipboard {get;} = new();
    public FileExplorerServiceOptions Options {get;}
    private IFileWatcherService _watcher;
    private string _rootPath = "";
    private int _internalOpCount; // Reference flag instead of bool due to atomic increment to prevent thread racing conflicts

    public event EventHandler<FileExplorerItem>? ItemCreated;
    public event EventHandler<FileExplorerItem>? ItemRenamed;
    public event EventHandler<string>? ItemDeleted;
    public event EventHandler? ExplorerRefreshed;

    public FileExplorerService(IFileWatcherService watcher, FileExplorerServiceOptions? options = null)
    {
        _watcher = watcher;
        Options = (options ?? new FileExplorerServiceOptions()).Validated();

        if (Options.EnableFileWatcher)
            _watcher.Changed += OnWatcherChanged;
    }
    
    // ─── PUBLIC METHODS ────────────────────────
    public void LoadDirectory()
    {
        if (string.IsNullOrWhiteSpace(Options.RootPath))
            throw new InvalidOperationException($"{nameof(Options.RootPath)} is not set; call {nameof(LoadDirectory)}(string) instead.");
        LoadDirectory(Options.RootPath);
    }

    public void LoadDirectory(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _rootPath = Path.TrimEndingDirectorySeparator(rootPath);

        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        
        RebuildRoot();
        
        if (Options.EnableFileWatcher)
            _watcher.StartWatching(rootPath);
        
        ExplorerRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshDirectory()
    {
        if (string.IsNullOrWhiteSpace(_rootPath)) return;
        LoadDirectory(_rootPath);
    }

    public FileExplorerItem CreateFile(FileExplorerItem? parent, string name)
        => CreateFileExplorerItem(parent, name, isFile: true);
    
    public FileExplorerItem CreateFolder(FileExplorerItem? parent, string name)
        => CreateFileExplorerItem(parent, name, isFile: false);

    public void RenameItem(FileExplorerItem item, string newName)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;
        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return;
        
        var parentPath = Path.GetDirectoryName(item.FullPath)!;
        var newFullPath = Path.Combine(parentPath, newName);

        if (PathExists(newFullPath)
            && !string.Equals(newFullPath, item.FullPath, StringComparison.OrdinalIgnoreCase))
            return;
        
        SuppressWatcher();

        try
        {
            RelocateFileExplorerItem(item, newFullPath);
            item.IsBeingEdited = false;

            ItemRenamed?.Invoke(this, item);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void DeleteItem(FileExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        SuppressWatcher();

        try
        {
            var path = item.FullPath;

            if (item.IsDirectory && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
            else if (item.IsFile && File.Exists(path))
                File.Delete(path);
            
            ChildrenOf(item.Parent).Remove(item);
            SelectedItems.Remove(item);

            ItemDeleted?.Invoke(this, path);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void DeleteMultipleItems(IEnumerable<FileExplorerItem> items)
    {
        foreach (var item in items.ToList())
            DeleteItem(item);
    }

    public void ExpandItem(FileExplorerItem item)
    {
        if (item.IsDirectory)
            item.IsExpanded = true;
    }

    public void CollapseItem(FileExplorerItem item)
    {
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

        if (targetFolder is null) return true;
        if (!targetFolder.IsDirectory) return false;
        if (item == targetFolder) return false;
        
        return !IsAncestor(ancestor: item, candidate: targetFolder);
    }

    public void MoveItem(FileExplorerItem item, FileExplorerItem? targetFolder)
    {
        if (!CanDrop(item, targetFolder)) return;

        var targetPath = PathOf(targetFolder);
        var currentParent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(item.FullPath));

        if (string.Equals(currentParent, Path.TrimEndingDirectorySeparator(targetPath), StringComparison.OrdinalIgnoreCase))
            return;
        
        SuppressWatcher();

        try
        {
            var newPath = GetUniquePath(targetPath, item.Name, isFile: item.IsFile);

            RelocateFileExplorerItem(item, newPath);
            ChildrenOf(item.Parent).Remove(item);

            item.Parent = targetFolder;

            InsertSorted(ChildrenOf(targetFolder), item);

            targetFolder?.IsExpanded = true;
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
    
    public void ClipboardPasteItems(FileExplorerItem? targetFolder)
    {
        if (!Clipboard.HasItems) return;

        SuppressWatcher();

        try
        {
            var targetPath = targetFolder?.FullPath ?? _rootPath;

            foreach (var item in Clipboard.Snapshot())
            {
                if (Clipboard.Operation == ClipboardOperation.Copy)
                {
                    if (item.IsDirectory
                            && (targetPath + Path.DirectorySeparatorChar)
                            .StartsWith(item.FullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    var newPath = GetUniquePath(targetPath, item.Name, isFile: item.IsFile);

                    if (item.IsDirectory)
                        CopyDirectory(item.FullPath, newPath, overwrite: false);
                    else
                        File.Copy(item.FullPath, newPath);
                    
                    var copy = BuildSingleItem(newPath, targetFolder);
                    InsertSorted(ChildrenOf(targetFolder), copy);
                    
                    ItemCreated?.Invoke(this, copy);
                }
                else
                {
                    MoveItem(item, targetFolder);
                    item.IsCut = false;
                }
            }

            if (Clipboard.Operation == ClipboardOperation.Cut)
                Clipboard.SetNone();
            
            targetFolder?.IsExpanded = true;
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void Dispose()
    {
        _watcher.Changed -= OnWatcherChanged;
        _watcher.Dispose();
    }

    // ─── PRIVATE METHODS ───────────────────────
    // Watcher
    private void OnWatcherChanged(object? sender, FileSystemEventArgs e)
    {
        if (Volatile.Read(ref _internalOpCount) > 0) return;

        Dispatcher.UIThread.Post(() =>
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
            && Options.EnableFileWatcher
            && !string.IsNullOrWhiteSpace(_rootPath))
        {
            _watcher.StartWatching(_rootPath);
        }
    }

    // Core implementation
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

        var applyFolderFilters = !Options.FolderNameFilter.IsWhitelist || parent is null;

        foreach (var dir in dirs)
        {
            try
            {
                if (applyFolderFilters && !Options.FolderNameFilter.Passes(dir.Name))
                    continue;
                
                if (!Options.ShowHiddenFiles && dir.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;
                
                var item = FileExplorerItemFactory.FromPath(dir.FullName, parent);

                // Junctions / symlinks can point back up the tree resulting into a recursive cycle (StackOverflowException)
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
                if (!Options.ShowHiddenFiles && file.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;
                
                if (!Options.FileExtensionFilter.Passes(file.Extension))
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
            var fullPath = GetUniquePath(PathOf(parent), name, isFile);

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

    private static void InsertSorted(ObservableCollection<FileExplorerItem> collection, FileExplorerItem item)
    {
        var index = 0;

        for (; index < collection.Count; index++)
        {
            var existing = collection[index];

            if (item.IsDirectory && existing.IsFile) break;
            if (!item.IsDirectory && existing.IsDirectory) continue;
            if (item.IsDirectory == existing.IsDirectory && string.Compare(item.Name, existing.Name, StringComparison.OrdinalIgnoreCase) < 0)
                break;
        }

        collection.Insert(index, item);
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
            if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) continue; // Don't follow reparse points to prevent recursive overflow
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)), overwrite);
        }
    }

    private static void SetExpandedRecursive(FileExplorerItem item, bool setExpanded)
    {
        if (!item.IsDirectory) return;
        item.IsExpanded = setExpanded;
        foreach (var child in item.Children)
            SetExpandedRecursive(child, setExpanded);
    }

    private static void CollectExpandedPaths(IEnumerable<FileExplorerItem> items, HashSet<string> paths)
    {
        foreach (var item in items)
        {
            if (!item.IsDirectory || !item.IsExpanded) continue;
            paths.Add(item.FullPath);
            CollectExpandedPaths(item.Children, paths);
        }
    }

    private static void RestoreExpandedPaths(IEnumerable<FileExplorerItem> items, HashSet<string> paths)
    {
        foreach (var item in items)
        {
            if (!item.IsDirectory) continue;
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
            if (current == ancestor) return true;
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

    private static string GetUniquePath(string parentPath, string name, bool isFile)
    {
        var path = Path.Combine(parentPath, name);
        if (!PathExists(path)) return path;

        var stem = isFile
            ? Path.GetFileNameWithoutExtension(name)
            : name;
        
        var ext = isFile
            ? Path.GetExtension(name)
            : "";
        
        var counter = 1;

        while (true)
        {
            path = Path.Combine(parentPath, $"{stem} ({counter}){ext}");
            if (!PathExists(path)) return path;
            counter++;
        }
    }

    private ObservableCollection<FileExplorerItem> ChildrenOf(FileExplorerItem? folder)
        => folder?.Children ?? RootItems;
    
    private string PathOf(FileExplorerItem? folder)
        => folder?.FullPath ?? _rootPath;

    private static bool PathExists(string path)
        => File.Exists(path) || Directory.Exists(path);
}