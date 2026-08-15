// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Domains.AtomicOperations;
using OTCode.Infrastructure.Utils;

namespace OTCode.Infrastructure.Services;

public sealed class AtomicFileAsync : IAtomicFileAsync
{
    private readonly ConcurrentDictionary<string, Lazy<ChannelWriter<WriteRequest>>> _queues = new(PathComparer);
    private readonly ConcurrentDictionary<string, byte> _pendingCleanup = new(PathComparer);
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    
    public AtomicFileAsync() { }

    // ─── PUBLIC METHODS ────────────────────────
    public void Write(string path, string contents, Encoding? encoding = null)
        => WriteAsync(path, contents, encoding).GetAwaiter().GetResult();

    public Task WriteAsync(string path, string contents, Encoding? encoding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        string fullPath = Path.GetFullPath(path);

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        WriteRequest request = new(contents, encoding, completion);

        ChannelWriter<WriteRequest> writer = _queues.GetOrAdd(
            fullPath,
            path => new Lazy<ChannelWriter<WriteRequest>>(() => CreateQueue(path))).Value;

        if (!writer.TryWrite(request))
            throw new IOException($"Unable to queue write for '{path}'");
        
        return completion.Task;
    }

    // ─── PRIVATE METHODS ───────────────────────
    private ChannelWriter<WriteRequest> CreateQueue(string path)
    {
        var channel = Channel.CreateUnbounded<WriteRequest>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            }
        );

        _ = ProcessQueueAsync(path, channel.Reader);

        return channel.Writer;
    }

    private async Task ProcessQueueAsync(string path, ChannelReader<WriteRequest> reader)
    {
        await foreach (WriteRequest request in reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                RetryOldCleanups();

                AtomicFile.WriteTo(
                    path: path,
                    contents: request.Contents,
                    encoding: request.Encoding,
                    cleanupFailed: AddToCleanup);
                
                request.Completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                request.Completion.TrySetException(ex);
            }
        }
    }

    private void RetryOldCleanups()
    {
        foreach (string tempPath in _pendingCleanup.Keys)
        {
            if (_pendingCleanup.TryRemove(tempPath, out _) && !AtomicFile.TryDelete(tempPath))
                AddToCleanup(tempPath);
        }
    }

    private void AddToCleanup(string tempPath)
        => _pendingCleanup.TryAdd(tempPath, 0);
}