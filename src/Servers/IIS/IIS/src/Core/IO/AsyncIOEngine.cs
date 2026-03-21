// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal sealed partial class AsyncIOEngine : IAsyncIOEngine, IDisposable
{
    private const ushort ResponseMaxChunks = 65533;

    private readonly IISHttpContext _context;
    private readonly NativeSafeHandle _handler;

    private bool _stopped;

    private AsyncIOOperation? _nextOperation;
    private AsyncIOOperation? _runningOperation;

    private AsyncReadOperation? _cachedAsyncReadOperation;
    private AsyncWriteOperation? _cachedAsyncWriteOperation;
    private AsyncFlushOperation? _cachedAsyncFlushOperation;

    public AsyncIOEngine(IISHttpContext context, NativeSafeHandle handler)
    {
        _context = context;
        _handler = handler;
    }

    public ValueTask<int> ReadAsync(Memory<byte> memory)
    {
        var read = GetReadOperation();
        read.Initialize(_handler, memory);
        Run(read);
        return new ValueTask<int>(read, 0);
    }

    public ValueTask<int> WriteAsync(ReadOnlySequence<byte> data)
    {
        if (SegmentsOverChunksLimit(data))
        {
            return WriteDataOverChunksLimit(data);
        }

        return WriteDataAsync(data);
    }

    private ValueTask<int> WriteDataAsync(in ReadOnlySequence<byte> data)
    {
        var write = GetWriteOperation();
        write.Initialize(_handler, data);
        Run(write);
        return new ValueTask<int>(write, 0);
    }

    // In case the number of chunks is bigger than responseMaxChunks we need to make multiple calls
    // to the native api https://learn.microsoft.com/iis/web-development-reference/native-code-api-reference/ihttpresponse-writeentitychunks-method
    // Despite the documentation states that feeding the function with more than 65535 chunks will cause the function to throw an exception,
    // it actually seems that 65534 is the maximum number of chunks allowed.
    // Also, there seems to be a problem when slicing a ReadOnlySequence on segment borders tracked here https://github.com/dotnet/runtime/issues/67607
    // That's why we only allow 65533 chunks.
    private async ValueTask<int> WriteDataOverChunksLimit(ReadOnlySequence<byte> data)
    {
        ushort segmentsCount = 0;
        var length = 0;

        // Since the result is discarded in the only place it's used (IISHttpContext.WriteBody), we return the last result.
        // If we start using the result there, we should make sure we handle the value correctly here.
        var result = 0;

        foreach (var segment in data)
        {
            segmentsCount++;
            length += segment.Length;

            if (segmentsCount == ResponseMaxChunks)
            {
                result = await WriteDataAsync(data.Slice(0, length));

                data = data.Slice(length);
                segmentsCount = 0;
                length = 0;
            }
        }

        if (segmentsCount > 0)
        {
            result = await WriteDataAsync(data);
        }

        return result;
    }

    private static bool SegmentsOverChunksLimit(in ReadOnlySequence<byte> data)
    {
        if (data.IsSingleSegment)
        {
            return false;
        }

        var count = 0;

        foreach (var _ in data)
        {
            count++;

            if (count > ResponseMaxChunks)
            {
                return true;
            }
        }

        return false;
    }

    private void Run(AsyncIOOperation ioOperation)
    {
        lock (_context._contextLock)
        {
            if (_stopped)
            {
                // Abort all operation after IO was stopped
                ioOperation.Complete(NativeMethods.ERROR_OPERATION_ABORTED, 0);
                return;
            }

            if (_runningOperation != null)
            {
                if (_nextOperation == null)
                {
                    _nextOperation = ioOperation;

                    // If there is an active read cancel it
                    if (_runningOperation is AsyncReadOperation)
                    {
                        NativeMethods.HttpTryCancelIO(_handler);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Only one queued operation is allowed");
                }
            }
            else
            {
                // we are just starting operation so there would be no
                // continuation registered
                var completed = ioOperation.Invoke() != null;

                // operation went async
                if (!completed)
                {
                    _runningOperation = ioOperation;
                }
            }
        }
    }

    public ValueTask FlushAsync(bool moreData)
    {
        var flush = GetFlushOperation();
        flush.Initialize(_handler, moreData);
        Run(flush);
        return new ValueTask(flush, 0);
    }

    public void NotifyCompletion(int hr, int bytes)
    {
        AsyncIOOperation.AsyncContinuation continuation;
        AsyncIOOperation.AsyncContinuation? nextContinuation = null;

        lock (_context._contextLock)
        {
            Debug.Assert(_runningOperation != null);

            continuation = _runningOperation.Complete(hr, bytes);

            var next = _nextOperation;
            _nextOperation = null;
            _runningOperation = null;

            if (next != null)
            {
                if (_stopped)
                {
                    // Abort next operation if IO is stopped
                    nextContinuation = next.Complete(NativeMethods.ERROR_OPERATION_ABORTED, 0);
                }
                else
                {
                    nextContinuation = next.Invoke();

                    // operation went async
                    if (nextContinuation == null)
                    {
                        _runningOperation = next;
                    }
                }
            }
        }

        continuation.Invoke();
        nextContinuation?.Invoke();
    }

    public void Complete()
    {
        lock (_context._contextLock)
        {
            _stopped = true;

            // Should only call CancelIO if the client hasn't disconnected
            if (!_context.ClientDisconnected)
            {
                NativeMethods.HttpTryCancelIO(_handler);
            }
        }
    }

    private AsyncReadOperation GetReadOperation() =>
        Interlocked.Exchange(ref _cachedAsyncReadOperation, null) ??
        new AsyncReadOperation(this);

    private AsyncWriteOperation GetWriteOperation() =>
        Interlocked.Exchange(ref _cachedAsyncWriteOperation, null) ??
        new AsyncWriteOperation(this);

    private AsyncFlushOperation GetFlushOperation() =>
        Interlocked.Exchange(ref _cachedAsyncFlushOperation, null) ??
        new AsyncFlushOperation(this);

    private void ReturnOperation(AsyncReadOperation operation)
    {
        Volatile.Write(ref _cachedAsyncReadOperation, operation);
    }

    private void ReturnOperation(AsyncWriteOperation operation)
    {
        Volatile.Write(ref _cachedAsyncWriteOperation, operation);
    }

    private void ReturnOperation(AsyncFlushOperation operation)
    {
        Volatile.Write(ref _cachedAsyncFlushOperation, operation);
    }

    public void Dispose()
    {
        _stopped = true;
    }
}
