// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.AvalonEdit;

public sealed record AvalonEditOptions
{
    public EditorOptions Editor {get; set;} = new();
}