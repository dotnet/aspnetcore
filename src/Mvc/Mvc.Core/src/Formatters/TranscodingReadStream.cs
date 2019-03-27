// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    internal sealed class TranscodingReadStream : Stream
    {
        internal const int MaxByteBufferSize = 4096;
        internal const int MaxCharBufferSize = 3 * MaxByteBufferSize;
        private static readonly int MaxByteCountForUTF8Char = Encoding.UTF8.GetMaxByteCount(charCount: 1);

        private readonly Stream _stream;
        private readonly Encoder _encoder;
        private readonly Decoder _decoder;

        private ArraySegment<byte> _byteBuffer;
        private ArraySegment<char> _charBuffer;
        private ArraySegment<byte> _overflowBuffer;

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
                ArrayPool<byte>.Shared.Rent(MaxByteCountForUTF8Char),
                0,
                count: 0);

            _encoder = Encoding.UTF8.GetEncoder();
            _decoder = sourceEncoding.GetDecoder();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get; set; }

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

            var totalBytes = 0;
            bool encoderCompleted;
            int bytesEncoded;

            do
            {
                // If we had left-over bytes from a previous read, move it to the start of the buffer and read content in to
                // the segment that follows.
                var eof = false;
                if (_charBuffer.Count == 0)
                {
                    // Only read more content from the input stream if we have exhausted all the buffered chars.
                    eof = await ReadInputChars(cancellationToken);
                }

                // We need to flush on the last write. This is true when we exhaust the input Stream and any buffered content.
                var allContentRead = eof && _charBuffer.Count == 0 && _byteBuffer.Count == 0;

                if (_charBuffer.Count > 0 && readBuffer.Count < MaxByteCountForUTF8Char && readBuffer.Count < Encoding.UTF8.GetByteCount(_charBuffer.AsSpan(0, 1)))
                {
                    // It's possible that the passed in buffer is smaller than the size required to encode a single
                    // char. For instance, the JsonSerializer may pass in a buffer of size 1 or 2 which
                    // is insufficient if the character requires more than 2 bytes to represent. In this case, read
                    // content in to an overflow buffer and fill up the passed in buffer.
                    _encoder.Convert(
                        _charBuffer,
                        _overflowBuffer.Array,
                        flush: false,
                        out var charsUsed,
                        out var bytesUsed,
                        out _);

                    _charBuffer = _charBuffer.Slice(charsUsed);

                    Debug.Assert(readBuffer.Count < bytesUsed);
                    _overflowBuffer.Array.AsSpan(0, readBuffer.Count).CopyTo(readBuffer);

                    _overflowBuffer = new ArraySegment<byte>(
                        _overflowBuffer.Array,
                        readBuffer.Count,
                        bytesUsed - readBuffer.Count);

                    totalBytes += readBuffer.Count;
                    // At this point we're done writing.
                    break;
                }
                else
                {
                    _encoder.Convert(
                        _charBuffer,
                        readBuffer,
                        flush: allContentRead,
                        out var charsUsed,
                        out bytesEncoded,
                        out encoderCompleted);

                    totalBytes += bytesEncoded;
                    _charBuffer = _charBuffer.Slice(charsUsed);
                    readBuffer = readBuffer.Slice(bytesEncoded);
                }

            // We need to exit in one of the 2 conditions:
            // * encoderCompleted will return false if "buffer" was too small for all the chars to be encoded.
            // * no bytes were converted in an iteration. This can occur if there wasn't any input.
            } while (encoderCompleted && bytesEncoded > 0);

            return totalBytes;
        }

        private async ValueTask<bool> ReadInputChars(CancellationToken cancellationToken)
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

            return readBytes == 0;
        }

        private static void ThrowArgumentOutOfRangeException(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
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
            ArrayPool<char>.Shared.Return(_charBuffer.Array);
            ArrayPool<byte>.Shared.Return(_byteBuffer.Array);
            ArrayPool<byte>.Shared.Return(_overflowBuffer.Array);

            base.Dispose(disposing);
        }
    }
}
