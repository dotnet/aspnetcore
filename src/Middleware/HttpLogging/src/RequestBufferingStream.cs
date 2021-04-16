// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal sealed class RequestBufferingStream : Stream, IBufferWriter<byte>
    {
        private const int MinimumBufferSize = 4096; // 4K
        private Stream _innerStream;
        private Encoding? _encoding;
        private readonly int _limit;
        private readonly ILogger _logger;
        private int _bytesWritten;

        private BufferSegment? _head;
        private BufferSegment? _tail;
        private Memory<byte> _tailMemory; // remainder of tail memory
        private int _tailBytesBuffered;

        public RequestBufferingStream(Stream innerStream, int limit, ILogger logger, Encoding? encoding)
        {
            _limit = limit;
            _logger = logger;
            _innerStream = innerStream;
            _encoding = encoding;
        }

        public override bool CanSeek => false;

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int WriteTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            var res = await _innerStream.ReadAsync(destination, cancellationToken);

            WriteToBuffer(destination.Slice(0, res).Span, res);

            return res;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var res = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

            WriteToBuffer(buffer.AsSpan(offset, res), res);

            return res;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var res = _innerStream.Read(buffer, offset, count);

            WriteToBuffer(buffer.AsSpan(offset, res), res);

            return res;
        }

        private void WriteToBuffer(ReadOnlySpan<byte> span, int res)
        {
            // get what was read into the buffer
            var remaining = _limit - _bytesWritten;

            if (remaining == 0)
            {
                return;
            }

            if (res == 0)
            {
                // Done reading, log the string.
                LogString();
            }

            var innerCount = Math.Min(remaining, span.Length);

            if (span.Slice(0, innerCount).TryCopyTo(_tailMemory.Span))
            {
                _tailBytesBuffered += innerCount;
                _bytesWritten += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(this, span.Slice(0, innerCount));
            }

            if (_limit - _bytesWritten == 0)
            {
                LogString();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        /// <inheritdoc />
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var remaining = _limit - _bytesWritten;

            while (remaining > 0)
            {
                // reusing inner buffer here between streams
                var memory = GetMemory();
                var innerCount = Math.Min(remaining, memory.Length);

                var res = await _innerStream.ReadAsync(memory.Slice(0, innerCount), cancellationToken);

                _tailBytesBuffered += res;
                _bytesWritten += res;
                _tailMemory = _tailMemory.Slice(res);

                await destination.WriteAsync(memory.Slice(0, res));

                remaining -= res;
            }

            await _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public void LogString()
        {
            if (_head == null || _tail == null || _encoding == null)
            {
                return;
            }

            // Only place where we are actually using the buffered data.
            // update tail here.
            _tail.End = _tailBytesBuffered;

            var ros = new ReadOnlySequence<byte>(_head, 0, _tail, _tailBytesBuffered);

            // If encoding is nullable
            var body = _encoding.GetString(ros);
            _logger.LogInformation(LoggerEventIds.RequestBody, CoreStrings.RequestBody, body);

            Reset();
        }

        public void Advance(int bytes)
        {
            if ((uint)bytes > (uint)_tailMemory.Length)
            {
                ThrowArgumentOutOfRangeException(nameof(bytes));
            }

            _tailBytesBuffered += bytes;
            _bytesWritten += bytes;
            _tailMemory = _tailMemory.Slice(bytes);
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            AllocateMemoryUnsynchronized(sizeHint);
            return _tailMemory;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            AllocateMemoryUnsynchronized(sizeHint);
            return _tailMemory.Span;
        }
        private void AllocateMemoryUnsynchronized(int sizeHint)
        {
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

        private BufferSegment AllocateSegmentUnsynchronized(int sizeHint)
        {
            BufferSegment newSegment = CreateSegmentUnsynchronized();

            // We can't use the recommended pool so use the ArrayPool
            newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(GetSegmentSize(sizeHint)));

            _tailMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            return new BufferSegment();
        }

        private static int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(MinimumBufferSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
            return adjustedToMaximumSize;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reset();
            }
        }

        public void Reset()
        {
            var segment = _head;
            while (segment != null)
            {
                var returnSegment = segment;
                segment = segment.NextSegment;

                // We haven't reached the tail of the linked list yet, so we can always return the returnSegment.
                returnSegment.ResetMemory();
            }

            _bytesWritten = 0;
            _tailBytesBuffered = 0;
        }

        // Copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.Memory/src/System/ThrowHelper.cs
        private static void ThrowArgumentOutOfRangeException(string argumentName) { throw CreateArgumentOutOfRangeException(argumentName); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException(string argumentName) { return new ArgumentOutOfRangeException(argumentName); }
    }
}
