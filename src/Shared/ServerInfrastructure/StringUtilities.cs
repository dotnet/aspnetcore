// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal static class StringUtilities
{
    // Null checks must be done independently of this method (if required)
    public static string GetAsciiOrUTF8String(this ReadOnlySpan<byte> span, Encoding defaultEncoding)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        var resultString = string.Create(span.Length, span, static (destination, source) =>
        {
            if (Ascii.ToUtf16(source, destination, out var written) != OperationStatus.Done)
            {
                // Mark resultString for UTF-8 encoding
                destination[0] = '\0';
            }

            Debug.Assert(destination[0] == '\0' || written == destination.Length);
        });

        // If resultString is marked, perform UTF-8 encoding
        if (resultString[0] == '\0')
        {
            try
            {
                resultString = defaultEncoding.GetString(span);
            }
            catch (DecoderFallbackException)
            {
                throw new InvalidOperationException();
            }
        }

        return resultString;
    }

    // Null checks must be done independently of this method (if required)
    public static string GetAsciiString(this ReadOnlySpan<byte> span)
    {
        return string.Create(span.Length, span, static (destination, source) =>
        {
            if (Ascii.ToUtf16(source, destination, out var written) != OperationStatus.Done)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(written == destination.Length);
        });
    }

    // Null checks must be done independently of this method (if required)
    public static string GetLatin1String(this ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        return Encoding.Latin1.GetString(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool BytesOrdinalEqualsStringAndAscii(string previousValue, ReadOnlySpan<byte> newValue)
    {
        // previousValue is a previously materialized string which *must* have already passed validation.
        Debug.Assert(IsValidHeaderString(previousValue));

        return Ascii.Equals(previousValue, newValue);
    }

    private static bool IsValidHeaderString(string value)
    {
        // Method for Debug.Assert to ensure BytesOrdinalEqualsStringAndAscii
        // is not called with an unvalidated string comparitor.
        try
        {
            if (value is null)
            {
                return false;
            }
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetByteCount(value);
            return !value.Contains('\0');
        }
        catch (ArgumentOutOfRangeException)
        {
            return false; // 'value' too large to compute a UTF-8 byte count
        }
        catch (EncoderFallbackException)
        {
            return false; // 'value' cannot be converted losslessly to UTF-8
        }
    }

    /// <summary>
    /// A faster version of String.Concat(<paramref name="str"/>, <paramref name="separator"/>, <paramref name="number"/>.ToString("X8"))
    /// </summary>
    /// <param name="str"></param>
    /// <param name="separator"></param>
    /// <param name="number"></param>
    /// <returns></returns>
    public static string ConcatAsHexSuffix(string str, char separator, uint number)
    {
        var length = 1 + 8;
        if (str != null)
        {
            length += str.Length;
        }

        return string.Create(length, (str, separator, number), s_populateSpanWithHexSuffix);
    }

    private static readonly SpanAction<char, (string? str, char separator, uint number)> s_populateSpanWithHexSuffix = (Span<char> buffer, (string? str, char separator, uint number) tuple) =>
    {
        var (tupleStr, tupleSeparator, tupleNumber) = tuple;

        var i = 0;
        if (tupleStr != null)
        {
            tupleStr.AsSpan().CopyTo(buffer);
            i = tupleStr.Length;
        }

        buffer[i] = tupleSeparator;
        i++;

        if (Ssse3.IsSupported)
        {
            // The constant inline vectors are read from the data section without any additional
            // moves. See https://github.com/dotnet/runtime/issues/44115 Case 1.1 for further details.

            var lowNibbles = Ssse3.Shuffle(Vector128.CreateScalarUnsafe(tupleNumber).AsByte(), Vector128.Create(
                0xF, 0xF, 3, 0xF,
                0xF, 0xF, 2, 0xF,
                0xF, 0xF, 1, 0xF,
                0xF, 0xF, 0, 0xF
            ).AsByte());

            var highNibbles = Sse2.ShiftRightLogical(Sse2.ShiftRightLogical128BitLane(lowNibbles, 2).AsInt32(), 4).AsByte();
            var indices = Sse2.And(Sse2.Or(lowNibbles, highNibbles), Vector128.Create((byte)0xF));

            // Lookup the hex values at the positions of the indices
            var hex = Ssse3.Shuffle(Vector128.Create(
                (byte)'0', (byte)'1', (byte)'2', (byte)'3',
                (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                (byte)'8', (byte)'9', (byte)'A', (byte)'B',
                (byte)'C', (byte)'D', (byte)'E', (byte)'F'
            ), indices);

            // The high bytes (0x00) of the chars have also been converted to ascii hex '0', so clear them out.
            hex = Sse2.And(hex, Vector128.Create((ushort)0xFF).AsByte());

            // This generates much more efficient asm than fixing the buffer and using
            // Sse2.Store((byte*)(p + i), chars.AsByte());
            Unsafe.WriteUnaligned(
                ref Unsafe.As<char, byte>(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), i)),
                hex);
        }
        else
        {
            var number = (int)tupleNumber;
            // Slice the buffer so we can use constant offsets in a backwards order
            // and the highest index [7] will eliminate the bounds checks for all the lower indicies.
            buffer = buffer.Slice(i);

            // This must be explicity typed as ReadOnlySpan<byte>
            // This then becomes a non-allocating mapping to the data section of the assembly.
            // If it is a var, Span<byte> or byte[], it allocates the byte array per call.
            ReadOnlySpan<byte> hexEncodeMap = "0123456789ABCDEF"u8;
            // Note: this only works with byte due to endian ambiguity for other types,
            // hence the later (char) casts

            buffer[7] = (char)hexEncodeMap[number & 0xF];
            buffer[6] = (char)hexEncodeMap[(number >> 4) & 0xF];
            buffer[5] = (char)hexEncodeMap[(number >> 8) & 0xF];
            buffer[4] = (char)hexEncodeMap[(number >> 12) & 0xF];
            buffer[3] = (char)hexEncodeMap[(number >> 16) & 0xF];
            buffer[2] = (char)hexEncodeMap[(number >> 20) & 0xF];
            buffer[1] = (char)hexEncodeMap[(number >> 24) & 0xF];
            buffer[0] = (char)hexEncodeMap[(number >> 28) & 0xF];
        }
    };
}
