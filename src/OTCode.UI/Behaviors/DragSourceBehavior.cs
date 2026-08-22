// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using OTCode.UI.Constants;

namespace OTCode.UI.Behaviors;

public sealed class DragSourceBehavior : Behavior<FrameworkElement>
{
    private Point _origin;
    private bool _pressed;
    public static readonly DependencyProperty PayloadProperty =
        DependencyProperty.Register(
            nameof(Payload),
            typeof(string),
            typeof(DragSourceBehavior),
            new PropertyMetadata(null));
    
    public static readonly DependencyProperty ThresholdProperty =
        DependencyProperty.Register(
            nameof(Threshold),
            typeof(double),
            typeof(DragSourceBehavior),
            new PropertyMetadata(UIConstants.Behavior.DragSource.DragThreshold));
    
    // Stable id to hand to the drop target
    public string? Payload
    {
        get => (string?)GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    // Distance in DIPs the pointer must travel before a drag starts
    public double Threshold
    {
        get => (double)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.PreviewMouseLeftButtonDown += OnPressed;
        AssociatedObject.PreviewMouseLeftButtonUp += OnReleased;
        AssociatedObject.LostMouseCapture += OnCaptureLost;
        AssociatedObject.PreviewMouseMove += OnMoved;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= OnPressed;
        AssociatedObject.PreviewMouseLeftButtonUp -= OnReleased;
        AssociatedObject.LostMouseCapture -= OnCaptureLost;
        AssociatedObject.PreviewMouseMove -= OnMoved;

        base.OnDetaching();
    }

    private void OnPressed(object? sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        _origin = PointerPosition(e);
        _pressed = true;
    }

    private void OnReleased(object? sender, MouseButtonEventArgs e)
        => _pressed = false;
    
    private void OnCaptureLost(object? sender, MouseEventArgs e)
        => _pressed = false;
    
    private async void OnMoved(object? sender, MouseEventArgs e)
    {
        if (!_pressed || Payload is not { } payload)
            return;
        
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            _pressed = false;
            return;
        }
        
        Vector delta = PointerPosition(e) - _origin;
        if (Math.Abs(delta.X) < Threshold && Math.Abs(delta.Y) < Threshold)
            return; // Still within click territory
        
        _pressed = false;

        var data = new DataObject();
        data.SetData(DataFormats.Text, payload);
        
        try
        {
            DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
        }
        catch { /* nothing to clean up */ }
    }
    
    private Point PointerPosition(MouseEventArgs e)
    {
        IInputElement reference =
            PresentationSource.FromVisual(AssociatedObject)?.RootVisual as IInputElement
                ?? Window.GetWindow(AssociatedObject)
                ?? AssociatedObject;
        
        return e.GetPosition(reference);
    }
}