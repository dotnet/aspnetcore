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
        private readonly int _maximumBytesPerCharacter;

        private readonly Encoder _encoder;
        private PipeWriter _writer;
        private Memory<byte> _memory;
        private int _memoryUsed;

        public PipeWriterTextWriter(PipeWriter writer, Encoding encoding)
        {
            _writer = writer;
            Encoding = encoding;

            _encoder = encoding.GetEncoder();
            _maximumBytesPerCharacter = encoding.GetMaxByteCount(1);
        }

        public override Encoding Encoding { get; }

        public override void Write(char[] buffer, int index, int count)
        {
            WriteInternal(buffer.AsSpan(index, count));
        }

        public override void Write(char[] buffer)
        {
            WriteInternal(buffer);
        }

        public override void Write(char value)
        {
            if (value <= 127)
            {
                EnsureBuffer();

                // Only need to set one byte
                // Avoid Memory<T>.Slice overhead for perf
                _memory.Span[_memoryUsed] = (byte)value;
                _memoryUsed++;
            }
            else
            {
                WriteMultiByteChar(value);
            }
        }

        private unsafe void WriteMultiByteChar(char value)
        {
            var destination = GetBuffer();

            // Json.NET only writes ASCII characters by themselves, e.g. {}[], etc
            // this should be an exceptional case
            var bytesUsed = 0;
            var charsUsed = 0;
#if NETCOREAPP3_0
            _encoder.Convert(new Span<char>(&value, 1), destination, false, out charsUsed, out bytesUsed, out _);
#else
            fixed (byte* destinationBytes = &MemoryMarshal.GetReference(destination))
            {
                _encoder.Convert(&value, 1, destinationBytes, destination.Length, false, out charsUsed, out bytesUsed, out _);
            }
#endif

            Debug.Assert(charsUsed == 1);

            _memoryUsed += bytesUsed;
        }

        public override void Write(string value)
        {
            WriteInternal(value.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<byte> GetBuffer()
        {
            EnsureBuffer();

            return _memory.Span.Slice(_memoryUsed, _memory.Length - _memoryUsed);
        }

        private void EnsureBuffer()
        {
            // We need at least enough bytes to encode a single UTF-8 character, or Encoder.Convert will throw.
            // Normally, if there isn't enough space to write every character of a char buffer, Encoder.Convert just
            // writes what it can. However, if it can't even write a single character, it throws. So if the buffer has only
            // 2 bytes left and the next character to write is 3 bytes in UTF-8, an exception is thrown.
            var remaining = _memory.Length - _memoryUsed;
            if (remaining < _maximumBytesPerCharacter)
            {
                // Used up the memory from the buffer writer so advance and get more
                if (_memoryUsed > 0)
                {
                    _writer.Advance(_memoryUsed);
                }

                _memory = _writer.GetMemory(_maximumBytesPerCharacter);
                _memoryUsed = 0;
            }
        }

        public override Task WriteAsync(string value)
        {
            WriteInternal(value.AsSpan());
            return Task.CompletedTask;
        }

        private void WriteInternal(ReadOnlySpan<char> buffer)
        {
            while (buffer.Length > 0)
            {
                // The destination byte array might not be large enough so multiple writes are sometimes required
                var destination = GetBuffer();

                _encoder.Convert(buffer, destination, flush: false, out var charsUsed, out var bytesUsed, out _);
                buffer = buffer.Slice(charsUsed);
                _memoryUsed += bytesUsed;
            }
        }

        public override void Flush()
        {
            if (_memoryUsed > 0)
            {
                _writer.Advance(_memoryUsed);
                _memory = _memory.Slice(_memoryUsed, _memory.Length - _memoryUsed);
                _memoryUsed = 0;
            }
        }

        public override Task FlushAsync()
        {
            //Flush();
            //await _writer.FlushAsync();
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Flush();
            }
        }
    }
}
