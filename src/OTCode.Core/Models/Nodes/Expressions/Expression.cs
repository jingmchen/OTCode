// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Models.Nodes.Expressions;

public abstract class Expression : Node
{
    protected Expression(TextSpan span, LinePosition position) : base(span, position)
}