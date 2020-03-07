// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    internal sealed class TranscodingReadStream : Stream
    {
        private static readonly int OverflowBufferSize = Encoding.UTF8.GetMaxByteCount(1); // The most number of bytes used to represent a single UTF char

        internal const int MaxByteBufferSize = 4096;
        internal const int MaxCharBufferSize = 3 * MaxByteBufferSize;

        private readonly Stream _stream;
        private readonly Decoder _decoder;

        private ArraySegment<byte> _byteBuffer;
        private ArraySegment<char> _charBuffer;
        private ArraySegment<byte> _overflowBuffer;
        private bool _disposed;

        public TranscodingReadStream(Stream input, Encoding sourceEncoding)
        {
            _stream = input;

            // The "count" in the buffer is the size of any content from a previous read.
            // Initialize them to 0 since nothing has been read so far.
            _byteBuffer = new ArraySegment<byte>(
                ArrayPool<byte>.Shared.Rent(MaxByteBufferSize),
                0,
                count: 0);

            // Attempt to allocate a char buffer than can tolerate the worst-case scenario for this 
            // encoding. This would allow the byte -> char conversion to complete in a single call.
            // However limit the buffer size to prevent an encoding that has a very poor worst-case scenario. 
            // The conversion process is tolerant of char buffer that is not large enough to convert all the bytes at once.
            var maxCharBufferSize = Math.Min(MaxCharBufferSize, sourceEncoding.GetMaxCharCount(MaxByteBufferSize));
            _charBuffer = new ArraySegment<char>(
                ArrayPool<char>.Shared.Rent(maxCharBufferSize),
                0,
                count: 0);

            _overflowBuffer = new ArraySegment<byte>(
                ArrayPool<byte>.Shared.Rent(OverflowBufferSize),
                0,
                count: 0);

            _decoder = sourceEncoding.GetDecoder();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        internal int ByteBufferCount => _byteBuffer.Count;
        internal int CharBufferCount => _charBuffer.Count;
        internal int OverflowCount => _overflowBuffer.Count;

        public override void Flush()
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowArgumentOutOfRangeException(buffer, offset, count);

            if (count == 0)
            {
                return 0;
            }

            var readBuffer = new ArraySegment<byte>(buffer, offset, count);

            if (_overflowBuffer.Count > 0)
            {
                var bytesToCopy = Math.Min(count, _overflowBuffer.Count);
                _overflowBuffer.Slice(0, bytesToCopy).CopyTo(readBuffer);

                _overflowBuffer = _overflowBuffer.Slice(bytesToCopy);

                // If we have any overflow bytes, avoid complicating the remainder of the code, by returning as
                // soon as we copy any content.
                return bytesToCopy;
            }

            if (_charBuffer.Count == 0)
            {
                // Only read more content from the input stream if we have exhausted all the buffered chars.
                await ReadInputChars(cancellationToken);
            }

            var operationStatus = Utf8.FromUtf16(_charBuffer, readBuffer, out var charsRead, out var bytesWritten, isFinalBlock: false);
            _charBuffer = _charBuffer.Slice(charsRead);

            switch (operationStatus)
            {
                case OperationStatus.Done:
                    return bytesWritten;

                case OperationStatus.DestinationTooSmall:
                    if (bytesWritten != 0)
                    {
                        return bytesWritten;
                    }

                    // Overflow buffer is always empty when we get here and we can use it's full length to write contents to.
                    Utf8.FromUtf16(_charBuffer, _overflowBuffer.Array, out var overFlowChars, out var overflowBytes, isFinalBlock: false);

                    Debug.Assert(overflowBytes > 0 && overFlowChars > 0, "We expect writes to the overflow buffer to always succeed since it is large enough to accommodate at least one char.");

                    _charBuffer = _charBuffer.Slice(overFlowChars);

                    // readBuffer: [ 0, 0, ], overflowBuffer: [ 7, 13, 34, ]
                    // Fill up the readBuffer to capacity, so the result looks like so:
                    // readBuffer: [ 7, 13 ], overflowBuffer: [ 34 ]
                    Debug.Assert(readBuffer.Count < overflowBytes);
                    _overflowBuffer.Array.AsSpan(0, readBuffer.Count).CopyTo(readBuffer);

                    _overflowBuffer = new ArraySegment<byte>(
                        _overflowBuffer.Array,
                        readBuffer.Count,
                        overflowBytes - readBuffer.Count);

                    Debug.Assert(_overflowBuffer.Count != 0);

                    return readBuffer.Count;

                default:
                    Debug.Fail("We should never see this");
                    throw new InvalidOperationException();
            }
        }

        private async Task ReadInputChars(CancellationToken cancellationToken)
        {
            // If we had left-over bytes from a previous read, move it to the start of the buffer and read content in to
            // the segment that follows.
            Buffer.BlockCopy(
                _byteBuffer.Array,
                _byteBuffer.Offset,
                _byteBuffer.Array,
                0,
                _byteBuffer.Count);

            var readBytes = await _stream.ReadAsync(_byteBuffer.Array.AsMemory(_byteBuffer.Count), cancellationToken);
            _byteBuffer = new ArraySegment<byte>(_byteBuffer.Array, 0, _byteBuffer.Count + readBytes);

            Debug.Assert(_charBuffer.Count == 0, "We should only expect to read more input chars once all buffered content is read");

            _decoder.Convert(
                _byteBuffer.AsSpan(),
                _charBuffer.Array,
                flush: readBytes == 0,
                out var bytesUsed,
                out var charsUsed,
                out _);

            _byteBuffer = _byteBuffer.Slice(bytesUsed);
            _charBuffer = new ArraySegment<char>(_charBuffer.Array, 0, charsUsed);
        }

        private static void ThrowArgumentOutOfRangeException(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                ArrayPool<char>.Shared.Return(_charBuffer.Array);
                ArrayPool<byte>.Shared.Return(_byteBuffer.Array);
                ArrayPool<byte>.Shared.Return(_overflowBuffer.Array);
            }
        }
    }
}
