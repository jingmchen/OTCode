// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Models.Nodes.Statements;

public abstract class Statement : Node
{
    protected Statement(TextSpan span, LinePosition position) : base(span, position)
}