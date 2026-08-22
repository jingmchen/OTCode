// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace OTCode.UI.Behaviors;

public class DropTargetBehavior : Behavior<Control>
{
    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.Register(
            nameof(DropCommand),
            typeof(ICommand),
            typeof(DropTargetBehavior),
            new PropertyMetadata(null));
    
    public static readonly DependencyProperty IsDropOkProperty =
        DependencyProperty.Register(
            "IsDropOk",
            typeof(bool),
            typeof(DropTargetBehavior),
            new FrameworkPropertyMetadata(false));
    
    public ICommand? DropCommand
    {
        get => (ICommand?)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    public static bool GetIsDropOk(DependencyObject element)
        => (bool)element.GetValue(IsDropOkProperty);
    
    public static void SetIsDropOk(DependencyObject element, bool value)
        => element.SetValue(IsDropOkProperty, value);

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.AllowDrop = true;

        AssociatedObject.AddHandler(DragDrop.DragOverEvent, new DragEventHandler(OnDragOver));
        AssociatedObject.AddHandler(DragDrop.DragLeaveEvent, new DragEventHandler(OnDragLeave));
        AssociatedObject.AddHandler(DragDrop.DropEvent, new DragEventHandler(OnDrop));
    }

    protected override void OnDetaching()
    {
        AssociatedObject.RemoveHandler(DragDrop.DragOverEvent, new DragEventHandler(OnDragOver));
        AssociatedObject.RemoveHandler(DragDrop.DragLeaveEvent, new DragEventHandler(OnDragLeave));
        AssociatedObject.RemoveHandler(DragDrop.DropEvent, new DragEventHandler(OnDrop));
        
        AssociatedObject.AllowDrop = false;
        SetIsDropOk(AssociatedObject, false);
        OnDragOverEnded();

        base.OnDetaching();
    }

    protected virtual object? BuildCommandParameter(DragEventArgs e)
        => TryGetPayload(e);
    
    protected static string? TryGetPayload(DragEventArgs e)
        => e.Data.GetDataPresent(DataFormats.Text)
            ? e.Data.GetData(DataFormats.Text) as string
            : null;

    protected virtual void OnValidDragOver() { }

    protected virtual void OnDragOverEnded() { }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        try
        {
            var parameter = BuildCommandParameter(e);
            var ok = parameter is not null && DropCommand?.CanExecute(parameter) == true;

            e.Effects = ok
                ? DragDropEffects.Move
                : DragDropEffects.None;
            
            SetIsDropOk(AssociatedObject, ok);

            if (ok)
                OnValidDragOver();
            else
                OnDragOverEnded();
        }
        catch
        {
            e.Effects = DragDropEffects.None;
            SetIsDropOk(AssociatedObject, false);
            OnDragOverEnded();
        }
        finally
        {
            e.Handled = true;
        }
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        SetIsDropOk(AssociatedObject, false);
        OnDragOverEnded();
    }
    
    private void OnDrop(object? sender, DragEventArgs e)
    {
        SetIsDropOk(AssociatedObject, false);
        OnDragOverEnded();

        object? parameter;

        try
        {
            parameter = BuildCommandParameter(e);
        }
        catch
        {
            return;
        }

        var command = DropCommand;

        if (parameter is null || command is null || !command.CanExecute(parameter))
            return;

        e.Handled = true;

        ICommand deferredCommand = command;
        object deferredParameter = parameter;
        
        AssociatedObject.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                // Re-check as state may have changed between drop and this turn.
                if (deferredCommand.CanExecute(deferredParameter))
                    deferredCommand.Execute(deferredParameter);
            }));
    }
}