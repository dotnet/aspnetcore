// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Reads the subset of the <see cref="BinaryReader"/> format (UTF-8 strings) used by
/// <see cref="TicketSerializer"/> and <see cref="PropertiesSerializer"/> directly from a span,
/// avoiding the stream, reader and decoder allocations. Errors are reported with the same
/// exception types that <see cref="BinaryReader"/> throws.
/// </summary>
internal ref struct BinaryPayloadReader
{
    private ReadOnlySpan<byte> _remaining;

    public BinaryPayloadReader(ReadOnlySpan<byte> data)
    {
        _remaining = data;
    }

    public int ReadInt32()
    {
        if (_remaining.Length < sizeof(int))
        {
            ThrowEndOfStream();
        }

        var value = BinaryPrimitives.ReadInt32LittleEndian(_remaining);
        _remaining = _remaining[sizeof(int)..];
        return value;
    }

    public bool ReadBoolean() => ReadByte() != 0;

    public string ReadString()
    {
        var bytes = ReadStringBytes();
        return bytes.IsEmpty ? string.Empty : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Reads a string, returning <paramref name="defaultValue"/> without allocating
    /// when the serialized value is the single <c>'\0'</c> placeholder.
    /// </summary>
    public string ReadStringWithDefault(string defaultValue)
    {
        var bytes = ReadStringBytes();
        if (bytes.Length == 1 && bytes[0] == 0)
        {
            return defaultValue;
        }

        return bytes.IsEmpty ? string.Empty : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Same as <see cref="ReadStringWithDefault(string)"/>, but returns the existing string constant
    /// for well-known claim types and claim value types instead of allocating a new string.
    /// </summary>
    public string ReadClaimTypeWithDefault(string defaultValue)
    {
        var bytes = ReadStringBytes();
        if (bytes.Length == 1 && bytes[0] == 0)
        {
            return defaultValue;
        }

        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        return WellKnownClaimStrings.Find(bytes) ?? Encoding.UTF8.GetString(bytes);
    }

    private ReadOnlySpan<byte> ReadStringBytes()
    {
        var length = Read7BitEncodedInt();
        if (length < 0)
        {
            throw new IOException($"BinaryReader encountered an invalid string length of {length} characters.");
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
            throw new FormatException("Too many bytes in what should have been a 7-bit encoded integer.");
        }

        result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (int)result;
    }

    private static void ThrowEndOfStream() => throw new EndOfStreamException("Unable to read beyond the end of the stream.");
}
