using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    internal class PipeWriterTextWriter : TextWriter
    {
        private const int MinBufferSize = 128;
        internal const int DefaultBufferSize = 16 * 1024;

        private readonly PipeWriter _writer;
        private readonly Encoder _encoder;
        private readonly int _maxSingleByteEncodingSize;
        private readonly ArrayPool<char> _charPool;
        private readonly int _charBufferSize;

        private Memory<byte> _buffer;
        private char[] _charBuffer;

        private int _charBufferCount;
        private int _memoryUsed;
        private bool _disposed;

        public PipeWriterTextWriter(
            PipeWriter writer,
            Encoding encoding,
            int bufferSize,
            ArrayPool<byte> bytePool,
            ArrayPool<char> charPool)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            _charBufferSize = bufferSize;
            _encoder = encoding.GetEncoder();
            _maxSingleByteEncodingSize = encoding.GetMaxByteCount(1);
            _charBuffer = charPool.Rent(bufferSize);

            try
            {
                var requiredLength = encoding.GetMaxByteCount(bufferSize);
            }
            catch
            {
                charPool.Return(_charBuffer);

                throw;
            }
        }

        public override Encoding Encoding { get; }

        public override void Write(char value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("derp");
            }

            if (_charBufferCount == _charBufferSize)
            {
                CommitInternal(flushEncoder: false);
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override void Write(char[] values, int index, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("derp");
            }

            if (values == null)
            {
                return;
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    CommitInternal(flushEncoder: false);
                }

                CopyToCharBuffer(values, ref index, ref count);
            }
        }

        public override void Write(string value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("derp");
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
                    CommitInternal(flushEncoder: false);
                }

                CopyToCharBuffer(value, ref index, ref count);
            }
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

        private Task WriteAsyncAwaited(char value)
        {
            Debug.Assert(_charBufferCount == _charBufferSize);

            CommitInternal(flushEncoder: false);

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
            return Task.CompletedTask;
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

        private Task WriteAsyncAwaited(char[] values, int index, int count)
        {
            Debug.Assert(count > 0);
            Debug.Assert(_charBufferSize - _charBufferCount < count);

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    CommitInternal(flushEncoder: false);
                }

                CopyToCharBuffer(values, ref index, ref count);
            }

            return Task.CompletedTask;
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

        private Task WriteAsyncAwaited(string value)
        {
            var count = value.Length;

            Debug.Assert(count > 0);
            Debug.Assert(_charBufferSize - _charBufferCount < count);

            var index = 0;
            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    CommitInternal(flushEncoder: false);
                }

                CopyToCharBuffer(value, ref index, ref count);
            }

            return Task.CompletedTask;
        }

        public override void Flush()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("derp");
            }

            CommitInternal(flushEncoder: true);
        }

        public override async Task FlushAsync()
        {
            if (_disposed)
            {
                await GetObjectDisposedTask();
                return;
            }

            CommitInternal(flushEncoder: true);
            if (_memoryUsed > 0)
            {
                _writer.Advance(_memoryUsed);
            }

            await _writer.FlushAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                try
                {
                    Flush();
                }
                finally
                {
                    _charPool.Return(_charBuffer);
                }
            }

            base.Dispose(disposing);
        }

        private void CommitInternal(bool flushEncoder)
        {
            if (_charBufferCount == 0)
            {
                return;
            }

            var charBuffer = _charBuffer.AsSpan(0, _charBufferCount);
            while (_charBufferCount > 0)
            {
                var buffer = GetBuffer();

                if (buffer.Length < 4 || !(buffer[0] == '\0' && buffer[1] == '\0' && buffer[2] == '\0' && buffer[3] =='\0'))
                {
                    Console.WriteLine();
                }
                
                _encoder.Convert(
                    charBuffer,
                    buffer,
                    flushEncoder,
                    out var charCount,
                    out var bytesUsed,
                    out var completed);
                _memoryUsed += bytesUsed;
                _charBufferCount -= charCount;

                charBuffer = charBuffer.Slice(charCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<byte> GetBuffer()
        {
            EnsureBuffer();

            return _buffer.Span.Slice(_memoryUsed, _buffer.Length - _memoryUsed);
        }

        private void EnsureBuffer()
        {
            // We need at least enough bytes to encode a single UTF-8 character, or Encoder.Convert will throw.
            // Normally, if there isn't enough space to write every character of a char buffer, Encoder.Convert just
            // writes what it can. However, if it can't even write a single character, it throws. So if the buffer has only
            // 2 bytes left and the next character to write is 3 bytes in UTF-8, an exception is thrown.
            var remaining = _buffer.Length - _memoryUsed;
            if (remaining < _maxSingleByteEncodingSize)
            {
                // Used up the memory from the buffer writer so advance and get more
                if (_memoryUsed > 0)
                {
                    _writer.Advance(_memoryUsed);
                }

                _buffer = _writer.GetMemory(_maxSingleByteEncodingSize);
                _memoryUsed = 0;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task GetObjectDisposedTask()
        {
            return Task.FromException(new ObjectDisposedException("LOL"));
        }
    }
}
