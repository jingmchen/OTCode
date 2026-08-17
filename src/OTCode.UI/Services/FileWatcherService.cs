// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.IO;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Logging;
using OTCode.UI.Constants;

namespace OTCode.UI.Services;

public sealed partial class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private FileSystemWatcher? _watcher;
    private FileSystemEventArgs? _pendingEvent;
    private Timer? _debounceTimer;
    private readonly TimeSpan _debounce;
    private bool _isdisposed;
    private readonly Lock _gate = new();
    public event EventHandler<FileSystemEventArgs>? Changed;

    public FileWatcherService(ILogger<FileWatcherService> logger, TimeSpan debounce)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _debounce = debounce;
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
            EnableRaisingEvents = true,
            InternalBufferSize = UIConstants.Service.FileWatcher.InternalBufferSize
        };

        watcher.Changed += OnDirEvent;
        watcher.Created += OnDirEvent;
        watcher.Deleted += OnDirEvent;
        watcher.Renamed += OnDirEvent;
        watcher.Error += OnError;

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
                _watcher.Changed -= OnDirEvent;
                _watcher.Created -= OnDirEvent;
                _watcher.Deleted -= OnDirEvent;
                _watcher.Renamed -= OnDirEvent;
                _watcher.Error -= OnError;
                _watcher.Dispose();
                _watcher = null;
            }
        }
    }

    public void Dispose()
    {
        if (_isdisposed)
            return;
        _isdisposed = true;
        StopWatching();
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void OnDirEvent(object? sender, FileSystemEventArgs e)
    {
        lock (_gate)
        {
            _pendingEvent = e;
            _debounceTimer?.Dispose(); // Reset debounce window each time a new event arrives
            _debounceTimer = new Timer(FireDebounced, null, _debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        LogUnexpectedError(ex);
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

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_isdisposed, nameof(FileWatcherService));
    
    [LoggerMessage(
        EventId = LogEventIDs.UI.FileWatcher.UnexpectedError,
        Level = LogLevel.Information,
        Message = "FileWatcherService stopped due to an error."
    )]
    private partial void LogUnexpectedError(Exception ex);
}