// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Writes the subset of the <see cref="System.IO.BinaryWriter"/> format used by <see cref="TicketSerializer"/> and
/// <see cref="PropertiesSerializer"/>, either to a caller-provided <see cref="System.IO.BinaryWriter"/> or directly
/// into a private pooled buffer that produces the same bytes as <see cref="System.IO.BinaryWriter(Stream)"/>.
/// The pooled buffer never escapes this type and is cleared before it is returned to the pool.
/// </summary>
internal ref struct BinaryPayloadWriter
{
    private readonly BinaryWriter? _writer;
    private byte[]? _buffer;
    private int _position;

    public BinaryPayloadWriter(BinaryWriter writer)
    {
        _writer = writer;
    }

    public BinaryPayloadWriter(int initialCapacity)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    /// <summary>
    /// Gets the wrapped <see cref="System.IO.BinaryWriter"/>, or <see langword="null"/> when writing to a pooled buffer.
    /// </summary>
    public readonly BinaryWriter? BinaryWriter => _writer;

    public void WriteInt32(int value)
    {
        if (_writer is not null)
        {
            _writer.Write(value);
            return;
        }

        var buffer = EnsureCapacity(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        _position += sizeof(int);
    }

    public void WriteBoolean(bool value)
    {
        if (_writer is not null)
        {
            _writer.Write(value);
            return;
        }

        var buffer = EnsureCapacity(1);
        buffer[0] = value ? (byte)1 : (byte)0;
        _position++;
    }

    // Uses Encoding.UTF8 (replacement fallback for invalid surrogates), matching BinaryWriter(Stream).
    public void WriteString(string value)
    {
        if (_writer is not null)
        {
            _writer.Write(value);
            return;
        }

        ArgumentNullException.ThrowIfNull(value);

        var byteCount = Encoding.UTF8.GetByteCount(value);
        var buffer = EnsureCapacity(5 + byteCount);

        // Matches BinaryWriter.Write7BitEncodedInt.
        var length = (uint)byteCount;
        var index = 0;
        while (length > 0x7Fu)
        {
            buffer[index++] = (byte)(length | ~0x7Fu);
            length >>= 7;
        }
        buffer[index++] = (byte)length;

        index += Encoding.UTF8.GetBytes(value, buffer[index..]);
        _position += index;
    }

    public readonly byte[] ToArray() => _buffer.AsSpan(0, _position).ToArray();

    public void Dispose()
    {
        if (_buffer is { } buffer)
        {
            _buffer = null;
            ReturnBuffer(buffer, _position);
        }
    }

    private Span<byte> EnsureCapacity(int sizeHint)
    {
        var buffer = _buffer!;
        if (buffer.Length - _position < sizeHint)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(checked(_position + sizeHint), checked(buffer.Length * 2)));
            buffer.AsSpan(0, _position).CopyTo(newBuffer);
            ReturnBuffer(buffer, _position);
            _buffer = buffer = newBuffer;
        }

        return buffer.AsSpan(_position);
    }

    private static void ReturnBuffer(byte[] buffer, int used)
    {
        // The payload may contain user claims, so make sure nothing lingers in the shared pool.
        buffer.AsSpan(0, used).Clear();
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
