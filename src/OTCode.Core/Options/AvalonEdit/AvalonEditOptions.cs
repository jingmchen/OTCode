// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.AvalonEdit;

public sealed class AvalonEditOptions
{
    public EditorOptions Editor {get; set;} = new();
    public FontOptions Font {get; set;} = new();
    public ColorOptions Color {get; set;} = new();
}