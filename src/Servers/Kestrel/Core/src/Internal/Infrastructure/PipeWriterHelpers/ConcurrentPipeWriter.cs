// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

/// <summary>
/// Wraps a PipeWriter so you can start appending more data to the pipe prior to the previous flush completing.
/// </summary>
internal sealed class ConcurrentPipeWriter : PipeWriter
{
    // The following constants were copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeWriter.cs
    // and the associated StreamPipeWriterOptions defaults.
    private const int InitialSegmentPoolSize = 4; // 16K
    private const int MaxSegmentPoolSize = 256; // 1MB
    private const int MinimumBufferSize = 4096; // 4K

    private static readonly Exception _successfullyCompletedSentinel = new UnreachableException();

    private readonly Lock _sync;
    private readonly PipeWriter _innerPipeWriter;
    private readonly MemoryPool<byte> _pool;
    private readonly BufferSegmentStack _bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);

    private BufferSegment? _head;
    private BufferSegment? _tail;
    private Memory<byte> _tailMemory;
    private int _tailBytesBuffered;
    private long _bytesBuffered;

    // When _currentFlushTcs is null and _head/_tail is also null, the ConcurrentPipeWriter is in passthrough mode.
    // When the ConcurrentPipeWriter is not in passthrough mode, that could be for one of two reasons:
    //
    // 1. A flush of the _innerPipeWriter is in progress.
    // 2. Or the last flush of the _innerPipeWriter completed between external calls to GetMemory/Span() and Advance().
    //
    // In either case, we need to manually append buffer segments until the loop in the current or next call to FlushAsync()
    // flushes all the buffers putting the ConcurrentPipeWriter back into passthrough mode.
    // The manual buffer appending logic is borrowed from corefx's StreamPipeWriter.
    private TaskCompletionSource<FlushResult>? _currentFlushTcs;
    private bool _bufferedWritePending;

    // We're trusting the Http2FrameWriter and Http1OutputProducer to not call into the PipeWriter after calling Abort() or Complete().
    // If an Abort() is called while a flush is in progress, we clean up after the next flush completes, and don't flush again.
    private bool _aborted;
    // If an Complete() is called while a flush is in progress, we clean up after the flush loop completes, and call Complete() on the inner PipeWriter.
    private Exception? _completeException;

    public ConcurrentPipeWriter(PipeWriter innerPipeWriter, MemoryPool<byte> pool, Lock sync)
    {
        _innerPipeWriter = innerPipeWriter;
        _pool = pool;
        _sync = sync;
    }

    public void Reset()
    {
        Debug.Assert(_currentFlushTcs == null, "There should not be a pending flush.");

        _aborted = false;
        _completeException = null;
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_currentFlushTcs == null && _head == null)
        {
            return _innerPipeWriter.GetMemory(sizeHint);
        }

        AllocateMemoryUnsynchronized(sizeHint);
        return _tailMemory;
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_currentFlushTcs == null && _head == null)
        {
            return _innerPipeWriter.GetSpan(sizeHint);
        }

        AllocateMemoryUnsynchronized(sizeHint);
        return _tailMemory.Span;
    }

    public override void Advance(int bytes)
    {
        if (_currentFlushTcs == null && _head == null)
        {
            _innerPipeWriter.Advance(bytes);
            return;
        }

        if ((uint)bytes > (uint)_tailMemory.Length)
        {
            ThrowArgumentOutOfRangeException(nameof(bytes));
        }

        _tailBytesBuffered += bytes;
        _bytesBuffered += bytes;
        _tailMemory = _tailMemory.Slice(bytes);
        _bufferedWritePending = false;
    }

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_currentFlushTcs != null)
        {
            return new ValueTask<FlushResult>(_currentFlushTcs.Task);
        }

        if (_bytesBuffered > 0)
        {
            CopyAndReturnSegmentsUnsynchronized();
        }

        var flushTask = _innerPipeWriter.FlushAsync(cancellationToken);

        if (flushTask.IsCompletedSuccessfully)
        {
            if (_currentFlushTcs != null)
            {
                CompleteFlushUnsynchronized(flushTask.GetAwaiter().GetResult(), null);
            }

            return flushTask;
        }

        // Use a TCS instead of something custom so it can be awaited by multiple awaiters.
        _currentFlushTcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new ValueTask<FlushResult>(_currentFlushTcs.Task);

        // FlushAsyncAwaited clears the TCS prior to completing. Make sure to construct the ValueTask
        // from the TCS before calling FlushAsyncAwaited in case FlushAsyncAwaited completes inline.
        _ = FlushAsyncAwaited(flushTask, cancellationToken);
        return result;
    }

    private async Task FlushAsyncAwaited(ValueTask<FlushResult> flushTask, CancellationToken cancellationToken)
    {
        try
        {
            // This while (true) does look scary, but the real continuation condition is at the start of the loop
            // after the await, so the _sync lock can be acquired.
            while (true)
            {
                var flushResult = await flushTask;

                lock (_sync)
                {
                    if (_bytesBuffered == 0 || _aborted)
                    {
                        CompleteFlushUnsynchronized(flushResult, null);
                        return;
                    }

                    if (flushResult.IsCanceled)
                    {
                        Debug.Assert(_currentFlushTcs != null);

                        // Complete anyone currently awaiting a flush with the canceled FlushResult since CancelPendingFlush() was called.
                        _currentFlushTcs.SetResult(flushResult);
                        // Reset _currentFlushTcs, so we don't enter passthrough mode while we're still flushing.
                        _currentFlushTcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                    }

                    CopyAndReturnSegmentsUnsynchronized();

                    flushTask = _innerPipeWriter.FlushAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                CompleteFlushUnsynchronized(default, ex);
            }
        }
    }

    public override void CancelPendingFlush()
    {
        // FlushAsyncAwaited never ignores a canceled FlushResult.
        _innerPipeWriter.CancelPendingFlush();
    }

    // To return all the segments without completing the inner pipe, call Abort().
    public override void Complete(Exception? exception = null)
    {
        // Store the exception or sentinel in a field so that if a flush is ongoing, we call the
        // inner Complete() method with the correct exception or lack thereof once the flush loop ends.
        _completeException = exception ?? _successfullyCompletedSentinel;

        if (_currentFlushTcs == null)
        {
            if (_bytesBuffered > 0)
            {
                CopyAndReturnSegmentsUnsynchronized();
            }

            CleanupSegmentsUnsynchronized();

            _innerPipeWriter.Complete(exception);
        }
    }

    public override bool CanGetUnflushedBytes => _innerPipeWriter.CanGetUnflushedBytes;
    public override long UnflushedBytes
    {
        get
        {
            return _innerPipeWriter.UnflushedBytes + _bytesBuffered;
        }
    }

    public void Abort()
    {
        _aborted = true;

        // If we're flushing, the cleanup will happen after the flush.
        if (_currentFlushTcs == null)
        {
            CleanupSegmentsUnsynchronized();
        }
    }

    private void CleanupSegmentsUnsynchronized()
    {
        BufferSegment? segment = _head;
        while (segment != null)
        {
            BufferSegment returnSegment = segment;
            segment = segment.NextSegment;
            returnSegment.ResetMemory();
        }

        _head = null;
        _tail = null;
        _tailMemory = null;
    }

    private void CopyAndReturnSegmentsUnsynchronized()
    {
        Debug.Assert(_tail != null);

        // Update any buffered data
        _tail.End += _tailBytesBuffered;
        _tailBytesBuffered = 0;

        var segment = _head;

        while (segment != null)
        {
            _innerPipeWriter.Write(segment.Memory.Span);

            var returnSegment = segment;
            segment = segment.NextSegment;

            // We haven't reached the tail of the linked list yet, so we can always return the returnSegment.
            if (segment != null)
            {
                returnSegment.ResetMemory();
                ReturnSegmentUnsynchronized(returnSegment);
            }
        }

        if (_bufferedWritePending)
        {
            // If an advance is pending, so is a flush, so the _tail segment should still get returned eventually.
            _head = _tail;
        }
        else
        {
            _tail.ResetMemory();
            ReturnSegmentUnsynchronized(_tail);
            _head = _tail = null;
        }

        // Even if a non-passthrough call to Advance is pending, there a 0 bytes currently buffered.
        _bytesBuffered = 0;
    }

    private void CompleteFlushUnsynchronized(FlushResult flushResult, Exception? flushEx)
    {
        // Ensure all blocks are returned prior to the last call to FlushAsync() completing.
        if (_completeException != null || _aborted)
        {
            CleanupSegmentsUnsynchronized();
        }

        if (ReferenceEquals(_completeException, _successfullyCompletedSentinel))
        {
            _innerPipeWriter.Complete();
        }
        else if (_completeException != null)
        {
            _innerPipeWriter.Complete(_completeException);
        }

        Debug.Assert(_currentFlushTcs != null);
        if (flushEx != null)
        {
            _currentFlushTcs.SetException(flushEx);
        }
        else
        {
            _currentFlushTcs.SetResult(flushResult);
        }

        _currentFlushTcs = null;
    }

    // The methods below were copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeWriter.cs
    private void AllocateMemoryUnsynchronized(int sizeHint)
    {
        _bufferedWritePending = true;

        if (_head == null)
        {
            // We need to allocate memory to write since nobody has written before
            BufferSegment newSegment = AllocateSegmentUnsynchronized(sizeHint);

            // Set all the pointers
            _head = _tail = newSegment;
            _tailBytesBuffered = 0;
        }
        else
        {
            int bytesLeftInBuffer = _tailMemory.Length;

            if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint)
            {
                Debug.Assert(_tail != null);

                if (_tailBytesBuffered > 0)
                {
                    // Flush buffered data to the segment
                    _tail.End += _tailBytesBuffered;
                    _tailBytesBuffered = 0;
                }

                BufferSegment newSegment = AllocateSegmentUnsynchronized(sizeHint);

                _tail.SetNext(newSegment);
                _tail = newSegment;
            }
        }
    }

    private BufferSegment AllocateSegmentUnsynchronized(int minSize)
    {
        BufferSegment newSegment = CreateSegmentUnsynchronized();

        if (minSize <= _pool.MaxBufferSize)
        {
            // Use the specified pool if it fits
            newSegment.SetOwnedMemory(_pool.Rent(GetSegmentSize(minSize, _pool.MaxBufferSize)));
        }
        else
        {
            // We can't use the recommended pool so use the ArrayPool
            newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(minSize));
        }

        _tailMemory = newSegment.AvailableMemory;

        return newSegment;
    }

    private BufferSegment CreateSegmentUnsynchronized()
    {
        if (_bufferSegmentPool.TryPop(out var segment))
        {
            return segment;
        }

        return new BufferSegment();
    }

    private void ReturnSegmentUnsynchronized(BufferSegment segment)
    {
        if (_bufferSegmentPool.Count < MaxSegmentPoolSize)
        {
            _bufferSegmentPool.Push(segment);
        }
    }

    private static int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
    {
        // First we need to handle case where hint is smaller than minimum segment size
        sizeHint = Math.Max(MinimumBufferSize, sizeHint);
        // After that adjust it to fit into pools max buffer size
        var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
        return adjustedToMaximumSize;
    }

    // Copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.Memory/src/System/ThrowHelper.cs
    private static void ThrowArgumentOutOfRangeException(string argumentName) { throw CreateArgumentOutOfRangeException(argumentName); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception CreateArgumentOutOfRangeException(string argumentName) { return new ArgumentOutOfRangeException(argumentName); }
}
