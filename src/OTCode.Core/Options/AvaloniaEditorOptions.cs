// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options;

public sealed record AvaloniaEditorOptions
{
    public bool ShowLineNumbers {get; init;} = true;
    public bool WordWrap {get; init;} = true;
}