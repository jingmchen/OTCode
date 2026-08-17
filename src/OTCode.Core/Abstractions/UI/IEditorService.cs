// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.UI;

public interface IEditorService<TEditor, TOptions>
    where TEditor : class
    where TOptions : class
{
    TOptions Options {get;}

    TEditor CreateEditor();
    void ConfigureEditor(TEditor editor);
    void SetSyntaxHighlighting(TEditor editor, string? colorFormat);
    void SetOptions(TOptions options);
}