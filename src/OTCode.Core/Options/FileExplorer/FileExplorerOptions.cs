// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.FileExplorer;

public sealed class FileExplorerOptions
{
    
    public PanelOptions Sanitize()
    {
        if (MinWidth > MaxWidth)
            throw new ArgumentException($"{nameof(MinWidth)} ({MinWidth}) > {nameof(MaxWidth)} ({MaxWidth}).");
        
        if (!double.IsNaN(PanelWidth) && (PanelWidth < MinPanelWidth || PanelWidth > MaxPanelWidth))
            throw new ArgumentException($"{nameof(PanelWidth)} ({PanelWidth}) outside [{MinPanelWidth}, {MaxPanelWidth}].");
        
        return this;
    }
}