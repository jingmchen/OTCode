// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Domains.AtomicOperations;
using OTCode.Infrastructure.Utils;

namespace OTCode.Infrastructure.Services;

public sealed class AtomicFileWriter : IAtomicFileWriter
{
    private readonly ConcurrentDictionary<string, Lazy<ChannelWriter<WriteRequest>>> _queues = new(PathComparer);
    private readonly ConcurrentDictionary<string, byte> _pendingCleanup = new(PathComparer);
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    public Task WriteToAsync(string path, string contents, Encoding? encoding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        WriteRequest request = new(contents, encoding, completion);

        ChannelWriter<WriteRequest> writer = _queues.GetOrAdd(
            path,
            path => new Lazy<ChannelWriter<WriteRequest>>(() => CreateQueue(path))).Value;

        if (!writer.TryWrite(request))
            throw new IOException($"Unable to queue write for '{path}'");
        
        return completion.Task;
    }

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

        _ = Task.Run(() => ProcessQueueAsync(path, channel.Reader));

        return channel.Writer;
    }

    private async Task ProcessQueueAsync(string path, ChannelReader<WriteRequest> reader)
    {
        await foreach (WriteRequest request in reader.ReadAllAsync())
        {
            try
            {
                RetryOldCleanups();

                AtomicFile.WriteTo(
                    path: path,
                    contents: request.Contents,
                    encoding: request.Encoding,
                    AddToCleanup);
                
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
            if (!_pendingCleanup.TryRemove(tempPath, out _))
                continue;
            
            try { File.Delete(tempPath); }
            catch { AddToCleanup(tempPath); }
        }
    }

    private void AddToCleanup(string tempPath)
        => _pendingCleanup.TryAdd(tempPath, 0);
}