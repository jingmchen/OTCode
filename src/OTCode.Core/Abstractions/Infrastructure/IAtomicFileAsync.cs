// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;

namespace OTCode.Core.Abstractions.Infrastructure;

public interface IAtomicFileAsync
{
    /// <summary>
    /// Use AtomicFile instead for write methods that is fire & forget
    /// </summary>
    Task WriteAsync(string path, string contents, Encoding? encoding = null);
}