// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Copied from https://github.com/dotnet/corefx/blob/b0751dcd4a419ba6731dcaa7d240a8a1946c934c/src/System.Text.Json/src/System/Text/Json/Serialization/ArrayBufferWriter.cs

using System.Diagnostics;

namespace System.Buffers
{
    internal sealed class PooledArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private T[] _rentedBuffer;
        private int _index;

        private const int MinimumBufferSize = 256;

        public PooledArrayBufferWriter()
        {
            _rentedBuffer = ArrayPool<T>.Shared.Rent(MinimumBufferSize);
            _index = 0;
        }

        public PooledArrayBufferWriter(int initialCapacity)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
            _index = 0;
        }

        public ReadOnlyMemory<T> WrittenMemory
        {
            get
            {
                CheckIfDisposed();

                return _rentedBuffer.AsMemory(0, _index);
            }
        }

        public int WrittenCount
        {
            get
            {
                CheckIfDisposed();

                return _index;
            }
        }

        public int Capacity
        {
            get
            {
                CheckIfDisposed();

                return _rentedBuffer.Length;
            }
        }

        public int FreeCapacity
        {
            get
            {
                CheckIfDisposed();

                return _rentedBuffer.Length - _index;
            }
        }

        public void Clear()
        {
            CheckIfDisposed();

            ClearHelper();
        }

        private void ClearHelper()
        {
            Debug.Assert(_rentedBuffer != null);

            _rentedBuffer.AsSpan(0, _index).Clear();
            _index = 0;
        }

        // Returns the rented buffer back to the pool
        public void Dispose()
        {
            if (_rentedBuffer == null)
            {
                return;
            }

            ClearHelper();
            ArrayPool<T>.Shared.Return(_rentedBuffer);
            _rentedBuffer = null;
        }

        private void CheckIfDisposed()
        {
            if (_rentedBuffer == null)
            {
                ThrowObjectDisposedException();
            }
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(nameof(ArrayBufferWriter<T>));
        }

        public void Advance(int count)
        {
            CheckIfDisposed();

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (_index > _rentedBuffer.Length - count)
            {
                ThrowInvalidOperationException(_rentedBuffer.Length);
            }

            _index += count;
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            CheckIfDisposed();

            CheckAndResizeBuffer(sizeHint);
            return _rentedBuffer.AsMemory(_index);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckIfDisposed();

            CheckAndResizeBuffer(sizeHint);
            return _rentedBuffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            Debug.Assert(_rentedBuffer != null);

            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint));
            }

            if (sizeHint == 0)
            {
                sizeHint = MinimumBufferSize;
            }

            var availableSpace = _rentedBuffer.Length - _index;

            if (sizeHint > availableSpace)
            {
                var growBy = Math.Max(sizeHint, _rentedBuffer.Length);

                var newSize = checked(_rentedBuffer.Length + growBy);

                var oldBuffer = _rentedBuffer;

                _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

                Debug.Assert(oldBuffer.Length >= _index);
                Debug.Assert(_rentedBuffer.Length >= _index);

                var previousBuffer = oldBuffer.AsSpan(0, _index);
                previousBuffer.CopyTo(_rentedBuffer);
                previousBuffer.Clear();
                ArrayPool<T>.Shared.Return(oldBuffer);
            }

            Debug.Assert(_rentedBuffer.Length - _index > 0);
            Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
        }

        private static void ThrowInvalidOperationException(int capacity)
        {
            throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {capacity}.");
        }
    }
}
