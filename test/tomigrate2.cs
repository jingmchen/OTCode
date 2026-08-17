// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using MacroCanvas.Core.Abstractions.FileExplorer;
using MacroCanvas.Core.Models;

namespace MacroCanvas.UI.ViewModels;

public sealed partial class FileExplorerViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<FileExplorerViewModel> _logger;
    private readonly IFileExplorerService _service;
    public ObservableCollection<FileExplorerItem> RootItems {get => _service.RootItems;}
    public ObservableCollection<FileExplorerItem> SelectedItems {get => _service.SelectedItems;}
    [ObservableProperty] public partial FileExplorerItem? ContextItem {get; set;}
    [ObservableProperty] public partial string CurrentRootPath {get; set;} = "";
    [ObservableProperty] public partial bool HasDirectory {get; set;}
    [ObservableProperty] public partial bool IsCreatingFile {get; set;}
    [ObservableProperty] public partial bool IsCreatingFolder {get; set;}
    [ObservableProperty] public partial string NewFileName {get; set;} = "";
    [ObservableProperty] public partial string RenameText {get; set;} = "";
    [ObservableProperty] public partial string StatusMessage {get; set;} = "";
    private FileExplorerItem? _pendingCreateParent;
    private FileExplorerItem? _pendingRenameItem;

    public FileExplorerViewModel(ILogger<FileExplorerViewModel> logger, IFileExplorerService service)
    {
        _logger = logger;
        _service = service;

        _service.ItemCreated += OnItemCreated;
        _service.ItemRenamed += OnItemRenamed;
        _service.ItemDeleted += OnItemDeleted;
        _service.ExplorerRefreshed += OnExplorerRefreshed;
    }

    public void LoadDirectory()
    {
        LoadDirectory(_service.Options.RootPath!);
    }

    public void LoadDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            _service.LoadDirectory(path);
        }
        catch
        {
            SetStatus("Unable to load path");
            return;
        }
    }

    public void Dispose()
    {
        _service.ItemCreated -= OnItemCreated;
        _service.ItemRenamed -= OnItemRenamed;
        _service.ItemDeleted -= OnItemDeleted;
        _service.ExplorerRefreshed -= OnExplorerRefreshed;
    }

    // Status logging methods
    private void SetStatus(string message)
        => StatusMessage = message;
    
    private void OnItemCreated(object? sender, FileExplorerItem item)
        => SetStatus($"Created '{item.Name}'");
    
    private void OnItemRenamed(object? sender, FileExplorerItem item)
        => SetStatus($"Renamed '{item.Name}'");
    
    private void OnItemDeleted(object? sender, string path)
    {
        if (ContextItem is not null
            && (string.Equals(ContextItem.FullPath, path, StringComparison.OrdinalIgnoreCase)
            || ContextItem.FullPath.StartsWith(path + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                ContextItem = SelectedItems.LastOrDefault();
        SetStatus($"Deleted '{Path.GetFileName(path)}'");
    }

    private void OnExplorerRefreshed(object? sender, EventArgs e)
        => SetStatus("Refreshed explorer.");
}