// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.IO;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Logging;
using OTCode.UI.Constants;

namespace OTCode.Infrastructure.Services;

public sealed partial class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private FileSystemWatcher? _watcher;
    private FileSystemEventArgs? _pendingEvent;
    private Timer? _debounceTimer;
    private readonly TimeSpan _debounce;
    private readonly Lock _gate = new();
    private bool _disposed;

    public event EventHandler<FileSystemEventArgs>? Changed;

    public FileWatcherService(ILogger<FileWatcherService> logger, TimeSpan debounce)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _debounce = debounce ?? TimeSpan.FromMilliseconds(400);
    }
    
    // ─── PUBLIC METHODS ────────────────────────
    public void StartWatching(string path)
    {
        ThrowIfDisposed();
        StopWatching();
        
        if (!Directory.Exists(path))
            return;

        var watcher = new FileSystemWatcher(path)
        {
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.Attributes,
            IncludeSubdirectories = true,
            InternalBufferSize = UIConstants.Service.FileWatcher.InternalBufferSize
        };

        watcher.Changed += OnWatcherEvent;
        watcher.Created += OnWatcherEvent;
        watcher.Deleted += OnWatcherEvent;
        watcher.Renamed += OnWatcherEvent;
        watcher.Error += OnError;
        watcher.EnableRaisingEvents = true;

        lock(_gate)
            _watcher = watcher;
    }

    public void StopWatching()
    {
        lock (_gate)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
            _pendingEvent = null;

            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnWatcherEvent;
                _watcher.Created -= OnWatcherEvent;
                _watcher.Deleted -= OnWatcherEvent;
                _watcher.Renamed -= OnWatcherEvent;
                _watcher.Error -= OnError;
                _watcher.Dispose();
                _watcher = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        StopWatching();
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void OnWatcherEvent(object? sender, FileSystemEventArgs e)
    {
        lock (_gate)
        {
            _pendingEvent = e;
            _debounceTimer?.Dispose(); // Reset debounce window each time a new event arrives
            _debounceTimer = new Timer(FireDebounced, null, _debounce, Timeout.InfiniteTimeSpan);
        }
    }
    
    private void FireDebounced(object? _)
    {
        FileSystemEventArgs? evnt;

        lock (_gate)
        {
            evnt = _pendingEvent;
            _pendingEvent = null;
        }

        if (evnt is not null)
            Changed?.Invoke(this, evnt);
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        LogUnexpectedError(ex);
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, nameof(FileWatcherService));
    
    [LoggerMessage(
        EventId = LogEventIDs.UI.FileWatcher.UnexpectedError,
        Level = LogLevel.Information,
        Message = "FileWatcherService encountered an error."
    )]
    private partial void LogUnexpectedError(Exception ex);
}