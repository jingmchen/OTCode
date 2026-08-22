// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Utils;

public static class PathHelper
{
    private static readonly bool CaseSensitive =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
    
    private static StringComparison Comparison {get;} =
        CaseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    
    public static StringComparer Comparer {get;} =
        CaseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    
    public static bool SamePath(string? path1, string? path2)
        => path1 is not null && path2 is not null
            && string.Equals(
                Path.TrimEndingDirectorySeparator(path1),
                Path.TrimEndingDirectorySeparator(path2),
                Comparison);
    
    public static bool IsUnder(string candidate, string ancestorDir)
    {
        var ancestor = Path.TrimEndingDirectorySeparator(ancestorDir) + Path.DirectorySeparatorChar;
        var child = Path.TrimEndingDirectorySeparator(candidate) + Path.DirectorySeparatorChar;
        return child.StartsWith(ancestor, Comparison);
    }
}