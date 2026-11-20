// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Writes the same binary format as <see cref="BinaryWriter"/> (little-endian integers, 7-bit encoded
/// length-prefixed UTF-8 strings) into a private pooled buffer. The buffer never escapes this type and is
/// cleared before it is returned to the pool.
/// </summary>
internal ref struct BinaryPayloadWriter
{
    private byte[] _buffer;
    private int _position;

    public BinaryPayloadWriter(int initialCapacity)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _position = 0;
    }

    public readonly int Position => _position;

    public void WriteInt32(int value)
    {
        EnsureCapacity(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), value);
        _position += sizeof(int);
    }

    public readonly void PatchInt32(int position, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(position), value);
    }

    public void WriteBoolean(bool value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value ? (byte)1 : (byte)0;
    }

    // Uses Encoding.UTF8 (replacement fallback for invalid surrogates), matching BinaryWriter(Stream).
    public void WriteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var byteCount = Encoding.UTF8.GetByteCount(value);
        EnsureCapacity(5 + byteCount);
        Write7BitEncodedInt(byteCount);
        _position += Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position));
    }

    private void Write7BitEncodedInt(int value)
    {
        var uValue = (uint)value;
        while (uValue > 0x7Fu)
        {
            _buffer[_position++] = (byte)(uValue | ~0x7Fu);
            uValue >>= 7;
        }
        _buffer[_position++] = (byte)uValue;
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (_buffer.Length - _position < sizeHint)
        {
            Grow(sizeHint);
        }
    }

    private void Grow(int sizeHint)
    {
        var newSize = Math.Max(checked(_position + sizeHint), checked(_buffer.Length * 2));
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ReturnBuffer(_buffer, _position);
        _buffer = newBuffer;
    }

    public readonly byte[] ToArray() => _buffer.AsSpan(0, _position).ToArray();

    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer is not null)
        {
            _buffer = null!;
            ReturnBuffer(buffer, _position);
        }
    }

    private static void ReturnBuffer(byte[] buffer, int used)
    {
        // The payload may contain user claims, so make sure nothing lingers in the shared pool.
        buffer.AsSpan(0, used).Clear();
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
