// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Implements PipeWriter using a underlying stream. 
    /// </summary>
    public class StreamPipeWriter : PipeWriter, IDisposable
    {
        private readonly int _minimumSegmentSize;
        private readonly Stream _writingStream;
        private int _bytesWritten;

        private List<CompletedBuffer> _completedSegments;
        private Memory<byte> _currentSegment;
        private IMemoryOwner<byte> _currentSegmentOwner;
        private MemoryPool<byte> _pool;
        private int _position;

        private CancellationTokenSource _internalTokenSource;
        private bool _isCompleted;
        private ExceptionDispatchInfo _exceptionInfo;
        private object _lockObject = new object();

        private CancellationTokenSource InternalTokenSource
        {
            get
            {
                lock (_lockObject)
                {
                    if (_internalTokenSource == null)
                    {
                        _internalTokenSource = new CancellationTokenSource();
                    }
                    return _internalTokenSource;
                }
            }
        }

        /// <summary>
        /// Creates a new StreamPipeWrapper 
        /// </summary>
        /// <param name="writingStream">The stream to write to</param>
        public StreamPipeWriter(Stream writingStream) : this(writingStream, 4096)
        {
        }

        public StreamPipeWriter(Stream writingStream, int minimumSegmentSize, MemoryPool<byte> pool = null)
        {
            _minimumSegmentSize = minimumSegmentSize;
            _writingStream = writingStream;
            _pool = pool ?? MemoryPool<byte>.Shared;
        }

        /// <inheritdoc />
        public override void Advance(int count)
        {
            if (_currentSegment.IsEmpty) // TODO confirm this
            {
                throw new InvalidOperationException("No writing operation. Make sure GetMemory() was called.");
            }

            if (count >= 0)
            {
                if (_currentSegment.Length < _position + count)
                {
                    throw new InvalidOperationException("Can't advance past buffer size.");
                }
                _bytesWritten += count;
                _position += count;
            }
        }

        /// <inheritdoc />
        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment;
        }

        /// <inheritdoc />
        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment.Span.Slice(_position);
        }

        /// <inheritdoc />
        public override void CancelPendingFlush()
        {
            Cancel();
        }

        /// <inheritdoc />
        public override void Complete(Exception exception = null)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            if (exception != null)
            {
                _exceptionInfo = ExceptionDispatchInfo.Capture(exception);
            }

            _internalTokenSource?.Dispose();

            if (_completedSegments != null)
            {
                foreach (var segment in _completedSegments)
                {
                    segment.Return();
                }
            }

            _currentSegmentOwner?.Dispose();
        }

        /// <inheritdoc />
        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotSupportedException("OnReaderCompleted isn't supported in StreamPipeWrapper.");
        }

        /// <inheritdoc />
        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_bytesWritten == 0)
            {
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, IsCompletedOrThrow()));
            }

            return FlushAsyncInternal(cancellationToken);
        }

        private void Cancel()
        {
            InternalTokenSource.Cancel();
        }

        private async ValueTask<FlushResult> FlushAsyncInternal(CancellationToken cancellationToken = default)
        {
            // Write all completed segments and whatever remains in the current segment
            // and flush the result.
            CancellationTokenRegistration reg = new CancellationTokenRegistration();
            if (cancellationToken.CanBeCanceled)
            {
                reg = cancellationToken.Register(state => ((StreamPipeWriter)state).Cancel(), this);
            }
            using (reg)
            {
                var localToken = InternalTokenSource.Token;
                try
                {
                    if (_completedSegments != null && _completedSegments.Count > 0)
                    {
                        var count = _completedSegments.Count;
                        for (var i = 0; i < count; i++)
                        {
                            var segment = _completedSegments[0];
#if NETCOREAPP3_0
                            await _writingStream.WriteAsync(segment.Buffer.Slice(0, segment.Length), localToken);
#elif NETSTANDARD2_0
                            MemoryMarshal.TryGetArray<byte>(segment.Buffer, out var arraySegment);
                            await _writingStream.WriteAsync(arraySegment.Array, 0, segment.Length, localToken);
#else
#error Target frameworks need to be updated.
#endif
                            _bytesWritten -= segment.Length;
                            segment.Return();
                            _completedSegments.RemoveAt(0);
                        }
                    }

                    if (!_currentSegment.IsEmpty)
                    {
#if NETCOREAPP3_0
                        await _writingStream.WriteAsync(_currentSegment.Slice(0, _position), localToken);
#elif NETSTANDARD2_0
                        MemoryMarshal.TryGetArray<byte>(_currentSegment, out var arraySegment);
                        await _writingStream.WriteAsync(arraySegment.Array, 0, _position, localToken);
#else
#error Target frameworks need to be updated.
#endif
                        _bytesWritten -= _position;
                        _position = 0;
                    }

                    await _writingStream.FlushAsync(localToken);

                    return new FlushResult(isCanceled: false, IsCompletedOrThrow());
                }
                catch (OperationCanceledException)
                {
                    // Remove the cancellation token such that the next time Flush is called
                    // A new CTS is created.
                    lock (_lockObject)
                    {
                        _internalTokenSource = null;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    // Catch any cancellation and translate it into setting isCanceled = true
                    return new FlushResult(isCanceled: true, IsCompletedOrThrow());
                }
            }
        }

        private void EnsureCapacity(int sizeHint)
        {
            // This does the Right Thing. It only subtracts _position from the current segment length if it's non-null.
            // If _currentSegment is null, it returns 0.
            var remainingSize = _currentSegment.Length - _position;

            // If the sizeHint is 0, any capacity will do
            // Otherwise, the buffer must have enough space for the entire size hint, or we need to add a segment.
            if ((sizeHint == 0 && remainingSize > 0) || (sizeHint > 0 && remainingSize >= sizeHint))
            {
                // We have capacity in the current segment
                return;
            }

            AddSegment(sizeHint);
        }

        private void AddSegment(int sizeHint = 0)
        {
            if (_currentSegment.Length != 0)
            {
                // We're adding a segment to the list
                if (_completedSegments == null)
                {
                    _completedSegments = new List<CompletedBuffer>();
                }

                // Position might be less than the segment length if there wasn't enough space to satisfy the sizeHint when
                // GetMemory was called. In that case we'll take the current segment and call it "completed", but need to
                // ignore any empty space in it.
                _completedSegments.Add(new CompletedBuffer(_currentSegmentOwner, _position));
            }

            // Get a new buffer using the minimum segment size, unless the size hint is larger than a single segment.
            _currentSegmentOwner = _pool.Rent(Math.Max(_minimumSegmentSize, sizeHint));
            _currentSegment = _currentSegmentOwner.Memory;
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCompletedOrThrow()
        {
            if (!_isCompleted)
            {
                return false;
            }

            if (_exceptionInfo != null)
            {
                ThrowLatchedException();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowLatchedException()
        {
            _exceptionInfo.Throw();
        }

        public void Dispose()
        {
            Complete();
        }

        /// <summary>
        /// Holds a byte[] from the pool and a size value. Basically a Memory but guaranteed to be backed by an ArrayPool byte[], so that we know we can return it.
        /// </summary>
        private readonly struct CompletedBuffer
        {
            public Memory<byte> Buffer { get; }
            public int Length { get; }

            public ReadOnlySpan<byte> Span => Buffer.Span;

            private readonly IMemoryOwner<byte> _memoryOwner;

            public CompletedBuffer(IMemoryOwner<byte> buffer, int length)
            {
                Buffer = buffer.Memory;
                Length = length;
                _memoryOwner = buffer;
            }

            public void Return()
            {
                _memoryOwner.Dispose();
            }
        }
    }
}
