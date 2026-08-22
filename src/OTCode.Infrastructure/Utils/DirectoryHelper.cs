// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Infrastructure.Utils;

internal static class DirectoryHelper
{
    private static readonly bool CaseSensitive =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
    
    private static StringComparison Comparison {get;} =
        CaseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    
    internal static StringComparer Comparer {get;} =
        CaseSensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    
    internal static string MakeUniquePath(string directory, string name, bool isFile = true)
    {
        var baseName = isFile
            ? Path.GetFileNameWithoutExtension(name)
            : name;
        
        var extension = isFile
            ? Path.GetExtension(name)
            : "";
        
        var candidate = Path.Combine(directory, name);
        var n = 1;
        
        while(Path.Exists(candidate))
            candidate = Path.Combine(directory, $"{baseName} ({n++}){extension}");
        
        return candidate;
    }

    internal static string MakeRelativePath(string fromFile, string toFile)
        => Path.GetRelativePath(Path.GetDirectoryName(fromFile)!, toFile)
            .Replace(Path.DirectorySeparatorChar, '/');
    
    internal static bool SamePath(string? path1, string? path2)
        => path1 is not null && path2 is not null
            && string.Equals(
                Path.TrimEndingDirectorySeparator(path1),
                Path.TrimEndingDirectorySeparator(path2),
                Comparison);
    
    internal static bool IsUnder(string candidate, string ancestorDir)
    {
        var ancestor = Path.TrimEndingDirectorySeparator(ancestorDir) + Path.DirectorySeparatorChar;
        var child = Path.TrimEndingDirectorySeparator(candidate) + Path.DirectorySeparatorChar;
        return child.StartsWith(ancestor, Comparison);
    }
}