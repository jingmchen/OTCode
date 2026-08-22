// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Domains.FileExplorer;

namespace OTCode.UI.ViewModels.Services;

public sealed partial class FileExplorerViewModel : ObservableObject, IHoverTracker, IDisposable
{
    // ─── DI-INJECTED ───────────────────────────
    private readonly ILogger<FileExplorerViewModel> _logger;
    private readonly IFileExplorerService _service;
    private readonly IFileExplorerItemActions _itemActions;

    // ─── PRIVATE STATE ─────────────────────────
    private FileExplorerItem? _pendingCreateParent;
    private FileExplorerItem? _pendingRenameItem;

    // ─── OBSERVABLE PROPERTIES ─────────────────
    [ObservableProperty] private string _statusMessage = "No folder loaded.";

    // Both raise IsCreating so the inline create-bar can bind its Visibility to a single
    // property (WPF has no first-class "OR" binding without a MultiBinding converter).
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreating))]
    private bool _isCreatingFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreating))]
    private bool _isCreatingFile;

    [ObservableProperty] private string _newItemName = string.Empty;
    [ObservableProperty] private string _renameText = string.Empty;
    [ObservableProperty] private FileExplorerItem? _contextItem; // most recently right-clicked / focused
    [ObservableProperty] private bool _hasDirectory;
    [ObservableProperty] private string _currentRootPath = string.Empty;

    // Row font size, driven by Ctrl+wheel (VSCode-style zoom).
    [ObservableProperty] private double _zoom = AppConstants.Control.FileExplorer.DefaultZoom;

    // Maintained by HoverTrackerBehavior via IHoverTracker; this is what makes the
    // hover-scoped shortcuts (F2 dead unless hovering, Delete-prefers-hover) work.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteHoveredCommand))]
    [NotifyCanExecuteChangedFor(nameof(BeginRenameHoveredCommand))]
    private FileExplorerItem? _hoveredItem;

    /// <summary>True while either inline create bar (file or folder) is open.</summary>
    public bool IsCreating => IsCreatingFolder || IsCreatingFile;

    // ─── COLLECTIONS FROM SERVICE ──────────────
    public ObservableCollection<FileExplorerItem> RootItems => _service.RootItems;
    public ObservableCollection<FileExplorerItem> SelectedItems => _service.SelectedItems;

    // ─── CONSTRUCTOR ───────────────────────────
    public FileExplorerViewModel(
        ILogger<FileExplorerViewModel> logger,
        IFileExplorerService service,
        IFileExplorerItemActions itemActions)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(itemActions);
        _logger = logger;
        _service = service;
        _itemActions = itemActions;

        // Named handlers (not lambdas) so Dispose can unsubscribe.
        _service.ItemCreated += OnItemCreated;
        _service.ItemRenamed += OnItemRenamed;
        _service.ItemDeleted += OnItemDeleted;
        _service.ExplorerRefreshed += OnExplorerRefreshed;
    }

    public void Dispose()
    {
        _service.ItemCreated -= OnItemCreated;
        _service.ItemRenamed -= OnItemRenamed;
        _service.ItemDeleted -= OnItemDeleted;
        _service.ExplorerRefreshed -= OnExplorerRefreshed;
    }

    // ─── LOADING ───────────────────────────────
    /// <summary>Auto-load from Options.RootPath, when configured.</summary>
    public void LoadDirectory()
    {
        var root = _service.Options.RootPath;
        if (!string.IsNullOrWhiteSpace(root))
            LoadDirectory(root);
    }

    public void LoadDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            _service.LoadDirectory(path);
        }
        catch (Exception ex)
        {
            // Typed paths come straight from the user: removed drives, denied
            // folders, invalid characters — status, don't crash.
            _logger.LogError(ex, "Failed to load directory '{Path}'", path);
            SetStatus($"Could not open '{path}': {ex.Message}");
            return;
        }

        // State only advances on success — a failed load must not leave
        // CurrentRootPath pointing at a folder that never opened.
        CurrentRootPath = path;
        HasDirectory = true;
        SetStatus($"Loaded: {DisplayName(path)}");
    }

    // ─── SELECTION (called from the control's SelectionChanged / empty-space click) ───
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

    // ─── IHoverTracker ─────────────────────────
    public void SetHovered(object? item) => HoveredItem = item as FileExplorerItem;

    public void ClearHovered(object? item)
    {
        // Only clear if the exiting row is still current (enter/exit interleave).
        if (ReferenceEquals(HoveredItem, item))
            HoveredItem = null;
    }

    // ─── ACTIVATION (double-click / Enter / context "Open") ───
    [RelayCommand]
    private void Open(FileExplorerItem? item)
    {
        item ??= HoveredItem ?? ContextItem;
        if (item is null) return;

        if (item.IsDirectory)
        {
            item.IsExpanded = !item.IsExpanded; // folders toggle
            return;
        }

        // The "open" action is host-supplied (shell launch, editor, …) and can throw —
        // Process.Start alone throws Win32Exception when a file has no associated handler.
        // This runs straight off a double-click / Enter / menu handler, so an escape would
        // crash the app: contain it and report.
        try
        {
            _itemActions.Open(item); // files funnel through the actions service
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
        if (item is null) return;

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

    // ─── DRAG & DROP (validated command; behaviors own the mechanics) ───
    [RelayCommand(CanExecute = nameof(CanDrop))]
    private async Task Drop(FileDropRequest? request)
    {
        if (request is null || FindByPath(request.Payload) is not { } source) return;
        var target = ResolveParentFolder(request.Target as FileExplorerItem);

        try
        {
            await _service.MoveItemAsync(source, target);
        }
        catch (Exception ex)
        {
            // A move can block or fail from the FS layer (cross-volume, network, locks); the
            // filesystem work now runs off the UI thread, and this catch keeps a failure from
            // surfacing as an unhandled exception.
            _logger.LogError(ex, "Move of '{Path}' failed", source.FullPath);
            SetStatus($"Move failed: {ex.Message}");
        }
    }

    private bool CanDrop(FileDropRequest? request)
    {
        if (request is null || FindByPath(request.Payload) is not { } source) return false;
        return _service.CanDrop(source, ResolveParentFolder(request.Target as FileExplorerItem));
    }

    // ─── WHEEL (Ctrl = zoom, Shift = step selection) ───
    [RelayCommand]
    private void ApplyZoom(double delta)
        => Zoom = Math.Clamp(
            Zoom + Math.Sign(delta) * AppConstants.Control.FileExplorer.ZoomStep,
            AppConstants.Control.FileExplorer.MinZoom,
            AppConstants.Control.FileExplorer.MaxZoom);

    [RelayCommand]
    private void StepSelection(double delta)
    {
        var visible = FlattenVisible().ToList();
        if (visible.Count == 0) return;

        var current = SelectedItems.LastOrDefault() ?? ContextItem;
        var index = current is null ? -1 : visible.IndexOf(current);
        index = Math.Clamp(index - Math.Sign(delta), 0, visible.Count - 1);
        SelectItem(visible[index], multiSelect: false);
    }

    // ─── CREATE ────────────────────────────────
    [RelayCommand]
    private void BeginCreateFolder(FileExplorerItem? parent)
    {
        _pendingCreateParent = ResolveParentFolder(parent);
        IsCreatingFile = false;
        IsCreatingFolder = true;
        NewItemName = _service.Options.NewFolderName;
    }

    [RelayCommand]
    private void BeginCreateFile(FileExplorerItem? parent)
    {
        _pendingCreateParent = ResolveParentFolder(parent);
        IsCreatingFolder = false;
        IsCreatingFile = true;
        NewItemName = $"{_service.Options.NewFileName}{_service.Options.NewFileExt}";
    }

    [RelayCommand]
    private void ConfirmCreate()
    {
        // Enter routes here globally; ignore when no create bar is open.
        if (!IsCreatingFolder && !IsCreatingFile) return;

        var name = NewItemName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            CancelCreate();
            return;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            SetStatus("Name contains invalid characters.");
            return; // keep the bar open so the user can fix it
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

    // ─── RENAME ────────────────────────────────
    [RelayCommand]
    private void BeginRename(FileExplorerItem? item)
    {
        var target = item ?? ContextItem;
        if (target is null) return;

        // Only one edit box at a time — starting a second rename must close the first.
        if (_pendingRenameItem is not null && _pendingRenameItem != target)
            _pendingRenameItem.IsBeingEdited = false;

        _pendingRenameItem = target;
        RenameText = target.Name;
        target.IsBeingEdited = true;
    }

    // Hover-scoped F2: dead unless something is hovered (the kit's "F2 renames hovered").
    [RelayCommand(CanExecute = nameof(CanRenameHovered))]
    private void BeginRenameHovered() => BeginRename(HoveredItem);

    private bool CanRenameHovered() => HoveredItem is not null;

    [RelayCommand]
    private void ConfirmRename()
    {
        var item = _pendingRenameItem;
        if (item is null) return;

        var newName = RenameText.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            CancelRename();
            return;
        }

        // The service declines these silently by design; the VM's job is to give the
        // user the *why* — and keep the box open to fix it.
        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            SetStatus("Name contains invalid characters.");
            return;
        }

        var parentDir = Path.GetDirectoryName(item.FullPath);
        if (string.IsNullOrEmpty(parentDir))
        {
            // A drive root has no parent — it can't be renamed. Close the box; there's
            // nothing for the user to correct.
            SetStatus($"'{item.Name}' can't be renamed.");
            CancelRename();
            return;
        }

        var newPath = Path.Combine(parentDir, newName);
        // PathFacts.SamePath allows a case-only rename on any platform and is correct
        // on case-sensitive filesystems (OrdinalIgnoreCase here was a Linux bug). [B2]
        if ((File.Exists(newPath) || Directory.Exists(newPath)) && !PathFacts.SamePath(newPath, item.FullPath))
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
            // Always close the edit box here — the service only resets IsBeingEdited on
            // its success path.
            item.IsBeingEdited = false;
            _pendingRenameItem = null;
            RenameText = string.Empty;
        }
    }

    [RelayCommand]
    private void CancelRename()
    {
        if (_pendingRenameItem is not null)
            _pendingRenameItem.IsBeingEdited = false;

        _pendingRenameItem = null;
        RenameText = string.Empty;
    }

    // Escape / Enter fold both edit modes into one command apiece, so a single
    // KeyBinding covers "cancel whatever is open" / "commit whatever is open".
    [RelayCommand]
    private void CancelEdits()
    {
        CancelCreate();
        CancelRename();
    }

    // Enter: commit whatever edit is open; otherwise activate the current row.
    [RelayCommand]
    private void ConfirmOrOpen()
    {
        if (_pendingRenameItem is not null) { ConfirmRename(); return; }
        if (IsCreatingFolder || IsCreatingFile) { ConfirmCreate(); return; }
        Open(HoveredItem ?? ContextItem);
    }

    // ─── CLIPBOARD ─────────────────────────────
    [RelayCommand]
    private void Copy()
    {
        if (SelectedItems.Count == 0) return;
        _service.ClipboardCopyItems(SelectedItems);
        SetStatus($"Copied {SelectedItems.Count} item(s).");
    }

    [RelayCommand]
    private void Cut()
    {
        if (SelectedItems.Count == 0) return;
        _service.ClipboardCutItems(SelectedItems);
        SetStatus($"Cut {SelectedItems.Count} item(s).");
    }

    [RelayCommand]
    private async Task Paste()
    {
        if (!_service.Clipboard.HasItems) return;

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

    // ─── DELETE ────────────────────────────────
    [RelayCommand]
    private async Task DeleteItem(FileExplorerItem? item)
    {
        if (item is null) return;

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
        if (SelectedItems.Count == 0) return;

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

    // Hover-first Delete for the Del key: hover wins, selection is the fallback
    // (file-explorer semantics, straight from the kit).
    [RelayCommand(CanExecute = nameof(CanDeleteHovered))]
    private async Task DeleteHovered()
    {
        if (HoveredItem is { } hovered)
            await DeleteItem(hovered);
        else
            await DeleteMultiple();
    }

    private bool CanDeleteHovered() => HoveredItem is not null || SelectedItems.Count > 0;

    // ─── REFRESH / EXPAND ──────────────────────
    [RelayCommand]
    private void RefreshDirectory()
    {
        try
        {
            _service.RefreshDirectory();
            // Status comes from the ExplorerRefreshed handler.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh failed");
            SetStatus($"Refresh failed: {ex.Message}");
        }
    }

    // A null item means "the whole tree" (toolbar buttons); a non-null item means
    // "this subtree" (context menu).
    [RelayCommand]
    private void ExpandAll(FileExplorerItem? item)
    {
        if (item is not null) { _service.ExpandAll(item); return; }
        foreach (var root in RootItems) _service.ExpandAll(root);
    }

    [RelayCommand]
    private void CollapseAll(FileExplorerItem? item)
    {
        if (item is not null) { _service.CollapseAll(item); return; }
        foreach (var root in RootItems) _service.CollapseAll(root);
    }

    // ─── SERVICE EVENT HANDLERS ────────────────
    private void OnItemCreated(object? sender, FileExplorerItem item) => SetStatus($"Created '{item.Name}'");

    private void OnItemRenamed(object? sender, FileExplorerItem item) => SetStatus($"Renamed to '{item.Name}'");

    private void OnItemDeleted(object? sender, string path)
    {
        // ContextItem pointing at the deleted item — or anything inside a deleted
        // folder — is a ghost: the next F2/paste would target a vanished path.
        if (ContextItem is not null &&
            (PathFacts.SamePath(ContextItem.FullPath, path) || PathFacts.IsUnder(ContextItem.FullPath, path)))
        {
            ContextItem = SelectedItems.LastOrDefault();
        }

        SetStatus($"Deleted '{Path.GetFileName(path)}'");
    }

    private void OnExplorerRefreshed(object? sender, EventArgs e)
    {
        // HasDirectory is deliberately NOT recomputed from item count here: an empty
        // folder must not disable the toolbar and make the first file uncreatable.
        SetStatus($"Refreshed — {CountItems()} item(s).");
    }

    // ─── PRIVATE HELPERS ───────────────────────
    private static FileExplorerItem? ResolveParentFolder(FileExplorerItem? item)
        => item switch
        {
            null => null,
            { IsDirectory: true } => item,
            _ => item.Parent
        };

    private FileExplorerItem? FindByPath(string path)
    {
        foreach (var root in RootItems)
            if (FindByPath(root, path) is { } hit) return hit;
        return null;
    }

    private static FileExplorerItem? FindByPath(FileExplorerItem item, string path)
    {
        if (PathFacts.SamePath(item.FullPath, path)) return item;
        foreach (var child in item.Children)
            if (FindByPath(child, path) is { } hit) return hit;
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

    // Path.GetFileName("C:\") is "" — fall back to the raw path for roots.
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

    private void SetStatus(string message) => StatusMessage = message;
}