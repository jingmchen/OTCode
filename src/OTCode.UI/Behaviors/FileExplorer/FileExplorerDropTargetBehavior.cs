// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Threading;
using OTCode.Core.Domains.Behaviors;
using OTCode.Core.Domains.FileExplorer;
using OTCode.UI.Constants;

namespace OTCode.UI.Behaviors.FileExplorer;

public sealed class FileExplorerDropTargetBehavior : DropTargetBehavior
{
    public static readonly DependencyProperty TargetContextProperty =
        DependencyProperty.Register(
            nameof(TargetContext),
            typeof(object),
            typeof(FileExplorerDropTargetBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty AutoExpandProperty =
        DependencyProperty.Register(
            nameof(AutoExpand),
            typeof(bool),
            typeof(FileExplorerDropTargetBehavior),
            new PropertyMetadata(true));

    public object? TargetContext
    {
        get => GetValue(TargetContextProperty);
        set => SetValue(TargetContextProperty, value);
    }

    public bool AutoExpand
    {
        get => (bool)GetValue(AutoExpandProperty);
        set => SetValue(AutoExpandProperty, value);
    }

    private DispatcherTimer? _autoExpandTimer;

    // VM drop command gets both the dragged path and destination folder
    protected override object? BuildCommandParameter(DragEventArgs e)
        => TryGetPayload(e) is { } payload
            ? new FileDropRequest(payload, TargetContext)
            : null;
    
    protected override void OnValidDragOver()
    {
        if (AutoExpand)
            ArmAutoExpand();
    }

    protected override void OnDragOverEnded() => CancelAutoExpand();

    private void ArmAutoExpand()
    {
        if (_autoExpandTimer is not null)
            return;
        
        if (TargetContext is not FileExplorerItem {IsDirectory: true, IsExpanded: false} folder)
            return;

        _autoExpandTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(UIConstants.Control.FileExplorer.AutoExpandDelayMs)
        };

        _autoExpandTimer.Tick += (_, _) =>
        {
            CancelAutoExpand();
            
            try
            {
                folder.IsExpanded = true;
            }
            catch { /* Nothing to catch */ }
        };
        
        _autoExpandTimer.Start();
    }

    private void CancelAutoExpand()
    {
        _autoExpandTimer?.Stop();
        _autoExpandTimer = null;
    }
}