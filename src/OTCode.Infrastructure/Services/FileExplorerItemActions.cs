// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Abstractions.UI;
using OTCode.Core.Domains.FileExplorer;

namespace OTCode.Infrastructure.Services;

public sealed class FileExplorerItemActions : IFileExplorerItemActions
{
    private readonly Action<FileExplorerItem>? _open;
    private readonly Action<FileExplorerItem>? _showProperties;

    public FileExplorerItemActions(
        Action<FileExplorerItem>? open = null,
        Action<FileExplorerItem>? showProperties = null
    )
    {
        _open = open;
        _showProperties = showProperties;
    }

    public void Open(FileExplorerItem item)
        => _open?.Invoke(item);
    
    public void ShowProperties(FileExplorerItem item)
        => _showProperties?.Invoke(item);
}