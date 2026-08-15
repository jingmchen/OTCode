// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Models.Text;

public readonly record struct TextSpan
{
    public int Start {get;}
    public int Length {get;}
    public int End => Start + Length;

    public TextSpan(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        Start = start;
        Length = length;
    }

    public static TextSpan FromBounds(int start, int end)
    {
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), end, "Start cannot be larger than End");
        return new(start, end - start);
    }
}