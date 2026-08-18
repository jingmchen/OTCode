// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.Infrastructure;

public interface IFileWatcherService : IDisposable
{
    event EventHandler<FileSystemEventArgs>? Changed;
    void StartWatching(string path);
    void StopWatching();
}