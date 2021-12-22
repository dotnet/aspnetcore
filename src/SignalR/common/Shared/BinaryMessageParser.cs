// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Internal;

internal static class BinaryMessageParser
{
    private const int MaxLengthPrefixSize = 5;

    public static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
    {
        if (buffer.IsEmpty)
        {
            payload = default;
            return false;
        }

        // The payload starts with a length prefix encoded as a VarInt. VarInts use the most significant bit
        // as a marker whether the byte is the last byte of the VarInt or if it spans to the next byte. Bytes
        // appear in the reverse order - i.e. the first byte contains the least significant bits of the value
        // Examples:
        // VarInt: 0x35 - %00110101 - the most significant bit is 0 so the value is %x0110101 i.e. 0x35 (53)
        // VarInt: 0x80 0x25 - %10000000 %00101001 - the most significant bit of the first byte is 1 so the
        // remaining bits (%x0000000) are the lowest bits of the value. The most significant bit of the second
        // byte is 0 meaning this is last byte of the VarInt. The actual value bits (%x0101001) need to be
        // prepended to the bits we already read so the values is %01010010000000 i.e. 0x1480 (5248)
        // We support payloads up to 2GB so the biggest number we support is 7fffffff which when encoded as
        // VarInt is 0xFF 0xFF 0xFF 0xFF 0x07 - hence the maximum length prefix is 5 bytes.

        var length = 0U;
        var numBytes = 0;

        var lengthPrefixBuffer = buffer.Slice(0, Math.Min(MaxLengthPrefixSize, buffer.Length));
        var span = GetSpan(lengthPrefixBuffer);

        byte byteRead;
        do
        {
            byteRead = span[numBytes];
            length = length | (((uint)(byteRead & 0x7f)) << (numBytes * 7));
            numBytes++;
        }
        while (numBytes < lengthPrefixBuffer.Length && ((byteRead & 0x80) != 0));

        // size bytes are missing
        if ((byteRead & 0x80) != 0 && (numBytes < MaxLengthPrefixSize))
        {
            payload = default;
            return false;
        }

        if ((byteRead & 0x80) != 0 || (numBytes == MaxLengthPrefixSize && byteRead > 7))
        {
            throw new FormatException("Messages over 2GB in size are not supported.");
        }

        // We don't have enough data
        if (buffer.Length < length + numBytes)
        {
            payload = default;
            return false;
        }

        // Get the payload
        payload = buffer.Slice(numBytes, (int)length);

        // Skip the payload
        buffer = buffer.Slice(numBytes + (int)length);
        return true;
    }

    private static ReadOnlySpan<byte> GetSpan(in ReadOnlySequence<byte> lengthPrefixBuffer)
    {
        if (lengthPrefixBuffer.IsSingleSegment)
        {
            return lengthPrefixBuffer.First.Span;
        }

        // Should be rare
        return lengthPrefixBuffer.ToArray();
    }
}
