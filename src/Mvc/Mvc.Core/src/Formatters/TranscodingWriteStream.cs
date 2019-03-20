// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    internal sealed class TranscodingWriteStream : Stream
    {
        internal const int MaxCharBufferSize = 4096;
        internal const int MaxByteBufferSize = 4 * MaxCharBufferSize;
        private readonly int _maxByteBufferSize;

        private readonly Stream _stream;
        private readonly Decoder _decoder;
        private readonly Encoder _encoder;
        private readonly char[] _charBuffer;
        private int _charsDecoded;

        public TranscodingWriteStream(Stream stream, Encoding targetEncoding)
        {
            _stream = stream;

            _charBuffer = ArrayPool<char>.Shared.Rent(MaxCharBufferSize);

            // Attempt to allocate a byte buffer than can tolerate the worst-case scenario for this 
            // encoding. This would allow the char -> byte conversion to complete in a single call.
            // However limit the buffer size to prevent an encoding that has a very poor worst-case scenario. 
            _maxByteBufferSize = Math.Min(MaxByteBufferSize, targetEncoding.GetMaxByteCount(MaxCharBufferSize));

            _decoder = Encoding.UTF8.GetDecoder();
            _encoder = targetEncoding.GetEncoder();
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get; set; }

        public override void Flush()
            => throw new NotSupportedException();

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await WriteAsync(ArraySegment<byte>.Empty, flush: true, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowArgumentException(buffer, offset, count);
            var bufferSegment = new ArraySegment<byte>(buffer, offset, count);
            return WriteAsync(bufferSegment, flush: false, cancellationToken);
        }

        private async Task WriteAsync(
            ArraySegment<byte> bufferSegment,
            bool flush,
            CancellationToken cancellationToken)
        {
            var decoderCompleted = false;
            while (!decoderCompleted)
            {
                _decoder.Convert(
                    bufferSegment,
                    _charBuffer.AsSpan(_charsDecoded),
                    flush,
                    out var bytesDecoded,
                    out var charsDecoded,
                    out decoderCompleted);

                _charsDecoded += charsDecoded;
                bufferSegment = bufferSegment.Slice(bytesDecoded);

                if (flush || !decoderCompleted)
                {
                    // This is being invoked from FlushAsync or the char buffer is not large enough
                    // to accomodate all writes.
                    await WriteBufferAsync(flush, cancellationToken);
                }
            }
        }

        private async Task WriteBufferAsync(bool flush, CancellationToken cancellationToken)
        {
            var encoderCompletd = false;
            var charsWritten = 0;
            var byteBuffer = ArrayPool<byte>.Shared.Rent(_maxByteBufferSize);

            try
            {
                while (!encoderCompletd && charsWritten < _charsDecoded)
                {
                    _encoder.Convert(
                        _charBuffer.AsSpan(charsWritten, _charsDecoded - charsWritten),
                        byteBuffer,
                        flush,
                        out var charsEncoded,
                        out var bytesUsed,
                        out encoderCompletd);

                    await _stream.WriteAsync(byteBuffer.AsMemory(0, bytesUsed), cancellationToken);
                    charsWritten += charsEncoded;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }

            // At this point, we've written all the buffered chars to the underlying Stream.
            _charsDecoded = 0;
        }

        private static void ThrowArgumentException(byte[] buffer, int offset, int count)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ArrayPool<char>.Shared.Return(_charBuffer);
            }


            base.Dispose(disposing);
        }
    }
}
