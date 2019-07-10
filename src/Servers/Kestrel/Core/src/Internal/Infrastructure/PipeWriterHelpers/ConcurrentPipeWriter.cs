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

        private static readonly Exception _successfulCompletionSentinal = new Exception();

        private readonly object _sync = new object();
        private readonly PipeWriter _innerPipeWriter;
        private readonly MemoryPool<byte> _pool;
        private readonly BufferSegmentStack _bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);

        private BufferSegment _head;
        private BufferSegment _tail;
        private Memory<byte> _tailMemory;
        private int _tailBytesBuffered;
        private int _bytesBuffered;
        private bool _nonPassThroughAdvancePending;
        private Exception _completeEx;


        private TaskCompletionSource<FlushResult> _currentFlushTcs;

        public ConcurrentPipeWriter(PipeWriter innerPipeWriter, MemoryPool<byte> pool)
        {
            _innerPipeWriter = innerPipeWriter;
            _pool = pool;
        }

        public override void Advance(int bytes)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null)
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
                _nonPassThroughAdvancePending = false;
            }
        }

        // This is not exposed to end users. Throw so we find out if we ever start calling this.
        public override void CancelPendingFlush()
        {
            // If we wanted, we could propogate IsCanceled when we do multiple flushes in a loop.
            // If FlushResult.IsCanceled is true with more data pending to flush, we could complete _currentFlushTcs with canceled flush task,
            // but rekick the FlushAsync loop.
            throw new NotImplementedException();
        }

        public override void Complete(Exception exception = null)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null)
                {
                    _innerPipeWriter.Complete(exception);
                    return;
                }

                _completeEx = exception ?? _successfulCompletionSentinal;
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

                var flushTask = _innerPipeWriter.FlushAsync(cancellationToken);

                if (flushTask.IsCompletedSuccessfully)
                {
                    return flushTask;
                }

                // Use a TCS instead of something resettable so it can be awaited by multiple awaiters.
                _currentFlushTcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                return FlushAsyncAwaited(flushTask, cancellationToken);
            }
        }

        private async ValueTask<FlushResult> FlushAsyncAwaited(ValueTask<FlushResult> flushTask, CancellationToken cancellationToken)
        {
            try
            {
                var flushResult = await flushTask;

                // This while (true) does look scary, but the real continuation condition is at the start of the loop
                // so the _sync lock can be acquired.
                while (true)
                {
                    lock (_sync)
                    {
                        if (_bytesBuffered == 0)
                        {
                            CompleteInnerPipeIfNecessaryUnsynchronized();
                            _currentFlushTcs.SetResult(flushResult);
                            _currentFlushTcs = null;
                            return flushResult;
                        }

                        CopyAndReturnSegmentsUnsynchronized();

                        flushTask = _innerPipeWriter.FlushAsync(cancellationToken);
                    }

                    flushResult = await flushTask;
                }
            }
            catch (Exception ex)
            {
                lock (_sync)
                {
                    CompleteInnerPipeIfNecessaryUnsynchronized();
                    _currentFlushTcs.SetException(ex);
                    _currentFlushTcs = null;

                    // Ensure the exception still gets observed by the original caller who started the flush loop
                    // And is observing this method directly instead of through the TCS.
                    throw;
                }
            }
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null)
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
                if (_currentFlushTcs == null)
                {
                    return _innerPipeWriter.GetSpan(sizeHint);
                }

                AllocateMemoryUnsynchronized(sizeHint);
                return _tailMemory.Span;
            }
        }

        // This is not exposed to end users. Throw so we find out if we ever start calling this.
        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotImplementedException();
        }

        private void CopyAndReturnSegmentsUnsynchronized()
        {
            var segment = _head;
            var returnSegment = default(BufferSegment);

            while (segment != null)
            {
                if (returnSegment != null)
                {
                    returnSegment.ResetMemory();
                    ReturnSegmentUnsynchronized(returnSegment);
                }

                var span = _innerPipeWriter.GetSpan(segment.Length);
                segment.Memory.Span.CopyTo(span);
                _innerPipeWriter.Advance(segment.Length);

                returnSegment = segment;
                segment = segment.NextSegment;
            }

            if (_nonPassThroughAdvancePending)
            {
                // If an advance is pending, so is a flush, so the _tail segment should still get returned eventually.
                _head = _tail;
            }
            else
            {
                returnSegment.ResetMemory();
                ReturnSegmentUnsynchronized(returnSegment);
                _head = _tail = null;
            }

            // Even if a non-passthrough advance is pending, there a 0 bytes currently buffered.
            _bytesBuffered = 0;
        }

        public void CompleteInnerPipeIfNecessaryUnsynchronized()
        {
            if (_completeEx != null)
            {
                if (ReferenceEquals(_completeEx, _successfulCompletionSentinal))
                {
                    _innerPipeWriter.Complete();
                }
                else
                {
                    _innerPipeWriter.Complete(_completeEx);
                }
            }
        }

        // The methods below were copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.IO.Pipelines/src/System/IO/Pipelines/StreamPipeWriter.cs
        private void AllocateMemoryUnsynchronized(int sizeHint)
        {
            _nonPassThroughAdvancePending = true;

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

            if (_pool is null)
            {
                // Use the array pool
                newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(GetSegmentSize(sizeHint)));
            }
            else if (sizeHint <= _pool.MaxBufferSize)
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
