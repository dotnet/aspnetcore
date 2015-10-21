// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Extensions.MemoryPool;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    public class HttpRequestStreamReader : TextReader
    {
        private const int DefaultBufferSize = 1024;
        private const int MinBufferSize = 128;
        private const int MaxSharedBuilderCapacity = 360; // also the max capacity used in StringBuilderCache

        private Stream _stream;
        private readonly Encoding _encoding;
        private readonly Decoder _decoder;

        private readonly LeasedArraySegment<byte> _leasedByteBuffer;
        private readonly LeasedArraySegment<char> _leasedCharBuffer;

        private readonly int _byteBufferSize;
        private readonly ArraySegment<byte> _byteBuffer;
        private readonly ArraySegment<char> _charBuffer;

        private int _charBufferIndex;
        private int _charsRead;
        private int _bytesRead;

        private bool _isBlocked;

        public HttpRequestStreamReader(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize)
        {
        }

        public HttpRequestStreamReader(Stream stream, Encoding encoding, int bufferSize)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(Resources.HttpRequestStreamReader_StreamNotReadable, nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            _stream = stream;
            _encoding = encoding;
            _decoder = encoding.GetDecoder();

            if (bufferSize < MinBufferSize)
            {
                bufferSize = MinBufferSize;
            }

            _byteBufferSize = bufferSize;
            _byteBuffer = new ArraySegment<byte>(new byte[bufferSize]);
            var maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = new ArraySegment<char>(new char[maxCharsPerBuffer]);
        }

        public HttpRequestStreamReader(
            Stream stream,
            Encoding encoding,
            int bufferSize,
            LeasedArraySegment<byte> leasedByteBuffer,
            LeasedArraySegment<char> leasedCharBuffer)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(Resources.HttpRequestStreamReader_StreamNotReadable, nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (leasedByteBuffer == null)
            {
                throw new ArgumentNullException(nameof(leasedByteBuffer));
            }

            if (leasedCharBuffer == null)
            {
                throw new ArgumentNullException(nameof(leasedCharBuffer));
            }

            if (bufferSize <= 0 || bufferSize > leasedByteBuffer.Data.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            var requiredLength = encoding.GetMaxCharCount(bufferSize);
            if (requiredLength > leasedCharBuffer.Data.Count)
            {
                var message = Resources.FormatHttpRequestStreamReader_InvalidBufferSize(
                    requiredLength,
                    bufferSize,
                    encoding.EncodingName,
                    typeof(Encoding).FullName,
                    nameof(Encoding.GetMaxCharCount));
                throw new ArgumentException(message, nameof(leasedCharBuffer));
            }

            _stream = stream;
            _encoding = encoding;
            _byteBufferSize = bufferSize;
            _leasedByteBuffer = leasedByteBuffer;
            _leasedCharBuffer = leasedCharBuffer;

            _decoder = encoding.GetDecoder();
            _byteBuffer = _leasedByteBuffer.Data;
            _charBuffer = _leasedCharBuffer.Data;
        }

#if dnx451 
        public override void Close()
        {
            Dispose(true);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing && _stream != null)
            {
                _stream = null;

                if (_leasedByteBuffer != null)
                {
                    _leasedByteBuffer.Owner.Return(_leasedByteBuffer);
                }

                if (_leasedCharBuffer != null)
                {
                    _leasedCharBuffer.Owner.Return(_leasedCharBuffer);
                }
            }

            base.Dispose(disposing);
        }

        public override int Peek()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (_charBufferIndex == _charsRead)
            {
                if (_isBlocked || ReadIntoBuffer() == 0)
                {
                    return -1;
                }
            }

            return _charBuffer.Array[_charBuffer.Offset + _charBufferIndex];
        }

        public override int Read()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (_charBufferIndex == _charsRead)
            {
                if (ReadIntoBuffer() == 0)
                {
                    return -1;
                }
            }

            return _charBuffer.Array[_charBuffer.Offset + _charBufferIndex++];
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0 || index + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            var charsRead = 0;
            while (count > 0)
            {
                var charsRemaining = _charsRead - _charBufferIndex;
                if (charsRemaining == 0)
                {
                    charsRemaining = ReadIntoBuffer();
                }

                if (charsRemaining == 0)
                {
                    break;  // We're at EOF
                }

                if (charsRemaining > count)
                {
                    charsRemaining = count;
                }

                Buffer.BlockCopy(
                    _charBuffer.Array,
                    (_charBuffer.Offset + _charBufferIndex) * 2,
                    buffer,
                    (index + charsRead) * 2,
                    charsRemaining * 2);
                _charBufferIndex += charsRemaining;

                charsRead += charsRemaining;
                count -= charsRemaining;

                // If we got back fewer chars than we asked for, then it's likely the underlying stream is blocked.
                // Send the data back to the caller so they can process it.
                if (_isBlocked)
                {
                    break;
                }
            }

            return charsRead;
        }

        public override async Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0 || index + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (_charBufferIndex == _charsRead && await ReadIntoBufferAsync() == 0)
            {
                return 0;
            }

            var charsRead = 0;
            while (count > 0)
            {
                // n is the characters available in _charBuffer
                var n = _charsRead - _charBufferIndex;

                // charBuffer is empty, let's read from the stream
                if (n == 0)
                {
                    _charsRead = 0;
                    _charBufferIndex = 0;
                    _bytesRead = 0;

                    // We loop here so that we read in enough bytes to yield at least 1 char.
                    // We break out of the loop if the stream is blocked (EOF is reached).
                    do
                    {
                        Debug.Assert(n == 0);
                        _bytesRead = await _stream.ReadAsync(
                            _byteBuffer.Array,
                            _byteBuffer.Offset,
                            _byteBufferSize);
                        if (_bytesRead == 0)  // EOF
                        {
                            _isBlocked = true;
                            break;
                        }

                        // _isBlocked == whether we read fewer bytes than we asked for.
                        _isBlocked = (_bytesRead < _byteBufferSize);

                        Debug.Assert(n == 0);

                        _charBufferIndex = 0;
                        n = _decoder.GetChars(
                            _byteBuffer.Array,
                            _byteBuffer.Offset,
                            _bytesRead,
                            _charBuffer.Array,
                            _charBuffer.Offset);
                        
                        Debug.Assert(n > 0);

                        _charsRead += n; // Number of chars in StreamReader's buffer.
                    }
                    while (n == 0);

                    if (n == 0)
                    {
                        break; // We're at EOF
                    }
                }

                // Got more chars in charBuffer than the user requested
                if (n > count)
                {
                    n = count;
                }

                Buffer.BlockCopy(
                    _charBuffer.Array,
                    (_charBuffer.Offset + _charBufferIndex) * 2,
                    buffer,
                    (index + charsRead) * 2,
                    n * 2);

                _charBufferIndex += n;

                charsRead += n;
                count -= n;

                // This function shouldn't block for an indefinite amount of time,
                // or reading from a network stream won't work right.  If we got
                // fewer bytes than we requested, then we want to break right here.
                if (_isBlocked)
                {
                    break;
                }
            }

            return charsRead;
        }

        private int ReadIntoBuffer()
        {
            _charsRead = 0;
            _charBufferIndex = 0;
            _bytesRead = 0;

            do
            {
                _bytesRead = _stream.Read(_byteBuffer.Array, _byteBuffer.Offset, _byteBufferSize);
                if (_bytesRead == 0)  // We're at EOF
                {
                    return _charsRead;
                }

                _isBlocked = (_bytesRead < _byteBufferSize);
                _charsRead += _decoder.GetChars(
                    _byteBuffer.Array,
                    _byteBuffer.Offset,
                    _bytesRead,
                    _charBuffer.Array,
                    _charBuffer.Offset + _charsRead);
            }
            while (_charsRead == 0);

            return _charsRead;
        }

        private async Task<int> ReadIntoBufferAsync()
        {
            _charsRead = 0;
            _charBufferIndex = 0;
            _bytesRead = 0;

            do
            {

                _bytesRead = await _stream.ReadAsync(
                    _byteBuffer.Array,
                    _byteBuffer.Offset,
                    _byteBufferSize).ConfigureAwait(false);
                if (_bytesRead == 0)
                {
                    // We're at EOF
                    return _charsRead;
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                _isBlocked = (_bytesRead < _byteBufferSize);

                _charsRead += _decoder.GetChars(
                    _byteBuffer.Array,
                    _byteBuffer.Offset,
                    _bytesRead,
                    _charBuffer.Array,
                    _charBuffer.Offset + _charsRead);
            }
            while (_charsRead == 0);

            return _charsRead;
        }
    }
}