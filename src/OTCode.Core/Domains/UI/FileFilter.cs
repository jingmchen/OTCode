// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Domains.UI;

public sealed record FileFilter(
    string Name,
    IReadOnlyList<string> Patterns
);