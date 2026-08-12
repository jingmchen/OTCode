// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Models.Text;

public sealed record struct LinePosition
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