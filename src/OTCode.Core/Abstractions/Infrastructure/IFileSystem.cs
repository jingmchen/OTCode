// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.Infrastructure;

public interface IFileSystem
{
    Task<string> ReadAllTextAsync(string path, CancellationToken ct);
    Task WriteAllTextAsync(string path, string text, CancellationToken ct);
}