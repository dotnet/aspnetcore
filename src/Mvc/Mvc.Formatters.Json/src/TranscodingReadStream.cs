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
    internal sealed class TranscodingReadStream : Stream
    {
        private readonly ArrayPool<char> _charPool;
        private readonly StreamReader _reader;
        private readonly int _maxCharSizeInBytes;

        public TranscodingReadStream(Stream input, Encoding sourceEncoding)
        {
            _charPool = ArrayPool<char>.Shared;

            _reader = new StreamReader(input, sourceEncoding);
            _maxCharSizeInBytes = Encoding.UTF8.GetMaxByteCount(1);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get; set; }

        public Stream BaseStream => _reader.BaseStream;

        public override void Flush()
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowArgumentOutOfRangeException(buffer, offset, count);

            if (_reader.EndOfStream)
            {
                return 0;
            }

            // Given the buffer size, determine the maximum number of characters we could read.
            var maxCharsToRead = count / _maxCharSizeInBytes;
            var charBuffer = _charPool.Rent(maxCharsToRead);
            try
            {
                var readChars = await _reader.ReadBlockAsync(charBuffer.AsMemory(0, maxCharsToRead), cancellationToken);
                if (readChars == 0)
                {
                    return 0;
                }

                return Encoding.UTF8.GetBytes(
                    charBuffer.AsSpan(0, readChars),
                    buffer.AsSpan(offset, count));
            }
            finally
            {
                _charPool.Return(charBuffer);
            }
        }

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
        {
            throw new NotSupportedException();
        }
    }
}
