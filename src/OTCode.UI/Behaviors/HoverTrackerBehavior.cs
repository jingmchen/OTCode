// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using OTCode.Core.Abstractions.UI;

namespace OTCode.UI.Behaviors;

public sealed class HoverTrackerBehavior : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(
            nameof(Item),
            typeof(object),
            typeof(HoverTrackerBehavior),
            new PropertyMetadata(null));
    
    public static readonly DependencyProperty TrackerProperty =
        DependencyProperty.Register(
            nameof(Tracker),
            typeof(IHoverTracker),
            typeof(HoverTrackerBehavior),
            new PropertyMetadata(null));
    
    public object? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public IHoverTracker? Tracker
    {
        get => (IHoverTracker?)GetValue(TrackerProperty);
        set => SetValue(TrackerProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.MouseEnter += OnEntered;
        AssociatedObject.MouseLeave += OnExited;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.MouseEnter -= OnEntered;
        AssociatedObject.MouseLeave -= OnExited;

        if (AssociatedObject.IsMouseOver)
            Tracker?.ClearHovered(Item);
        
        base.OnDetaching();
    }

    private void OnEntered(object? sender, MouseEventArgs e)
        => Tracker?.SetHovered(Item);
    
    private void OnExited(object? sender, MouseEventArgs e)
        => Tracker?.ClearHovered(Item);
}