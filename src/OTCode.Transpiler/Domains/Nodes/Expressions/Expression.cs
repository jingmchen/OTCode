// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Models.Transpiler.Text;

namespace OTCode.Core.Models.Transpiler.Nodes.Expressions;

public abstract class Expression : Node
{
    protected Expression(TextSpan span, LinePosition position) : base(span, position) { }
}