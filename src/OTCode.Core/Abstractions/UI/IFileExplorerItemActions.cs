// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Domains.FileExplorer;

namespace OTCode.Core.Abstractions.UI;

public interface IFileExplorerItemActions
{
    void Open(FileExplorerItem item);
    void ShowProperties(FileExplorerItem item);
}