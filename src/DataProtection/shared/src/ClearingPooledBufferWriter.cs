// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.Buffers;

namespace Microsoft.AspNetCore.DataProtection;

// A pooled buffer writer which clears every buffer it rents before returning it to the pool, since the buffers
// may hold plaintext. Instances are private to a single Protect/Unprotect call and never escape to callers.
internal ref struct ClearingPooledBufferWriter : IBufferWriter<byte>
{
    private byte[] _buffer;
    private int _written;

    public ClearingPooledBufferWriter(int initialCapacity)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _written = 0;
    }

    public readonly ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _buffer.Length - _written);
        _written += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void EnsureCapacity(int sizeHint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (_buffer.Length - _written >= sizeHint)
        {
            return;
        }

        var newBuffer = ArrayPool<byte>.Shared.Rent(checked(_written + Math.Max(sizeHint, _buffer.Length)));
        _buffer.AsSpan(0, _written).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = Array.Empty<byte>();
        _written = 0;
    }
}
#endif
