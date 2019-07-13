// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers
{
    /// <summary>
    /// Wraps a PipeWriter so you can start appending more data to the pipe prior to the previous flush completing.
    /// </summary>
    internal class ConcurrentPipeWriter : PipeWriter
    {
        // The following constants were copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeWriter.cs
        // and the associated StreamPipeWriterOptions defaults.
        private const int InitialSegmentPoolSize = 4; // 16K
        private const int MaxSegmentPoolSize = 256; // 1MB
        private const int MinimumBufferSize = 4096; // 4K

        private static readonly Exception _successfullyCompletedSentinel = new Exception();

        private readonly object _sync = new object();
        private readonly PipeWriter _innerPipeWriter;
        private readonly MemoryPool<byte> _pool;
        private readonly BufferSegmentStack _bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);

        private BufferSegment _head;
        private BufferSegment _tail;
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
        private TaskCompletionSource<FlushResult> _currentFlushTcs;
        private bool _bufferedWritePending;

        // We're trusting the Http2FrameWriter and Http1OutputProducer to not call into the PipeWriter after calling Abort() or Complete()
        // If an abort occurs while a flush is in progress, we clean up after the flush completes, and don't flush again.
        private bool _aborted;
        private Exception _completeException;

        public ConcurrentPipeWriter(PipeWriter innerPipeWriter, MemoryPool<byte> pool)
        {
            _innerPipeWriter = innerPipeWriter;
            _pool = pool;
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null && _head == null)
                {
                    return _innerPipeWriter.GetMemory(sizeHint);
                }

                AllocateMemoryUnsynchronized(sizeHint);
                return _tailMemory;
            }
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null && _head == null)
                {
                    return _innerPipeWriter.GetSpan(sizeHint);
                }

                AllocateMemoryUnsynchronized(sizeHint);
                return _tailMemory.Span;
            }
        }

        public override void Advance(int bytes)
        {
            lock (_sync)
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
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
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

                // Use a TCS instead of something resettable so it can be awaited by multiple awaiters.
                _currentFlushTcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = FlushAsyncAwaited(flushTask, cancellationToken);
                return new ValueTask<FlushResult>(_currentFlushTcs.Task);
            }
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
                            // Complete anyone currently awaiting a flush since CancelPendingFlush() was called
                            CompleteFlushUnsynchronized(flushResult, null);
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

        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            _innerPipeWriter.OnReaderCompleted(callback, state);
        }

        public override void CancelPendingFlush()
        {
            // We propagate IsCanceled when we do multiple flushes in a loop. If FlushResult.IsCanceled is true with more data pending to flush,
            // _currentFlushTcs with canceled flush task, but rekick the FlushAsync loop.
            _innerPipeWriter.CancelPendingFlush();
        }

        public override void Complete(Exception exception = null)
        {
            lock (_sync)
            {
                // We store the complete exception or  s sentinel exception instance in a field  if a flush was ongoing.
                // We call the inner Complete() method after the flush loop ended.

                // To simply ensure everything gets returned after the PipeWriter is left in some unknown state (say GetMemory() was
                // called but not Advance(), or there's a flush pending), but you don't want to complete the inner pipe, just call Abort().
                _completeException = exception ?? _successfullyCompletedSentinel;

                if (_currentFlushTcs == null)
                {
                    CleanupSegmentsUnsynchronized();
                    _innerPipeWriter.Complete(exception);
                }
            }
        }

        public void Abort()
        {
            lock (_sync)
            {
                _aborted = true;

                // If we're flushing, the cleanup will happen after the flush.
                if (_currentFlushTcs == null)
                {
                    CleanupSegmentsUnsynchronized();
                }
            }
        }

        private void CleanupSegmentsUnsynchronized()
        {
            BufferSegment segment = _head;
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

        private void CompleteFlushUnsynchronized(FlushResult flushResult, Exception flushEx)
        {
            // Ensure all blocks are returned prior to the last call to FlushAsync() completing.
            if (_aborted || _completeException != null)
            {
                CleanupSegmentsUnsynchronized();
            }

            if (_completeException != null)
            {
                if (_completeException == _successfullyCompletedSentinel)
                {
                    _innerPipeWriter.Complete();
                }
                else
                {
                    _innerPipeWriter.Complete(_completeException);
                }
            }

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

        private BufferSegment AllocateSegmentUnsynchronized(int sizeHint)
        {
            BufferSegment newSegment = CreateSegmentUnsynchronized();

            if (sizeHint <= _pool.MaxBufferSize)
            {
                // Use the specified pool if it fits
                newSegment.SetOwnedMemory(_pool.Rent(GetSegmentSize(sizeHint, _pool.MaxBufferSize)));
            }
            else
            {
                // We can't use the pool so allocate an array
                newSegment.SetUnownedMemory(new byte[sizeHint]);
            }

            _tailMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            if (_bufferSegmentPool.TryPop(out BufferSegment segment))
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
}
