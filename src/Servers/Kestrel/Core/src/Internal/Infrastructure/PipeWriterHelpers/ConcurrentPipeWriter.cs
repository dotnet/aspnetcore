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
    internal sealed class ConcurrentPipeWriter : PipeWriter
    {
        // The following constants were copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeWriter.cs
        // and the associated StreamPipeWriterOptions defaults.
        private const int InitialSegmentPoolSize = 4; // 16K
        private const int MaxSegmentPoolSize = 256; // 1MB
        private const int MinimumBufferSize = 4096; // 4K

        private static readonly Exception _successfullyCompletedSentinel = new Exception();
        private static readonly Task<FlushResult> _completedFlush = Task.FromResult<FlushResult>(default);
        private static readonly Task<FlushResult> _completedFlushWithPreviousError = Task.FromResult<FlushResult>(default);

        private readonly object _sync;
        private readonly PipeWriter _innerPipeWriter;
        private readonly MemoryPool<byte> _pool;
        private readonly BufferSegmentStack _bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);

        private BufferSegment _head;
        private BufferSegment _tail;
        private Memory<byte> _tailMemory;
        private int _tailBytesBuffered;
        private long _bytesBuffered;

        // When _currentFlush IsCompletedSuccessfully and _head/_tail is also null, the ConcurrentPipeWriter is in passthrough mode.
        // When the ConcurrentPipeWriter is not in passthrough mode, that could be for one of two reasons:
        //
        // 1. A flush of the _innerPipeWriter is in progress.
        // 2. Or the last flush of the _innerPipeWriter completed between external calls to GetMemory/Span() and Advance().
        //
        // In either case, we need to manually append buffer segments until the loop in the current or next call to FlushAsync()
        // flushes all the buffers putting the ConcurrentPipeWriter back into passthrough mode.
        // The manual buffer appending logic is borrowed from corefx's StreamPipeWriter.
        private Task<FlushResult> _currentFlush = _completedFlush;
        private bool _bufferedWritePending;

        // We're trusting the Http2FrameWriter and Http1OutputProducer to not call into the PipeWriter after calling Abort() or Complete().
        // If an Abort() is called while a flush is in progress, we clean up after the next flush completes, and don't flush again.
        private bool _aborted;
        // If an Complete() is called while a flush is in progress, we clean up after the flush loop completes, and call Complete() on the inner PipeWriter.
        private Exception _completeException;

        public ConcurrentPipeWriter(PipeWriter innerPipeWriter, MemoryPool<byte> pool, object sync)
        {
            _innerPipeWriter = innerPipeWriter;
            _pool = pool;
            _sync = sync;
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_currentFlush.IsCompletedSuccessfully && _head == null)
            {
                return _innerPipeWriter.GetMemory(sizeHint);
            }

            AllocateMemoryUnsynchronized(sizeHint);
            return _tailMemory;
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_currentFlush.IsCompletedSuccessfully && _head == null)
            {
                return _innerPipeWriter.GetSpan(sizeHint);
            }

            AllocateMemoryUnsynchronized(sizeHint);
            return _tailMemory.Span;
        }

        public override void Advance(int bytes)
        {
            if (_currentFlush.IsCompletedSuccessfully && _head == null)
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
            if (!_currentFlush.IsCompletedSuccessfully)
            {
                return new ValueTask<FlushResult>(_currentFlush);
            }

            if (_bytesBuffered > 0)
            {
                CopyAndReturnSegmentsUnsynchronized();
            }

            var flushTask = _innerPipeWriter.FlushAsync(cancellationToken);

            if (flushTask.IsCompletedSuccessfully)
            {
                if (!_currentFlush.IsCompletedSuccessfully)
                {
                    var flushResult = flushTask.GetAwaiter().GetResult();
                    CompleteFlushUnsynchronized();

                    // Create a new ValueTask from the result rather than passing out the original
                    // as the act of reading the result above can reset the ValueTask if its backed by an IValueTaskSource
                    return new ValueTask<FlushResult>(flushResult);
                }

                return flushTask;
            }

            var lastFlush = _currentFlush;
            var currentFlush = FlushAsyncAwaited(flushTask, cancellationToken);

            lock (_sync)
            {
                if (ReferenceEquals(_currentFlush, lastFlush))
                {
                    // Update _currentFlush if it is the same as lastFlush i.e. if FlushAsyncAwaited hasn't already replaced it
                    _currentFlush = currentFlush;
                }
                else if (ReferenceEquals(_currentFlush, _completedFlushWithPreviousError))
                {
                    // If _currentFlush was set to completed in FlushAsyncAwaited after an error; reset it to _completedFlush
                    // so we can detect the difference in the above test (ABA problem).
                    _currentFlush = _completedFlush;
                }
            }

            return new ValueTask<FlushResult>(currentFlush);
        }

        private async Task<FlushResult> FlushAsyncAwaited(ValueTask<FlushResult> flushTask, CancellationToken cancellationToken)
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
                            CompleteFlushUnsynchronized();
                            return flushResult;
                        }

                        CopyAndReturnSegmentsUnsynchronized();

                        flushTask = _innerPipeWriter.FlushAsync(cancellationToken);

                        if (flushResult.IsCanceled)
                        {
                            // Reset _currentFlush, so we don't enter passthrough mode while we're still flushing.
                            _currentFlush = FlushAsyncAwaited(flushTask, cancellationToken);

                            // Complete anyone currently awaiting a flush with the canceled FlushResult since CancelPendingFlush() was called.
                            return flushResult;
                        }
                    }
                }
            }
            catch
            {
                lock (_sync)
                {
                    CompleteFlushUnsynchronized();

                    // Reset the flush
                    _currentFlush = _completedFlushWithPreviousError;
                }

                // Propergate the exception in the current Task
                throw;
            }
        }

        public override void CancelPendingFlush()
        {
            // FlushAsyncAwaited never ignores a canceled FlushResult.
            _innerPipeWriter.CancelPendingFlush();
        }

        // To return all the segments without completing the inner pipe, call Abort().
        public override void Complete(Exception exception = null)
        {
            // Store the exception or sentinel in a field so that if a flush is ongoing, we call the
            // inner Complete() method with the correct exception or lack thereof once the flush loop ends.
            _completeException = exception ?? _successfullyCompletedSentinel;

            if (_currentFlush.IsCompletedSuccessfully)
            {
                if (_bytesBuffered > 0)
                {
                    CopyAndReturnSegmentsUnsynchronized();
                }

                CleanupSegmentsUnsynchronized();

                _innerPipeWriter.Complete(exception);
            }
        }

        public void Abort()
        {
            _aborted = true;

            // If we're flushing, the cleanup will happen after the flush.
            if (_currentFlush.IsCompletedSuccessfully)
            {
                CleanupSegmentsUnsynchronized();
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

        private void CompleteFlushUnsynchronized()
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
