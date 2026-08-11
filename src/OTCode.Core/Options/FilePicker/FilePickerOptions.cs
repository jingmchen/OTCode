// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Domains.UI;

namespace OTCode.Core.Options.FilePicker;

public sealed record FilePickerOptions
{
    public string? Title {get; init;}
    public string? InitialDirectory {get; init;}
    public IReadOnlyList<FileFilter> Filters {get; init;} = [];
}