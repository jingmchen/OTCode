// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.AvalonEdit;

public sealed class EditorOptions
{
    public bool ShowLineNumbers {get; set;} = true;
    public bool ShowSpaces {get; set;}
    public bool ShowTabs {get; set;} = true;
    public bool ShowEndOfLine {get; set;}

    public bool WordWrap {get; set;} = true;
    public bool ConvertTabsToSpaces {get; set;} = true;
    public bool HighlightCurrentLine {get; set;} = true;

    public int IndentationSize {get; set;} = 4;
}