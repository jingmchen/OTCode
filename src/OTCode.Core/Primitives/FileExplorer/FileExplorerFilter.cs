// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Primitives.FileExplorer;

public readonly record struct FileExplorerFilter
{
    public IReadOnlySet<string> Entries {get; init;}
    public bool IsWhitelist {get; init;}

    public bool Passes(string value)
    {
        if (Entries is null || Entries.Count == 0)
            return true;
        
        return IsWhitelist
            ? Entries.Contains(value)
            : !Entries.Contains(value);
    }
}