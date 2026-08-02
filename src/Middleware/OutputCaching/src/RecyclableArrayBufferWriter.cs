// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.OutputCaching;

// this is effectively a cut-down re-implementation of ArrayBufferWriter
// from https://github.com/dotnet/runtime/blob/6cd9bf1937c3b4d2f7304a6c534aacde58a202b6/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs
// except it uses the array pool for allocations
internal sealed class RecyclableArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
{

    // Copy of Array.MaxLength.
    // Used by projects targeting .NET Framework.
    private const int ArrayMaxLength = 0x7FFFFFC7;

    private const int DefaultInitialBufferSize = 256;

    private T[] _buffer;
    private int _index;

    public int FreeCapacity => _buffer.Length - _index;

    public RecyclableArrayBufferWriter()
    {
        _buffer = Array.Empty<T>();
        _index = 0;
    }

    public void Dispose()
    {
        var tmp = _buffer;
        _index = 0;
        _buffer = Array.Empty<T>();
        if (tmp.Length != 0)
        {
            ArrayPool<T>.Shared.Return(tmp);
        }
    }

    public void Advance(int count)
    {
        if (count < 0)
        {
            throw new ArgumentException(null, nameof(count));
        }

        if (_index > _buffer.Length - count)
        {
            Throw();
        }

        _index += count;

        static void Throw()
            => throw new ArgumentOutOfRangeException(nameof(count));
    }

    public ReadOnlyMemory<T> GetCommittedMemory() => new ReadOnlyMemory<T>(_buffer, 0, _index); // could also directly expose a ReadOnlySpan<byte> if useful

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        Debug.Assert(_buffer.Length > _index);
        return _buffer.AsMemory(_index);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        Debug.Assert(_buffer.Length > _index);
        return _buffer.AsSpan(_index);
    }

    // create a standalone isolated copy of the buffer
    public T[] ToArray() => _buffer.AsSpan(0, _index).ToArray();

    private void CheckAndResizeBuffer(int sizeHint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (sizeHint > FreeCapacity)
        {
            int currentLength = _buffer.Length;

            // Attempt to grow by the larger of the sizeHint and double the current size.
            int growBy = Math.Max(sizeHint, currentLength);

            if (currentLength == 0)
            {
                growBy = Math.Max(growBy, DefaultInitialBufferSize);
            }

            int newSize = currentLength + growBy;

            if ((uint)newSize > int.MaxValue)
            {
                // Attempt to grow to ArrayMaxLength.
                uint needed = (uint)(currentLength - FreeCapacity + sizeHint);
                Debug.Assert(needed > currentLength);

                if (needed > ArrayMaxLength)
                {
                    ThrowOutOfMemoryException();
                }

                newSize = ArrayMaxLength;
            }

            // resize the backing buffer
            var oldArray = _buffer;
            _buffer = ArrayPool<T>.Shared.Rent(newSize);
            oldArray.AsSpan(0, _index).CopyTo(_buffer);
            if (oldArray.Length != 0)
            {
                ArrayPool<T>.Shared.Return(oldArray);
            }
        }

        Debug.Assert(FreeCapacity > 0 && FreeCapacity >= sizeHint);

        static void ThrowOutOfMemoryException() => throw new InvalidOperationException("Unable to grow buffer as requested");
    }
}
