// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Domains.FileExplorer;

public sealed record FileExplorerFilter
{
    public IReadOnlySet<string> Entries {get; init;} =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    
    public bool IsWhitelist {get; init;}

    public bool Passes(string value)
        => IsWhitelist
            ? Entries.Count == 0 || Entries.Contains(value)
            : !Entries.Contains(value);
}