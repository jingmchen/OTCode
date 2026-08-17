// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Models.Transpiler.Text;

public readonly record struct LinePosition
{
    public int Line {get;}
    public int Column {get;}

    public LinePosition(int line, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(line);
        ArgumentOutOfRangeException.ThrowIfNegative(column);

        Line = line;
        Column = column;
    }
}