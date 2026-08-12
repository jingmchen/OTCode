// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace OTCode.UI.Behaviors;

public sealed class WheelRouterBehavior : Behavior<Control>
{
    public static readonly DependencyProperty CtrlWheelCommandProperty =
        DependencyProperty.Register(
            nameof(CtrlWheelCommand),
            typeof(ICommand),
            typeof(WheelRouterBehavior),
            new PropertyMetadata(null));
    
    public static readonly DependencyProperty ShiftWheelCommandProperty =
        DependencyProperty.Register(
            nameof(ShiftWheelCommand),
            typeof(ICommand),
            typeof(WheelRouterBehavior),
            new PropertyMetadata(null));
    
    public ICommand? CtrlWheelCommand
    {
        get => (ICommand?)GetValue(CtrlWheelCommandProperty);
        set => SetValue(CtrlWheelCommandProperty, value);
    }

    public ICommand? ShiftWheelCommand
    {
        get => (ICommand?)GetValue(ShiftWheelCommandProperty);
        set => SetValue(ShiftWheelCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject?.AddHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnWheel));
    }

    protected override void OnDetaching()
    {
        AssociatedObject?.RemoveHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnWheel));

        base.OnDetaching();
    }

    private void OnWheel(object? sender, MouseWheelEventArgs e)
    {
        ICommand? command = null;

        var delta = e.Delta;
        var modifiers = Keyboard.Modifiers;

        if ((modifiers & ModifierKeys.Control) != 0)
            command = CtrlWheelCommand;
        else if ((modifiers & ModifierKeys.Shift) != 0)
            command = ShiftWheelCommand;

        if (command?.CanExecute(delta) == true)
        {
            command.Execute(delta);
            e.Handled = true;
        }
    }
}