// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using AvaloniaEdit;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.UI;

namespace OTCode.UI.Services;

public sealed class AvaloniaEditorService : IEditorService<TextEditor>
{
    private readonly ILogger<AvaloniaEditorService> _logger;

    public AvaloniaEditorService(ILogger<AvaloniaEditorService> logger)
        => _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    // ─── PUBLIC METHODS ────────────────────────
    public TextEditor CreateEditor()
    {
        var editor = new TextEditor();
        return editor;
    }

    public void ConfigureEditor(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        editor.ShowLineNumbers = true;
    }

    public void SetSyntaxHighlighting(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
    }

    // ─── PRIVATE METHODS ───────────────────────
}