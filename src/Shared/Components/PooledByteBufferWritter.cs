// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Infrastructure
{
    internal sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private byte[] _currentBuffer;
        private int _index;
        private readonly bool _owned;

        private const int MinimumBufferSize = 256;

        public PooledByteBufferWriter(int initialCapacity = 0)
        {
            _currentBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _index = 0;
        }

        public PooledByteBufferWriter(byte[] existingBuffer)
        {
            _currentBuffer = existingBuffer;
            _index = existingBuffer.Length;
            _owned = true;
        }

        public ReadOnlyMemory<byte> WrittenMemory => _currentBuffer.AsMemory(0, _index);

        public int WrittenCount => _index;

        public int Capacity => _currentBuffer.Length;

        public int FreeCapacity => _currentBuffer.Length - _index;

        public void Clear() => ClearHelper();

        private void ClearHelper()
        {
            _currentBuffer.AsSpan(0, _index).Clear();
            _index = 0;
        }

        // Returns the rented buffer back to the pool
        public void Dispose()
        {
            if (_currentBuffer == null || _owned)
            {
                return;
            }

            ClearHelper();
            var currentBuffer = _currentBuffer;
            _currentBuffer = null!;
            ArrayPool<byte>.Shared.Return(currentBuffer);
        }

        public void Advance(int count)
        {
            _index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _currentBuffer.AsMemory(_index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _currentBuffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (_owned)
            {
                throw new InvalidOperationException("Can't grow a buffer created from a given array.");
            }

            if (sizeHint == 0)
            {
                sizeHint = MinimumBufferSize;
            }

            var availableSpace = _currentBuffer.Length - _index;

            if (sizeHint > availableSpace)
            {
                var currentLength = _currentBuffer.Length;
                var growBy = Math.Max(sizeHint, currentLength);

                var newSize = currentLength + growBy;

                if ((uint)newSize > int.MaxValue)
                {
                    newSize = currentLength + sizeHint;
                    if ((uint)newSize > int.MaxValue)
                    {
                        throw new OutOfMemoryException();
                    }
                }

                var oldBuffer = _currentBuffer;

                _currentBuffer = ArrayPool<byte>.Shared.Rent(newSize);

                var previousBuffer = oldBuffer.AsSpan(0, _index);
                previousBuffer.CopyTo(_currentBuffer);
                ArrayPool<byte>.Shared.Return(oldBuffer, clearArray: true);
            }
        }
    }
}
