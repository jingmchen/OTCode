// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Enums;

namespace OTCode.Core.Domains.FileExplorer;

public sealed class FileExplorerClipboard
{
    public ClipboardOperation Operation {get; private set;} = ClipboardOperation.None;
    public IReadOnlyList<FileExplorerItem> Items {get; private set;} = [];
    public bool HasItems => Items.Count > 0 && Operation != ClipboardOperation.None;

    public void SetCopy(IEnumerable<FileExplorerItem> items)
        => Set(ClipboardOperation.Copy, items, cut: false);
    
    public void SetCut(IEnumerable<FileExplorerItem> items)
        => Set(ClipboardOperation.Cut, items, cut: true);
    
    public void SetNone()
    {
        foreach (var item in Items)
            item.IsCut = false;
        Items = [];
        Operation = ClipboardOperation.None;
    }

    public List<FileExplorerItem> Snapshot()
        => [.. Items];

    private void Set(ClipboardOperation operation, IEnumerable<FileExplorerItem> items, bool cut)
    {
        ArgumentNullException.ThrowIfNull(items);
        SetNone();
        Operation = operation;
        Items = [.. items];
        foreach (var item in Items)
            item.IsCut = cut;
    }
}