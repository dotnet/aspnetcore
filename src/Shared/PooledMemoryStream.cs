// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    internal sealed class PooledMemoryStream : Stream
    {
        private readonly ArrayPool<byte> _bytePool;
        private readonly int _maxCapacity;
        private byte[] _buffer;
        private int _length;
        private int _position;
        private bool _disposed;

        public PooledMemoryStream(ArrayPool<byte> bytePool, int maxCapacity)
        {
            if (bytePool == null)
            {
                throw new ArgumentNullException(nameof(bytePool));
            }

            if (maxCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            }

            _bytePool = bytePool;
            _maxCapacity = maxCapacity;
        }

        public override bool CanRead => !_disposed;

        public override bool CanSeek => !_disposed;

        public override bool CanWrite => !_disposed;

        public override long Length
        {
            get
            {
                ThrowIfDisposed();

                return _length;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();

                return _position;
            }
            set
            {
                ThrowIfDisposed();
                if (value < 0 || value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _position = (int)value;
            }
        }

        public byte[] Buffer
        {
            get
            {
                ThrowIfDisposed();

                return _buffer;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            ThrowIfDisposed();

            var bytesToRead = Math.Min(_length - Math.Min(_position, _length), count);
            if (bytesToRead == 0)
            {
                return 0;
            }

            System.Buffer.BlockCopy(_buffer, _position, buffer, offset, bytesToRead);
            _position += bytesToRead;

            return bytesToRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(Read(buffer, offset, count));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            ThrowIfDisposed();

            var endPosition = checked(_position + count);
            EnsureCapacity(endPosition);

            if (_position > _length)
            {
                Array.Clear(_buffer, _length, _position - _length);
            }

            if (count > 0)
            {
                System.Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }

            _position = endPosition;
            _length = Math.Max(_length, endPosition);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer, offset, count);

            return Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();

            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = _position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            if (newPosition < 0)
            {
                throw new IOException("An attempt was made to move the position before the beginning of the stream.");
            }

            Position = newPosition;

            return _position;
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            if (value < 0 || value > _maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var length = (int)value;
            EnsureCapacity(length);
            if (length > _length)
            {
                Array.Clear(_buffer, _length, length - _length);
            }

            _length = length;
            _position = Math.Min(_position, _length);
        }

        public override void Flush()
        {
            ThrowIfDisposed();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_buffer != null)
                {
                    _bytePool.Return(_buffer);
                    _buffer = null;
                }
            }

            base.Dispose(disposing);
        }

        private void EnsureCapacity(int requiredCapacity)
        {
            if (requiredCapacity > _maxCapacity)
            {
                throw new IOException("Memory threshold exceeded.");
            }

            if (_buffer != null && requiredCapacity <= _buffer.Length)
            {
                return;
            }

            if (requiredCapacity == 0)
            {
                return;
            }

            var newCapacity = _buffer == null
                ? requiredCapacity
                : Math.Max(requiredCapacity, Math.Min(_buffer.Length * 2, _maxCapacity));
            var newBuffer = _bytePool.Rent(newCapacity);

            if (_buffer != null)
            {
                System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
                _bytePool.Return(_buffer);
            }

            _buffer = newBuffer;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PooledMemoryStream));
            }
        }

        private static void ValidateBufferArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || buffer.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }
    }
}
