// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Infrastructure.Utils;

internal static class DirectoryHelper
{   
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
}