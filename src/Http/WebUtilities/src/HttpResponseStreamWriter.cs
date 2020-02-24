// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Writes to the <see cref="Stream"/> using the supplied <see cref="Encoding"/>.
    /// It does not write the BOM and also does not close the stream.
    /// </summary>
    public class HttpResponseStreamWriter : TextWriter
    {
        private const int MinBufferSize = 128;
        internal const int DefaultBufferSize = 16 * 1024;

        private Stream _stream;
        private readonly Encoder _encoder;
        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<char> _charPool;
        private readonly int _charBufferSize;

        private byte[] _byteBuffer;
        private char[] _charBuffer;

        private int _charBufferCount;
        private bool _disposed;

        public HttpResponseStreamWriter(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public HttpResponseStreamWriter(Stream stream, Encoding encoding, int bufferSize)
            : this(stream, encoding, bufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public HttpResponseStreamWriter(
            Stream stream,
            Encoding encoding,
            int bufferSize,
            ArrayPool<byte> bytePool,
            ArrayPool<char> charPool)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _bytePool = bytePool ?? throw new ArgumentNullException(nameof(bytePool));
            _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }
            if (!_stream.CanWrite)
            {
                throw new ArgumentException(Resources.HttpResponseStreamWriter_StreamNotWritable, nameof(stream));
            }

            _charBufferSize = bufferSize;

            _encoder = encoding.GetEncoder();
            _charBuffer = charPool.Rent(bufferSize);

            try
            {
                var requiredLength = encoding.GetMaxByteCount(bufferSize);
                _byteBuffer = bytePool.Rent(requiredLength);
            }
            catch
            {
                charPool.Return(_charBuffer);

                if (_byteBuffer != null)
                {
                    bytePool.Return(_byteBuffer);
                }

                throw;
            }
        }

        public override Encoding Encoding { get; }

        public override void Write(char value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            if (_charBufferCount == _charBufferSize)
            {
                FlushInternal(flushEncoder: false);
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override void Write(char[] values, int index, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            if (values == null)
            {
                return;
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    FlushInternal(flushEncoder: false);
                }

                CopyToCharBuffer(values, ref index, ref count);
            }
        }

        public override void Write(ReadOnlySpan<char> value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            var remaining = value.Length;
            while (remaining > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    FlushInternal(flushEncoder: false);
                }

                var written = CopyToCharBuffer(value);
                
                remaining -= written;
                value = value.Slice(written);
            };
        }

        public override void Write(string value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            if (value == null)
            {
                return;
            }

            var count = value.Length;
            var index = 0;
            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    FlushInternal(flushEncoder: false);
                }

                CopyToCharBuffer(value, ref index, ref count);
            }
        }

        public override void WriteLine(ReadOnlySpan<char> value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            Write(value);
            Write(NewLine);
        }

        public override Task WriteAsync(char value)
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            if (_charBufferCount == _charBufferSize)
            {
                return WriteAsyncAwaited(value);
            }
            else
            {
                // Enough room in buffer, no need to go async
                _charBuffer[_charBufferCount] = value;
                _charBufferCount++;
                return Task.CompletedTask;
            }
        }

        private async Task WriteAsyncAwaited(char value)
        {
            Debug.Assert(_charBufferCount == _charBufferSize);

            await FlushInternalAsync(flushEncoder: false);

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override Task WriteAsync(char[] values, int index, int count)
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            if (values == null || count == 0)
            {
                return Task.CompletedTask;
            }

            var remaining = _charBufferSize - _charBufferCount;
            if (remaining >= count)
            {
                // Enough room in buffer, no need to go async
                CopyToCharBuffer(values, ref index, ref count);
                return Task.CompletedTask;
            }
            else
            {
                return WriteAsyncAwaited(values, index, count);
            }
        }

        private async Task WriteAsyncAwaited(char[] values, int index, int count)
        {
            Debug.Assert(count > 0);
            Debug.Assert(_charBufferSize - _charBufferCount < count);

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushInternalAsync(flushEncoder: false);
                }

                CopyToCharBuffer(values, ref index, ref count);
            }
        }

        public override Task WriteAsync(string value)
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            var count = value?.Length ?? 0;
            if (count == 0)
            {
                return Task.CompletedTask;
            }

            var remaining = _charBufferSize - _charBufferCount;
            if (remaining >= count)
            {
                // Enough room in buffer, no need to go async
                CopyToCharBuffer(value);
                return Task.CompletedTask;
            }
            else
            {
                return WriteAsyncAwaited(value);
            }
        }

        private async Task WriteAsyncAwaited(string value)
        {
            var count = value.Length;

            Debug.Assert(count > 0);
            Debug.Assert(_charBufferSize - _charBufferCount < count);

            var index = 0;
            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushInternalAsync(flushEncoder: false);
                }

                CopyToCharBuffer(value, ref index, ref count);
            }
        }

        public override Task WriteAsync(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (value.IsEmpty)
            {
                return Task.CompletedTask;
            }

            var remaining = _charBufferSize - _charBufferCount;
            if (remaining >= value.Length)
            {
                // Enough room in buffer, no need to go async
                CopyToCharBuffer(value.Span);
                return Task.CompletedTask;
            }
            else
            {
                return WriteAsyncAwaited(value);
            }
        }

        private async Task WriteAsyncAwaited(ReadOnlyMemory<char> value)
        {
            Debug.Assert(value.Length > 0);
            Debug.Assert(_charBufferSize - _charBufferCount < value.Length);

            var remaining = value.Length;
            while (remaining > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushInternalAsync(flushEncoder: false);
                }

                var written = CopyToCharBuffer(value.Span);
                
                remaining -= written;
                value = value.Slice(written);
            };
        }

        public override Task WriteLineAsync(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (value.IsEmpty && NewLine.Length == 0)
            {
                return Task.CompletedTask;
            }

            var remaining = _charBufferSize - _charBufferCount;
            if (remaining >= value.Length + NewLine.Length)
            {
                // Enough room in buffer, no need to go async
                CopyToCharBuffer(value.Span);
                CopyToCharBuffer(NewLine);
                return Task.CompletedTask;
            }
            else
            {
                return WriteLineAsyncAwaited(value);
            }
        }

        private async Task WriteLineAsyncAwaited(ReadOnlyMemory<char> value)
        {
            await WriteAsync(value);
            await WriteAsync(NewLine);
        }

        // We want to flush the stream when Flush/FlushAsync is explicitly
        // called by the user (example: from a Razor view).

        public override void Flush()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            FlushInternal(flushEncoder: true);
        }

        public override Task FlushAsync()
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            return FlushInternalAsync(flushEncoder: true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                try
                {
                    FlushInternal(flushEncoder: true);
                }
                finally
                {
                    _bytePool.Return(_byteBuffer);
                    _charPool.Return(_charBuffer);
                }
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                try
                {
                    await FlushInternalAsync(flushEncoder: true);
                }
                finally
                {
                    _bytePool.Return(_byteBuffer);
                    _charPool.Return(_charBuffer);
                }
            }

            await base.DisposeAsync();
        }

        // Note: our FlushInternal method does NOT flush the underlying stream. This would result in
        // chunking.
        private void FlushInternal(bool flushEncoder)
        {
            if (_charBufferCount == 0)
            {
                return;
            }

            var count = _encoder.GetBytes(
                _charBuffer,
                0,
                _charBufferCount,
                _byteBuffer,
                0,
                flush: flushEncoder);

            _charBufferCount = 0;

            if (count > 0)
            {
                _stream.Write(_byteBuffer, 0, count);
            }
        }

        // Note: our FlushInternalAsync method does NOT flush the underlying stream. This would result in
        // chunking.
        private async Task FlushInternalAsync(bool flushEncoder)
        {
            if (_charBufferCount == 0)
            {
                return;
            }

            var count = _encoder.GetBytes(
                _charBuffer,
                0,
                _charBufferCount,
                _byteBuffer,
                0,
                flush: flushEncoder);

            _charBufferCount = 0;

            if (count > 0)
            {
                await _stream.WriteAsync(_byteBuffer, 0, count);
            }
        }

        private void CopyToCharBuffer(string value)
        {
            Debug.Assert(_charBufferSize - _charBufferCount >= value.Length);

            value.CopyTo(
                sourceIndex: 0,
                destination: _charBuffer,
                destinationIndex: _charBufferCount,
                count: value.Length);

            _charBufferCount += value.Length;
        }

        private void CopyToCharBuffer(string value, ref int index, ref int count)
        {
            var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

            value.CopyTo(
                sourceIndex: index,
                destination: _charBuffer,
                destinationIndex: _charBufferCount,
                count: remaining);

            _charBufferCount += remaining;
            index += remaining;
            count -= remaining;
        }

        private void CopyToCharBuffer(char[] values, ref int index, ref int count)
        {
            var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

            Buffer.BlockCopy(
                src: values,
                srcOffset: index * sizeof(char),
                dst: _charBuffer,
                dstOffset: _charBufferCount * sizeof(char),
                count: remaining * sizeof(char));

            _charBufferCount += remaining;
            index += remaining;
            count -= remaining;
        }

        private int CopyToCharBuffer(ReadOnlySpan<char> value)
        {
            var remaining = Math.Min(_charBufferSize - _charBufferCount, value.Length);

            var source = value.Slice(0, remaining);
            var destination = new Span<char>(_charBuffer, _charBufferCount, remaining);
            source.CopyTo(destination);

            _charBufferCount += remaining;

            return remaining;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task GetObjectDisposedTask()
        {
            return Task.FromException(new ObjectDisposedException(nameof(HttpResponseStreamWriter)));
        }
    }
}
