// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Models.Transpiler.Text;

namespace OTCode.Core.Models.Transpiler.Nodes;

public abstract class Node
{
    public TextSpan Span {get;}
    public LinePosition Position {get;}

    public Node(TextSpan span, LinePosition position)
    {
        Span = span;
        Position = position;
    }
}