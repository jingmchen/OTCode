// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.AvalonEdit;

public sealed class AvalonEditOptions
{
    public AvalonEditEditorOptions Editor {get; set;} = new();
    public AvalonEditFontOptions Font {get; set;} = new();
    public AvalonEditColorOptions Color {get; set;} = new();

    public AvalonEditOptions SanitizeValidate()
    {
        Editor ??= new AvalonEditEditorOptions();
        Font ??= new AvalonEditFontOptions();
        Color ??= new AvalonEditColorOptions();

        return this;
    }
}