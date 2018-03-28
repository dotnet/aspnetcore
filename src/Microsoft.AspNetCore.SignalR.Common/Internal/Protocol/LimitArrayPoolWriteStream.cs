// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public sealed class LimitArrayPoolWriteStream : Stream
    {
        private const int MaxByteArrayLength = 0x7FFFFFC7;
        private const int InitialLength = 256;

        private readonly int _maxBufferSize;
        private byte[] _buffer;
        private int _length;

        public LimitArrayPoolWriteStream() : this(MaxByteArrayLength) { }

        public LimitArrayPoolWriteStream(int maxBufferSize) : this(maxBufferSize, InitialLength) { }

        public LimitArrayPoolWriteStream(int maxBufferSize, long capacity)
        {
            if (capacity < InitialLength)
            {
                capacity = InitialLength;
            }
            else if (capacity > maxBufferSize)
            {
                throw CreateOverCapacityException(maxBufferSize);
            }

            _maxBufferSize = maxBufferSize;
            _buffer = ArrayPool<byte>.Shared.Rent((int)capacity);
        }

        protected override void Dispose(bool disposing)
        {
            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }

            base.Dispose(disposing);
        }

        public ArraySegment<byte> GetBuffer() => new ArraySegment<byte>(_buffer, 0, _length);

        public byte[] ToArray()
        {
            var arr = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, arr, 0, _length);
            return arr;
        }

        private void EnsureCapacity(int value)
        {
            if ((uint)value > (uint)_maxBufferSize) // value cast handles overflow to negative as well
            {
                throw CreateOverCapacityException(_maxBufferSize);
            }
            else if (value > _buffer.Length)
            {
                Grow(value);
            }
        }

        private void Grow(int value)
        {
            Debug.Assert(value > _buffer.Length);

            // Extract the current buffer to be replaced.
            byte[] currentBuffer = _buffer;
            _buffer = null;

            // Determine the capacity to request for the new buffer.  It should be
            // at least twice as long as the current one, if not more if the requested
            // value is more than that.  If the new value would put it longer than the max
            // allowed byte array, than shrink to that (and if the required length is actually
            // longer than that, we'll let the runtime throw).
            uint twiceLength = 2 * (uint)currentBuffer.Length;
            int newCapacity = twiceLength > MaxByteArrayLength ?
                (value > MaxByteArrayLength ? value : MaxByteArrayLength) :
                Math.Max(value, (int)twiceLength);

            // Get a new buffer, copy the current one to it, return the current one, and
            // set the new buffer as current.
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
            Buffer.BlockCopy(currentBuffer, 0, newBuffer, 0, _length);
            ArrayPool<byte>.Shared.Return(currentBuffer);
            _buffer = newBuffer;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);

            EnsureCapacity(_length + count);
            Buffer.BlockCopy(buffer, offset, _buffer, _length, count);
            _length += count;
        }

#if NETCOREAPP2_1
        public override void Write(ReadOnlySpan<byte> source)
        {
            EnsureCapacity(_length + source.Length);
            source.CopyTo(new Span<byte>(_buffer, _length, source.Length));
            _length += source.Length;
        }
#endif

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

#if NETCOREAPP2_1
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            Write(source.Span);
            return default;
        }
#endif

        public override void WriteByte(byte value)
        {
            int newLength = _length + 1;
            EnsureCapacity(newLength);
            _buffer[_length] = value;
            _length = newLength;
        }

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override long Length => _length;
        public override bool CanWrite => true;
        public override bool CanRead => false;
        public override bool CanSeek => false;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }

        private static Exception CreateOverCapacityException(int maxBufferSize)
        {
            return new InvalidOperationException($"Buffer size of {maxBufferSize} exceeded.");
        }
    }
}