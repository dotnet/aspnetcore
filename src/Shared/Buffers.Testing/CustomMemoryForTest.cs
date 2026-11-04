// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers;

internal sealed class CustomMemoryForTest<T> : IMemoryOwner<T>
{
    private bool _disposed;
    private T[] _array;
    private readonly int _offset;
    private readonly int _length;

    public CustomMemoryForTest(T[] array) : this(array, 0, array.Length)
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
            ObjectDisposedException.ThrowIf(_disposed, this);
            return new Memory<T>(_array, _offset, _length);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _array = null!;
        _disposed = true;
    }
}

