// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using ICSharpCode.AvalonEdit;
using OTCode.Core.Abstractions.UI;

namespace OTCode.UI.Services;

public sealed class AvalonEditService : IEditorService<TextEditor, AvalonEditOptions>
{
    private readonly ILogger<AvalonEditService> _logger;
    public AvalonEditOptions Options {get;}

    public AvalonEditService(ILogger<AvalonEditService> logger, AvalonEditOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    // ─── PUBLIC METHODS ────────────────────────
    public TextEditor CreateEditor()
    {
        var editor = new TextEditor();
        ConfigureEditor(editor);
        return editor;
    }

    public void ConfigureEditor(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Apply(editor, Options);
    }

    // Separate from ConfigureEditor to call when theme changes
    public void SetSyntaxHighlighting(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
    }

    public void SetOptions(AvalonEditOptions options)
        => Options = options ?? throw new ArgumentNullException(nameof(options));
    
    // ─── PRIVATE METHODS ───────────────────────
    private static void Apply(TextEditor editor, AvalonEditOptions options)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(options);

        editor.FontFamily = new FontFamily(options.FontOptions.Family);
        editor.FontSize = options.FontOptions.Size;
        editor.ShowLineNumbers = options.EditorOptions.ShowLineNumbers;
        editor.WordWrap = options.EditorOptions.WordWrap;

        editor.Options.HighlightCurrentLine = options.EditorOptions.HighlightCurrentLine;
        editor.Options.ShowSpaces = options.EditorOptions.ShowSpaces;
        editor.Options.ShowTabs = options.EditorOptions.ShowTabs;
        editor.Options.ShowEndOfLine = options.EditorOptions.ShowEndOfLine;
        editor.Options.ConvertTabsToSpaces = options.EditorOptions.ConvertTabsToSpaces;
        editor.options.IndentationSize = options.EditorOptions.IndentationSize;

        SetBrush(editor, Control.ForegroundProperty, options.ColorOptions.ForegroundColor);
        SetBrush(editor, Control.BackgroundProperty, options.ColorOptions.BackgroundColor);
        SetBrush(editor, TextEditor.LineNumbersForegroundProperty, options.ColorOptions.LineNumbersColor);
        SetBrush(editor.TextArea, TextArea.SelectionBrushProperty, options.ColorOptions.SelectionColor);
        ApplyCurrentLine(editor.TextArea.TextView, options.ColorOptions.CurrentLineColor);
    }

    private static void SetBrush(DependencyObject target, DependencyObject property, string? color)
    {
        if (color is null)
            target.ClearValue(property);
    }
}