// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Shared;

internal static class Int7BitEncodingUtils
{
    public static int Measure7BitEncodedUIntLength(this int value)
        => Measure7BitEncodedUIntLength((uint)value);

    public static int Measure7BitEncodedUIntLength(this uint value)
    {
#if NET10_0_OR_GREATER
        return ((31 - System.Numerics.BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;
#else
        int count = 1;
        while ((value >>= 7) != 0)
        {
            count++;
        }
        return count;
#endif
    }

    public static int Write7BitEncodedInt(this Span<byte> target, int value)
        => Write7BitEncodedInt(target, (uint)value);

    public static int Write7BitEncodedInt(this Span<byte> target, uint uValue)
    {
        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        //
        // Using the constants 0x7F and ~0x7F below offers smaller
        // codegen than using the constant 0x80.

        int index = 0;
        while (uValue > 0x7Fu)
        {
            target[index++] = (byte)(uValue | ~0x7Fu);
            uValue >>= 7;
        }

        target[index++] = (byte)uValue;
        return index;
    }

    /// <summary>
    /// Reads a 7-bit encoded unsigned integer from the source span.
    /// </summary>
    /// <param name="source">The source span to read from.</param>
    /// <param name="value">The decoded value.</param>
    /// <returns>The number of bytes consumed from the source span.</returns>
    /// <exception cref="FormatException">Thrown when the encoded value is malformed or exceeds 32 bits.</exception>
    public static int Read7BitEncodedInt(this ReadOnlySpan<byte> source, out int value)
    {
        // Read out an int 7 bits at a time. The high bit of the byte,
        // when on, indicates more bytes to read.
        // A 32-bit unsigned integer can be encoded in at most 5 bytes.

        value = 0;
        var shift = 0;
        var index = 0;

        byte b;
        do
        {
            // Check if we've exceeded the maximum number of bytes for a 32-bit integer
            // or if we've run out of data.
            if (shift == 35 || index >= source.Length)
            {
                throw new FormatException("Bad 7-bit encoded integer.");
            }

            b = source[index++];
            value |= (b & 0x7F) << shift;
            shift += 7;
        }
        while ((b & 0x80) != 0);

        return index;
    }

    /// <summary>
    /// Returns consumed length
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    internal static int Read7BitEncodedString(this ReadOnlySpan<byte> bytes, out string value)
    {
        value = string.Empty;
        var consumed = Read7BitEncodedInt(bytes, out var length);
        if (length == 0)
        {
            return consumed;
        }

        if (bytes.Length < length)
        {
            throw new FormatException("Bad 7-bit encoded string.");
        }

        value = System.Text.Encoding.UTF8.GetString(bytes.Slice(consumed, length));
        consumed += length;
        return consumed;
    }
}
