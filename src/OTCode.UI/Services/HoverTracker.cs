// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Abstractions.UI;

namespace OTCode.UI.Services;

public sealed class HoverTracker : IHoverTracker
{
    private object? _hoveredItem;

    public void SetHovered(object? item)
        => _hoveredItem = item;

    public void ClearHovered(object? item)
    {
        if (ReferenceEquals(_hoveredItem, item))
            _hoveredItem = null;
    }
}