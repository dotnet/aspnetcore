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
        private readonly Stream _stream;
        private readonly ArrayPool<char> _charPool;
        private readonly Encoding _targetEncoding;

        public TranscodingWriteStream(Stream stream, Encoding targetEncoding)
        {
            _stream = stream;
            _charPool = ArrayPool<char>.Shared;
            _targetEncoding = targetEncoding;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get; set; }

        public override void Flush()
            => throw new NotSupportedException();

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        private void ThrowArgumentOutOfRangeException(byte[] buffer, int offset, int count)
        {
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
            => throw new NotSupportedException();

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowArgumentOutOfRangeException(buffer, offset, count);

            var maxChars = Encoding.UTF8.GetMaxCharCount(count);
            var charBuffer = _charPool.Rent(maxChars);

            try
            {
                var writtenChars = Encoding.UTF8.GetChars(buffer.AsSpan(offset, count), charBuffer);
                var encoder = _targetEncoding.GetEncoder();

                var charOffset = 0;
                var completed = false;
                var bytesUsed = 0;
                var usableBufferLength = buffer.Length - offset;
                while (!completed)
                {
                    encoder.Convert(
                        charBuffer.AsSpan(charOffset, writtenChars),
                        buffer.AsSpan(offset, usableBufferLength),
                        flush: writtenChars == 0,
                        out var charsUsed,
                        out bytesUsed,
                        out completed);

                    await _stream.WriteAsync(buffer.AsMemory(offset, bytesUsed), cancellationToken);

                    charOffset += charsUsed;
                    writtenChars -= charsUsed;
                }

                encoder.Convert(ReadOnlySpan<char>.Empty, buffer.AsSpan(offset, usableBufferLength), flush: true, out _, out bytesUsed, out _);
                await _stream.WriteAsync(buffer.AsMemory(offset, bytesUsed), cancellationToken);
            }
            finally
            {
                _charPool.Return(charBuffer);
            }
        }
    }
}
