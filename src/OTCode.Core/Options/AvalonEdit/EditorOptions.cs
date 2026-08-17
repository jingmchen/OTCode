// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.AvalonEdit;

public sealed record EditorOptions
{
    public bool ShowLineNumbers {get; init;} = true;
    public bool ShowSpaces {get; init;}
    public bool ShowTabs {get; init;} = true;
    public bool ShowEndOfLine {get; init;}

    public bool WordWrap {get; init;} = true;
    public bool ConvertTabsToSpaces {get; init;} = true;
    public bool HighlightCurrentLine {get; init;} = true;

    public int IndentationSize {get; init;} = 4;
}