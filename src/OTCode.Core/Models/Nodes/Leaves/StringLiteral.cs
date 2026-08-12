// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Models.Nodes.Leaves;

public sealed class StringLiteral : Expression
{
    public string RawText {get;}

    public StringLiteral(string text)
        => Text = text;
}