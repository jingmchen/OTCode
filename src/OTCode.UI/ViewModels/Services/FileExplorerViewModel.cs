// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Domains.Behaviors;
using OTCode.Core.Domains.FileExplorer;
using OTCode.UI.Constants;
using OTCode.Core.Utils;
using OTCode.Core.Logging;

namespace OTCode.UI.ViewModels.Services;

public sealed partial class FileExplorerViewModel : ObservableObject, IHoverTracker, IDisposable
{
    private readonly IFileExplorerService _service;
    private readonly IFileExplorerItemActions _itemActions;
    private readonly ILogger<FileExplorerViewModel> _logger;
    private FileExplorerItem? _pendingCreateParent;
    private FileExplorerItem? _pendingRenameItem;
    private bool _disposed;

    [ObservableProperty]
    public partial string StatusMessage {get; set;} = "No folder loaded.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreating))]
    public partial bool IsCreatingFolder {get; set;}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreating))]
    public partial bool IsCreatingFile {get; set;}

    [ObservableProperty]
    public partial string NewItemName {get; set;} = "";

    [ObservableProperty]
    public partial string RenameText {get; set;} = "";

    [ObservableProperty]
    public partial FileExplorerItem? ContextItem {get; set;}

    [ObservableProperty]
    public partial bool HasDirectory {get; set;}

    [ObservableProperty]
    public partial string CurrentRootPath {get; set;} = "";

    [ObservableProperty]
    public partial double Zoom {get; set;} = UIConstants.Control.FileExplorer.DefaultZoom;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteHoveredCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginRenameHoveredCommand))]
    public partial FileExplorerItem? HoveredItem {get; set;}

    public ObservableCollection<FileExplorerItem> RootItems => _service.RootItems;
    public ObservableCollection<FileExplorerItem> SelectedItems => _service.SelectedItems;
    public bool IsCreating => IsCreatingFolder || IsCreatingFile;

    public FileExplorerViewModel(
        ILogger<FileExplorerViewModel> logger,
        IFileExplorerService service,
        IFileExplorerItemActions itemActions)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _itemActions = itemActions ?? throw new ArgumentNullException(nameof(itemActions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _service.ItemCreated += OnItemCreated;
        _service.ItemRenamed += OnItemRenamed;
        _service.ItemDeleted += OnItemDeleted;
        _service.ExplorerRefreshed += OnExplorerRefreshed;
    }

    public void LoadDirectory()
    {
        var root = _service.Options.Service.RootPath;

        if (!string.IsNullOrWhiteSpace(root))
            LoadDirectory(root);
    }

    public void LoadDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            _service.LoadDirectory(path);
        }
        catch (Exception ex)
        {
            LogFailedToLoadDirectory(ex, path);
            SetStatus($"Could not open '{path}': {ex.Message}");
            return;
        }

        CurrentRootPath = path;
        HasDirectory = true;

        SetStatus($"Loaded: {DisplayName(path)}");
    }

    public void SelectItem(FileExplorerItem item, bool multiSelect)
    {
        if (!multiSelect)
        {
            foreach (var prev in SelectedItems.ToList())
                prev.IsSelected = false;
            SelectedItems.Clear();
        }

        if (!SelectedItems.Contains(item))
        {
            item.IsSelected = true;
            SelectedItems.Add(item);
        }

        ContextItem = item;
        DeleteHoveredCommand.NotifyCanExecuteChanged();

        SetStatus(SelectedItems.Count > 1 ? $"{SelectedItems.Count} items selected" : item.Name);
    }

    public void DeselectItem(FileExplorerItem item)
    {
        item.IsSelected = false;
        SelectedItems.Remove(item);

        if (ContextItem == item)
            ContextItem = SelectedItems.LastOrDefault();

        DeleteHoveredCommand.NotifyCanExecuteChanged();
    }

    public void ClearSelection()
    {
        foreach (var item in SelectedItems.ToList())
            item.IsSelected = false;
        
        SelectedItems.Clear();
        ContextItem = null;
        DeleteHoveredCommand.NotifyCanExecuteChanged();

        SetStatus(HasDirectory ? $"Loaded: {DisplayName(CurrentRootPath)}" : "No folder loaded.");
    }

    public void SetHovered(object? item)
        => HoveredItem = item as FileExplorerItem;

    public void ClearHovered(object? item)
    {
        if (ReferenceEquals(HoveredItem, item))
            HoveredItem = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;

        _service.ItemCreated -= OnItemCreated;
        _service.ItemRenamed -= OnItemRenamed;
        _service.ItemDeleted -= OnItemDeleted;
        _service.ExplorerRefreshed -= OnExplorerRefreshed;
    }

    [RelayCommand]
    private void Open(FileExplorerItem? item)
    {
        item ??= HoveredItem ?? ContextItem;

        if (item is null)
            return;

        if (item.IsDirectory)
        {
            item.IsExpanded = !item.IsExpanded;
            return;
        }

        try
        {
            _itemActions.Open(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open of '{Path}' failed", item.FullPath);
            SetStatus($"Could not open '{item.Name}': {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowProperties(FileExplorerItem? item)
    {
        item ??= ContextItem;

        if (item is null)
            return;

        try
        {
            _itemActions.ShowProperties(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ShowProperties of '{Path}' failed", item.FullPath);
            SetStatus($"Could not show properties for '{item.Name}': {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanDrop))]
    private async Task Drop(FileDropRequest? request)
    {
        if (request is null || FindByPath(request.Payload) is not { } source)
            return;
        
        var target = ResolveParentFolder(request.Target as FileExplorerItem);

        try
        {
            await _service.MoveItemAsync(source, target);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Move of '{Path}' failed", source.FullPath);
            SetStatus($"Move failed: {ex.Message}");
        }
    }

    private bool CanDrop(FileDropRequest? request)
    {
        if (request is null || FindByPath(request.Payload) is not { } source)
            return false;
        
        return _service.CanDrop(source, ResolveParentFolder(request.Target as FileExplorerItem));
    }

    [RelayCommand]
    private void ApplyZoom(double delta)
        => Zoom = Math.Clamp(
            Zoom + Math.Sign(delta) * UIConstants.Control.FileExplorer.ZoomStep,
            UIConstants.Control.FileExplorer.MinZoom,
            UIConstants.Control.FileExplorer.MaxZoom);

    [RelayCommand]
    private void StepSelection(double delta)
    {
        var visible = FlattenVisible().ToList();

        if (visible.Count == 0)
            return;

        var current = SelectedItems.LastOrDefault() ?? ContextItem;

        var index = current is null
            ? -1
            : visible.IndexOf(current);
        
        index = Math.Clamp(index - Math.Sign(delta), 0, visible.Count - 1);
        SelectItem(visible[index], multiSelect: false);
    }

    [RelayCommand]
    private void BeginCreateFolder(FileExplorerItem? parent)
    {
        _pendingCreateParent = ResolveParentFolder(parent);
        IsCreatingFile = false;
        IsCreatingFolder = true;
        NewItemName = _service.Options.Service.NewFolderName;
    }

    [RelayCommand]
    private void BeginCreateFile(FileExplorerItem? parent)
    {
        _pendingCreateParent = ResolveParentFolder(parent);
        IsCreatingFolder = false;
        IsCreatingFile = true;
        NewItemName = $"{_service.Options.Service.NewFileName}{_service.Options.Service.NewFileExt}";
    }

    [RelayCommand]
    private void ConfirmCreate()
    {
        if (!IsCreatingFolder && !IsCreatingFile)
            return;

        var name = NewItemName.Trim();

        if (string.IsNullOrEmpty(name))
        {
            CancelCreate();
            return;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            SetStatus("Name contains invalid characters.");
            return;
        }

        try
        {
            if (IsCreatingFolder)
                _service.CreateFolder(_pendingCreateParent, name);
            else
                _service.CreateFile(_pendingCreateParent, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create '{Name}' failed", name);
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            CancelCreate();
        }
    }

    [RelayCommand]
    private void CancelCreate()
    {
        IsCreatingFolder = false;
        IsCreatingFile = false;
        NewItemName = string.Empty;
        _pendingCreateParent = null;
    }

    [RelayCommand]
    private void BeginRename(FileExplorerItem? item)
    {
        var target = item ?? ContextItem;

        if (target is null)
            return;
        
        if (_pendingRenameItem is not null && _pendingRenameItem != target)
            _pendingRenameItem.IsBeingEdited = false;

        _pendingRenameItem = target;
        RenameText = target.Name;
        target.IsBeingEdited = true;
    }

    [RelayCommand(CanExecute = nameof(CanRenameHovered))]
    private void BeginRenameHovered()
        => BeginRename(HoveredItem);

    private bool CanRenameHovered()
        => HoveredItem is not null;

    [RelayCommand]
    private void ConfirmRename()
    {
        var item = _pendingRenameItem;

        if (item is null)
            return;

        var newName = RenameText.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            CancelRename();
            return;
        }

        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            SetStatus("Name contains invalid characters.");
            return;
        }

        var parentDir = Path.GetDirectoryName(item.FullPath);

        if (string.IsNullOrEmpty(parentDir))
        {
            SetStatus($"'{item.Name}' can't be renamed.");
            CancelRename();
            return;
        }

        var newPath = Path.Combine(parentDir, newName);

        if ((File.Exists(newPath) || Directory.Exists(newPath)) && !PathHelper.SamePath(newPath, item.FullPath))
        {
            SetStatus($"'{newName}' already exists.");
            return;
        }

        try
        {
            _service.RenameItem(item, newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rename of '{Path}' failed", item.FullPath);
            SetStatus($"Rename failed: {ex.Message}");
        }
        finally
        {
            item.IsBeingEdited = false;
            _pendingRenameItem = null;
            RenameText = string.Empty;
        }
    }

    [RelayCommand]
    private void CancelRename()
    {
        _pendingRenameItem?.IsBeingEdited = false;
        _pendingRenameItem = null;
        RenameText = string.Empty;
    }

    [RelayCommand]
    private void CancelEdits()
    {
        CancelCreate();
        CancelRename();
    }

    [RelayCommand]
    private void ConfirmOrOpen()
    {
        if (_pendingRenameItem is not null)
        {
            ConfirmRename();
            return;
        }

        if (IsCreatingFolder || IsCreatingFile)
        {
            ConfirmCreate();
            return;
        }

        Open(HoveredItem ?? ContextItem);
    }

    [RelayCommand]
    private void Copy()
    {
        if (SelectedItems.Count == 0)
            return;
        
        _service.ClipboardCopyItems(SelectedItems);

        SetStatus($"Copied {SelectedItems.Count} item(s).");
    }

    [RelayCommand]
    private void Cut()
    {
        if (SelectedItems.Count == 0)
            return;
        
        _service.ClipboardCutItems(SelectedItems);

        SetStatus($"Cut {SelectedItems.Count} item(s).");
    }

    [RelayCommand]
    private async Task Paste()
    {
        if (!_service.Clipboard.HasItems)
            return;

        try
        {
            await _service.ClipboardPasteItemsAsync(ResolveParentFolder(ContextItem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste failed");
            SetStatus($"Paste failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteItem(FileExplorerItem? item)
    {
        if (item is null)
            return;

        try
        {
            await _service.DeleteItemAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete of '{Path}' failed", item.FullPath);
            SetStatus($"Delete failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteMultiple()
    {
        if (SelectedItems.Count == 0)
            return;

        try
        {
            await _service.DeleteMultipleItemsAsync(SelectedItems.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete of selection failed");
            SetStatus($"Delete failed: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteHovered))]
    private async Task DeleteHovered()
    {
        if (HoveredItem is { } hovered)
            await DeleteItem(hovered);
        else
            await DeleteMultiple();
    }

    private bool CanDeleteHovered()
        => HoveredItem is not null || SelectedItems.Count > 0;
    
    [RelayCommand]
    private void RefreshDirectory()
    {
        try
        {
            _service.RefreshDirectory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh failed");
            SetStatus($"Refresh failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ExpandAll(FileExplorerItem? item)
    {
        if (item is not null)
        {
            _service.ExpandAll(item);
            return;
        }

        foreach (var root in RootItems)
            _service.ExpandAll(root);
    }

    [RelayCommand]
    private void CollapseAll(FileExplorerItem? item)
    {
        if (item is not null)
        {
            _service.CollapseAll(item);
            return;
        }

        foreach (var root in RootItems)
            _service.CollapseAll(root);
    }

    private void OnItemCreated(object? sender, FileExplorerItem item)
        => SetStatus($"Created '{item.Name}'");

    private void OnItemRenamed(object? sender, FileExplorerItem item)
        => SetStatus($"Renamed to '{item.Name}'");

    private void OnItemDeleted(object? sender, string path)
    {
        if (ContextItem is not null &&
            (PathHelper.SamePath(ContextItem.FullPath, path) || PathHelper.IsUnder(ContextItem.FullPath, path)))
        {
            ContextItem = SelectedItems.LastOrDefault();
        }

        SetStatus($"Deleted '{Path.GetFileName(path)}'");
    }

    private void OnExplorerRefreshed(object? sender, EventArgs e)
    {
        SetStatus($"Refreshed — {CountItems()} item(s).");
    }

    private static FileExplorerItem? ResolveParentFolder(FileExplorerItem? item)
        => item switch
        {
            null => null,
            {IsDirectory: true} => item,
            _ => item.Parent
        };

    private FileExplorerItem? FindByPath(string path)
    {
        foreach (var root in RootItems)
            if (FindByPath(root, path) is { } hit)
                return hit;
        
        return null;
    }

    private static FileExplorerItem? FindByPath(FileExplorerItem item, string path)
    {
        if (PathHelper.SamePath(item.FullPath, path))
            return item;
        
        foreach (var child in item.Children)
            if (FindByPath(child, path) is { } hit)
                return hit;
        
        return null;
    }

    private IEnumerable<FileExplorerItem> FlattenVisible()
    {
        foreach (var root in RootItems)
            foreach (var node in FlattenVisible(root))
                yield return node;
    }

    private static IEnumerable<FileExplorerItem> FlattenVisible(FileExplorerItem item)
    {
        yield return item;

        if (item.IsDirectory && item.IsExpanded)
            foreach (var child in item.Children)
                foreach (var node in FlattenVisible(child))
                    yield return node;
    }

    private static string DisplayName(string path)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        return string.IsNullOrEmpty(name) ? path : name;
    }

    private int CountItems()
    {
        var count = 0;
        foreach (var root in _service.RootItems)
            count += CountRecursive(root);
        return count;
    }

    private static int CountRecursive(FileExplorerItem item)
    {
        var count = 1;
        foreach (var child in item.Children)
            count += CountRecursive(child);
        return count;
    }

    private void SetStatus(string message)
        => StatusMessage = message;
    
    [LoggerMessage(
        EventId = LogEventIDs.UI.FileExplorerViewModel.FailedToLoadDirectory,
        Level = LogLevel.Error,
        Message = "Failed to load directory at '{Path}'.")]
    private partial void LogFailedToLoadDirectory(Exception ex, string path);
}