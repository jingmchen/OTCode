// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.FileExplorer;

public sealed class PanelOptions
{
    public double Width {get; set;} = 260;
    public double Height {get; set;} = double.NaN;
    public double MaxWidth {get; set;} = 700;
    public double MinWidth {get; set;} = 150;
    public bool IsResizable {get; set;}
    public bool ShowFileSize {get; set;} = true;
    public bool ShowIcons {get; set;}
    public bool AllowMultiSelect {get; set;} = true;
}