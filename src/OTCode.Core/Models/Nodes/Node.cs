// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Models.Nodes;

public abstract class Node
{
    public TextSpan Span {get;}
    public LinePosition Position {get;}

    public Node(TextSpan span, LinePosition position)
    {
        Span = span;
        LinePosition = position;
    }
}