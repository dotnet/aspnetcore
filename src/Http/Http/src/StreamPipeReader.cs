// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
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
    /// Implements PipeReader using an underlying stream.
    /// </summary>
    public class StreamPipeReader : PipeReader
    {
        private readonly int _minimumSegmentSize;
        private readonly int _minimumReadThreshold;
        private readonly Stream _readingStream;
        private readonly MemoryPool<byte> _pool;

        private CancellationTokenSource _internalTokenSource;
        private bool _isCompleted;
        private ExceptionDispatchInfo _exceptionInfo;

        private BufferSegment _readHead;
        private int _readIndex;

        private BufferSegment _readTail;
        private long _bufferedBytes;
        private bool _examinedEverything;
        private object _lock = new object();

        /// <summary>
        /// Creates a new StreamPipeReader.
        /// </summary>
        /// <param name="readingStream">The stream to read from.</param>
        public StreamPipeReader(Stream readingStream)
            : this(readingStream, StreamPipeReaderOptions.DefaultOptions)
        {
        }


        /// <summary>
        /// Creates a new StreamPipeReader.
        /// </summary>
        /// <param name="readingStream">The stream to read from.</param>
        /// <param name="options">The options to use.</param>
        public StreamPipeReader(Stream readingStream, StreamPipeReaderOptions options)
        {
            _readingStream = readingStream ?? throw new ArgumentNullException(nameof(readingStream));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.MinimumReadThreshold <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MinimumReadThreshold));
            }

            _minimumSegmentSize = options.MinimumSegmentSize;
            _minimumReadThreshold = Math.Min(options.MinimumReadThreshold, options.MinimumSegmentSize);
            _pool = options.MemoryPool;
        }

        /// <inheritdoc />
        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        private CancellationTokenSource InternalTokenSource
        {
            get
            {
                lock (_lock)
                {
                    if (_internalTokenSource == null)
                    {
                        _internalTokenSource = new CancellationTokenSource();
                    }
                    return _internalTokenSource;
                }
            }
            set
            {
                _internalTokenSource = value;
            }
        }

        /// <inheritdoc />
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            ThrowIfCompleted();

            if (_readHead == null || _readTail == null)
            {
                ThrowHelper.ThrowInvalidOperationException_NoDataRead();
            }

            AdvanceTo((BufferSegment)consumed.GetObject(), consumed.GetInteger(), (BufferSegment)examined.GetObject(), examined.GetInteger());
        }

        private void AdvanceTo(BufferSegment consumedSegment, int consumedIndex, BufferSegment examinedSegment, int examinedIndex)
        {
            if (consumedSegment == null)
            {
                return;
            }

            var returnStart = _readHead;
            var returnEnd = consumedSegment;

            var consumedBytes = new ReadOnlySequence<byte>(returnStart, _readIndex, consumedSegment, consumedIndex).Length;

            _bufferedBytes -= consumedBytes;

            Debug.Assert(_bufferedBytes >= 0);

            _examinedEverything = false;

            if (examinedSegment == _readTail)
            {
                // If we examined everything, we force ReadAsync to actually read from the underlying stream
                // instead of returning a ReadResult from TryRead.
                _examinedEverything = examinedIndex == _readTail.End - _readTail.Start;
            }

            // Three cases here:
            // 1. All data is consumed. If so, we reset _readHead and _readTail to _readTail's original memory owner
            //  SetMemory on a IMemoryOwner will reset the internal Memory<byte> to be an empty segment
            // 2. A segment is entirely consumed but there is still more data in nextSegments
            //  We are allowed to remove an extra segment. by setting returnEnd to be the next block.
            // 3. We are in the middle of a segment.
            //  Move _readHead and _readIndex to consumedSegment and index
            if (_bufferedBytes == 0)
            {
                _readTail.SetMemory(_readTail.MemoryOwner);
                _readHead = _readTail;
                returnEnd = _readTail;
                _readIndex = 0;
            }
            else if (consumedIndex == returnEnd.Length)
            {
                var nextBlock = returnEnd.NextSegment;
                _readHead = nextBlock;
                _readIndex = 0;
                returnEnd = nextBlock;
            }
            else
            {
                _readHead = consumedSegment;
                _readIndex = consumedIndex;
            }

            // Remove all blocks that are freed (except the last one)
            while (returnStart != returnEnd)
            {
                returnStart.ResetMemory();
                returnStart = returnStart.NextSegment;
            }
        }

        /// <inheritdoc />
        public override void CancelPendingRead()
        {
            InternalTokenSource.Cancel();
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

            var segment = _readHead;
            while (segment != null)
            {
                segment.ResetMemory();
                segment = segment.NextSegment;
            }
        }

        /// <inheritdoc />
        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotSupportedException("OnWriterCompleted is not supported");
        }

        /// <inheritdoc />
        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            // TODO ReadyAsync needs to throw if there are overlapping reads.
            ThrowIfCompleted();

            // PERF: store InternalTokenSource locally to avoid querying it twice (which acquires a lock)
            var tokenSource = InternalTokenSource;
            if (TryReadInternal(tokenSource, out var readResult))
            {
                return readResult;
            }

            var reg = new CancellationTokenRegistration();
            if (cancellationToken.CanBeCanceled)
            {
                reg = cancellationToken.Register(state => ((StreamPipeReader)state).Cancel(), this);
            }

            using (reg)
            {
                var isCanceled = false;
                try
                {
                    AllocateReadTail();
#if NETCOREAPP3_0
                    var length = await _readingStream.ReadAsync(_readTail.AvailableMemory.Slice(_readTail.End), tokenSource.Token);
#elif NETSTANDARD2_0
                    if (!MemoryMarshal.TryGetArray<byte>(_readTail.AvailableMemory.Slice(_readTail.End), out var arraySegment))
                    {
                        ThrowHelper.CreateInvalidOperationException_NoArrayFromMemory();
                    }

                    var length = await _readingStream.ReadAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, tokenSource.Token);
#else
#error Target frameworks need to be updated.
#endif
                    Debug.Assert(length + _readTail.End <= _readTail.AvailableMemory.Length);

                    _readTail.End += length;
                    _bufferedBytes += length;
                }
                catch (OperationCanceledException)
                {
                    ClearCancellationToken();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    isCanceled = true;
                }

                return new ReadResult(GetCurrentReadOnlySequence(), isCanceled, IsCompletedOrThrow());
            }
        }

        private void ClearCancellationToken()
        {
            lock(_lock)
            {
                _internalTokenSource = null;
            }
        }

        private void ThrowIfCompleted()
        {
            if (_isCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }
        }

        public override bool TryRead(out ReadResult result)
        {
            ThrowIfCompleted();

            return TryReadInternal(InternalTokenSource, out result);
        }

        private bool TryReadInternal(CancellationTokenSource source, out ReadResult result)
        {
            var isCancellationRequested = source.IsCancellationRequested;
            if (isCancellationRequested || _bufferedBytes > 0 && !_examinedEverything)
            {
                // If TryRead/ReadAsync are called and cancellation is requested, we need to make sure memory is allocated for the ReadResult,
                // otherwise if someone calls advance afterward on the ReadResult, it will throw.
                if (isCancellationRequested)
                {
                    AllocateReadTail();

                    ClearCancellationToken();
                }

                result = new ReadResult(
                    GetCurrentReadOnlySequence(),
                    isCanceled: isCancellationRequested,
                    IsCompletedOrThrow());
                return true;
            }

            result = new ReadResult();
            return false;
        }

        private ReadOnlySequence<byte> GetCurrentReadOnlySequence()
        {
            return new ReadOnlySequence<byte>(_readHead, _readIndex, _readTail, _readTail.End - _readTail.Start);
        }

        private void AllocateReadTail()
        {
            if (_readHead == null)
            {
                Debug.Assert(_readTail == null);
                _readHead = CreateBufferSegment();
                _readHead.SetMemory(_pool.Rent(GetSegmentSize()));
                _readTail = _readHead;
            }
            else if (_readTail.WritableBytes < _minimumReadThreshold)
            {
                CreateNewTailSegment();
            }
        }

        private void CreateNewTailSegment()
        {
            var nextSegment = CreateBufferSegment();
            nextSegment.SetMemory(_pool.Rent(GetSegmentSize()));
            _readTail.SetNext(nextSegment);
            _readTail = nextSegment;
        }

        private int GetSegmentSize() => Math.Min(_pool.MaxBufferSize, _minimumSegmentSize);

        private BufferSegment CreateBufferSegment()
        {
            // TODO this can pool buffer segment objects
            return new BufferSegment();
        }

        private void Cancel()
        {
            InternalTokenSource.Cancel();
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
    }
}
