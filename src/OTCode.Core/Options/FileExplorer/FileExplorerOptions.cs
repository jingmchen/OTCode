// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.FileExplorer;

public sealed class FileExplorerOptions
{
    public ServiceOptions Service {get; set;} = new();
    public PanelOptions Panel {get; set;} = new();
}