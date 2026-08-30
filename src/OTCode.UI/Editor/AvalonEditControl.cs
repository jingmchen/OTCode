// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using ICSharpCode.AvalonEdit;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Enums;
using OTCode.Core.Options.AvalonEdit;

namespace OTCode.UI.Editor;

public sealed class AvalonEditControl : TextEditor
{
    private readonly IEditorService<AvalonEditControl, AvalonEditOptions> _service;
    private bool _syncingText;

    // AvalonEdit Text Editor text as bindable property since TextEditor.Text is not bindable
    public string BindableText
    {
        get => (string)GetValue(BindableTextProperty);
        set => SetValue(BindableTextProperty, value);
    }

    public static readonly DependencyProperty BindableTextProperty = DependencyProperty.Register(
        nameof(BindableText),
        typeof(string),
        typeof(AvalonEditControl),
        new FrameworkPropertyMetadata(
            "",
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnBindableTextChanged));
    
    public AvalonEditControl(IEditorService<AvalonEditControl, AvalonEditOptions> service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);

        if (_syncingText)
            return;
        
        _syncingText = true;

        try
        {
            SetCurrentValue(BindableTextProperty, Text);
        }
        finally
        {
            _syncingText = false;
        }
    }
    
    private static void OnBindableTextChanged(
        DependencyObject dependencyObj,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (AvalonEditControl)dependencyObj;

        if (control._syncingText)
            return;
        
        control._syncingText = true;

        try
        {
            control.Text = e.NewValue as string ?? "";
        }
        finally
        {
            control._syncingText = false;
        }
    }

    private void 

    private bool TryExecute(TextEditorShortcut shortcut, int wheelDelta)
    {
        switch (shortcut)
        {
            case TextEditorShortcut.ZoomIn:
                _service.ZoomIn(this);
                return true;

            case TextEditorShortcut.ZoomOut:
                _service.ZoomOut(this);
                return true;

            case TextEditorShortcut.ZoomReset:
                _service.ResetZoom(this);
                return true;

            case TextEditorShortcut.ScrollHorizontal:
                ScrollToHorizontalOffset(HorizontalOffset - wheelDelta);
                return true;

            default:
                return false;
        }
    }
}