// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    internal class CustomMemoryForTest<T> : IMemoryOwner<T>
    {
        private bool _disposed;
        private T[] _array;
        private readonly int _offset;
        private readonly int _length;

        public CustomMemoryForTest(T[] array): this(array, 0, array.Length)
        {
        }

        public CustomMemoryForTest(T[] array, int offset, int length)
        {
            _array = array;
            _offset = offset;
            _length = length;
        }

        public Memory<T> Memory
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
                return new Memory<T>(_array, _offset, _length);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _array = null;
            _disposed = true;
        }
    }
}

