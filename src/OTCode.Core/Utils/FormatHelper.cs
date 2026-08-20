// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Utils;

public static class FormatHelper
{
    public static string FormatBytes(long bytes) => bytes switch
    {
        < 1_024L => FormattableString.Invariant($"{bytes} B"),
        < 1_048_576L => FormattableString.Invariant($"{bytes / (double)1_024L:F1} KB"),
        < 1_073_741_824L => FormattableString.Invariant($"{bytes / (double)1_048_576L:F1} MB"),
        _ => FormattableString.Invariant($"{bytes / (double)1_073_741_824L:F2} GB")
    };
}