// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Infrastructure.Utils;

internal static class DirectoryHelper
{
    internal static string MakeUniquePath(string directory, string baseName, string extension)
    {
        string candidate = Path.Combine(directory, baseName + extension);
        int n = 2;
        
        while(File.Exists(candidate))
            candidate = Path.Combine(directory, $"{baseName} ({n++}){extension}");
        
        return candidate;
    }

    internal static string MakeRelativePath(string fromFile, string toFile)
        => Path.GetRelativePath(Path.GetDirectoryName(fromFile)!, toFile)
            .Replace(Path.DirectorySeparatorChar, '/');
    
    internal static bool PathsEqual(string path1, string path2)
        => string.Equals(Path.GetFullPath(path1), Path.GetFullPath(path2),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}