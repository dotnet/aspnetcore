// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Reads the subset of the <see cref="System.IO.BinaryReader"/> format used by <see cref="TicketSerializer"/> and
/// <see cref="PropertiesSerializer"/>, either from a caller-provided <see cref="System.IO.BinaryReader"/> or directly
/// from a UTF-8 payload buffer without stream, reader or decoder allocations. Malformed payloads throw the same
/// exception types that <see cref="System.IO.BinaryReader"/> throws.
/// </summary>
internal ref struct BinaryPayloadReader
{
    private readonly BinaryReader? _reader;
    private ReadOnlySpan<byte> _remaining;

    public BinaryPayloadReader(ReadOnlySpan<byte> data)
    {
        _remaining = data;
    }

    public BinaryPayloadReader(BinaryReader reader)
    {
        _reader = reader;
    }

    /// <summary>
    /// Gets the wrapped <see cref="System.IO.BinaryReader"/>, or <see langword="null"/> when reading from a buffer.
    /// </summary>
    public readonly BinaryReader? BinaryReader => _reader;

    public int ReadInt32()
    {
        if (_reader is not null)
        {
            return _reader.ReadInt32();
        }

        if (_remaining.Length < sizeof(int))
        {
            ThrowEndOfStream();
        }

        var value = BinaryPrimitives.ReadInt32LittleEndian(_remaining);
        _remaining = _remaining[sizeof(int)..];
        return value;
    }

    public bool ReadBoolean() => _reader is not null ? _reader.ReadBoolean() : ReadByte() != 0;

    public string ReadString() => _reader is not null ? _reader.ReadString() : Encoding.UTF8.GetString(ReadStringBytes());

    /// <summary>
    /// Reads a string, returning <paramref name="defaultValue"/> when the serialized value is
    /// <see cref="TicketSerializer.DefaultStringPlaceholder"/>.
    /// </summary>
    public string ReadStringWithDefault(string defaultValue)
    {
        if (_reader is not null)
        {
            var value = _reader.ReadString();
            return string.Equals(value, TicketSerializer.DefaultStringPlaceholder, StringComparison.Ordinal) ? defaultValue : value;
        }

        // The UTF-8 encoding of the "\0" placeholder is a single zero byte, so it can be detected without decoding.
        var bytes = ReadStringBytes();
        return bytes is [0] ? defaultValue : Encoding.UTF8.GetString(bytes);
    }

    private ReadOnlySpan<byte> ReadStringBytes()
    {
        var length = Read7BitEncodedInt();
        if (length < 0)
        {
            throw new IOException();
        }

        if (_remaining.Length < length)
        {
            ThrowEndOfStream();
        }

        var bytes = _remaining[..length];
        _remaining = _remaining[length..];
        return bytes;
    }

    private byte ReadByte()
    {
        if (_remaining.IsEmpty)
        {
            ThrowEndOfStream();
        }

        var value = _remaining[0];
        _remaining = _remaining[1..];
        return value;
    }

    // Matches BinaryReader.Read7BitEncodedInt.
    private int Read7BitEncodedInt()
    {
        const int MaxBytesWithoutOverflow = 4;

        uint result = 0;
        byte byteReadJustNow;

        for (var shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            byteReadJustNow = ReadByte();
            result |= (byteReadJustNow & 0x7Fu) << shift;

            if (byteReadJustNow <= 0x7Fu)
            {
                return (int)result;
            }
        }

        byteReadJustNow = ReadByte();
        if (byteReadJustNow > 0b_1111u)
        {
            throw new FormatException();
        }

        result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (int)result;
    }

    private static void ThrowEndOfStream() => throw new EndOfStreamException();
}
