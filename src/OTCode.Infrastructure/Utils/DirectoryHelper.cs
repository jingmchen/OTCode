// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Infrastructure.Utils;

internal static class DirectoryHelper
{
    internal static void CopyDirectory(string source, string destination, bool overwrite = false)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite);

        foreach (var dir in Directory.GetDirectories(source))
        {
            if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0)
                continue; // Ignore reparse points
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)), overwrite);
        }
    }

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
    
    internal static bool PathsEqual(string path1, string path2)
        => string.Equals(Path.GetFullPath(path1), Path.GetFullPath(path2),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    
    internal static bool IsFile(string name)
        => File.Exists(name) ? true : false;
    
    internal static bool IsFolder(string name)
        => Directory.Exists(name) ? true : false;
}