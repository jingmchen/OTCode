// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Domains.FileExplorer;

namespace OTCode.Infrastructure.Factories;

public static class FileExplorerItemFactory
{
    public static FileExplorerItem FromPath(string path, FileExplorerItem? parent = null)
    {
        FileSystemInfo info = Directory.Exists(path)
            ? new DirectoryInfo(path)
            : new FileInfo(path);
        
        var attrs = info.Attributes;

        return new FileExplorerItem
        {
            Name = info.Name,
            FullPath = info.FullName,
            Extension = info is FileInfo fi ? fi.Extension.ToLowerInvariant() : "",
            IsDirectory = info is DirectoryInfo,
            Size = info is FileInfo fileInfo ? fileInfo.Length : 0L,
            CreatedAt = info.CreationTime,
            LastModifiedAt = info.LastWriteTime,
            IsHidden = attrs.HasFlag(FileAttributes.Hidden),
            IsSymLink = attrs.HasFlag(FileAttributes.ReparsePoint),
            IsReadOnly = attrs.HasFlag(FileAttributes.ReadOnly),
            Parent = parent
        };
    }
}