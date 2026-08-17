// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Domains.FilePicker;

namespace OTCode.Core.Options.FilePicker;

public sealed class FilePickerOptions
{
    public string? Title {get; set;}
    public string? InitialDirectory {get; set;}
    public IReadOnlyList<FileFilter> Filters {get; set;} = [];
}