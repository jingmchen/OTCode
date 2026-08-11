// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Domains.UI;

namespace OTCode.Core.Abstractions.UI;

public interface IFilePickerService
{
    string? OpenFile(FilePickerOptions)
    Task<string?> PickFolderAsync(string title);
}