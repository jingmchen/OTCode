// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.UI;

public interface IEditorService<T>
{
    T CreateEditor();
    void ConfigureEditor(T editor);
    void SetSyntaxHighlighting(T editor);
}