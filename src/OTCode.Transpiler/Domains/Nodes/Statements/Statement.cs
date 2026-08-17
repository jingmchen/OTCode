// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Models.Transpiler.Text;

namespace OTCode.Core.Models.Transpiler.Nodes.Statements;

public abstract class Statement : Node
{
    protected Statement(TextSpan span, LinePosition position) : base(span, position) { }
}