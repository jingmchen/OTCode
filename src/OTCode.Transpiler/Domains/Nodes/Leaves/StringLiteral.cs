// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Models.Transpiler.Nodes.Expressions;
using OTCode.Core.Models.Transpiler.Text;

namespace OTCode.Core.Models.Transpiler.Nodes.Leaves;

public sealed class StringLiteral : Expression
{
    public string RawText {get;}

    public StringLiteral(string text, TextSpan span, LinePosition position) : base(span, position)
        => RawText = text;
}