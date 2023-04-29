// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

#nullable enable
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal static class StringUtilities
{
    private static readonly SpanAction<char, IntPtr> s_getLatin1StringNonNullCharacters = GetLatin1StringNonNullCharacters;
    private static readonly SpanAction<char, (string? str, char separator, uint number)> s_populateSpanWithHexSuffix = PopulateSpanWithHexSuffix;

    public static unsafe string GetAsciiOrUTF8StringNonNullCharacters(this ReadOnlySpan<byte> span, Encoding defaultEncoding)
    {
        var resultString = string.Create(span.Length, (IntPtr)(&span), (destination, spanPtr) =>
        {
            if (Ascii.ToUtf16(*(ReadOnlySpan<byte>*)spanPtr, destination, out _) != OperationStatus.Done)
            {
                // Mark resultString for UTF-8 encoding
                destination[0] = '\0';
            }
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

    public static unsafe string GetAsciiStringNonNullCharacters(this ReadOnlySpan<byte> span)
    {
        return string.Create(span.Length, (IntPtr)(&span), (destination, spanPtr) =>
        {
            if (Ascii.ToUtf16(*(ReadOnlySpan<byte>*)spanPtr, destination, out _) != OperationStatus.Done)
            {
                throw new InvalidOperationException();
            }
        });
    };

    public static unsafe string GetLatin1StringNonNullCharacters(this ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        fixed (byte* source = &MemoryMarshal.GetReference(span))
        {
            return string.Create(span.Length, (IntPtr)source, s_getLatin1StringNonNullCharacters);
        }
    }

    private static readonly unsafe SpanAction<char, IntPtr> s_getLatin1StringNonNullCharacters = (Span<char> buffer, IntPtr state) =>
    {
        fixed (char* output = &MemoryMarshal.GetReference(buffer))
        {
            if (!TryGetLatin1String((byte*)state.ToPointer(), output, buffer.Length))
            {
                // null characters are considered invalid
                throw new InvalidOperationException();
            }
        }
    };

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe bool TryGetLatin1String(byte* input, char* output, int count)
    {
        Debug.Assert(input != null);
        Debug.Assert(output != null);

        // Calculate end position
        var end = input + count;
        // Start as valid
        var isValid = true;

        do
        {
            // If Vector not-accelerated or remaining less than vector size
            if (!Vector.IsHardwareAccelerated || input > end - Vector<sbyte>.Count)
            {
                if (Environment.Is64BitProcess) // Use Intrinsic switch for branch elimination
                {
                    // 64-bit: Loop longs by default
                    while (input <= end - sizeof(long))
                    {
                        isValid &= CheckBytesNotNull(((long*)input)[0]);

                        output[0] = (char)input[0];
                        output[1] = (char)input[1];
                        output[2] = (char)input[2];
                        output[3] = (char)input[3];
                        output[4] = (char)input[4];
                        output[5] = (char)input[5];
                        output[6] = (char)input[6];
                        output[7] = (char)input[7];

                        input += sizeof(long);
                        output += sizeof(long);
                    }
                    if (input <= end - sizeof(int))
                    {
                        isValid &= CheckBytesNotNull(((int*)input)[0]);

                        output[0] = (char)input[0];
                        output[1] = (char)input[1];
                        output[2] = (char)input[2];
                        output[3] = (char)input[3];

                        input += sizeof(int);
                        output += sizeof(int);
                    }
                }
                else
                {
                    // 32-bit: Loop ints by default
                    while (input <= end - sizeof(int))
                    {
                        isValid &= CheckBytesNotNull(((int*)input)[0]);

                        output[0] = (char)input[0];
                        output[1] = (char)input[1];
                        output[2] = (char)input[2];
                        output[3] = (char)input[3];

                        input += sizeof(int);
                        output += sizeof(int);
                    }
                }
                if (input <= end - sizeof(short))
                {
                    isValid &= CheckBytesNotNull(((short*)input)[0]);

                    output[0] = (char)input[0];
                    output[1] = (char)input[1];

                    input += sizeof(short);
                    output += sizeof(short);
                }
                if (input < end)
                {
                    isValid &= CheckBytesNotNull(((sbyte*)input)[0]);
                    output[0] = (char)input[0];
                }

                return isValid;
            }

            // do/while as entry condition already checked
            do
            {
                // Use byte/ushort instead of signed equivalents to ensure it doesn't fill based on the high bit.
                var vector = Unsafe.AsRef<Vector<byte>>(input);
                isValid &= CheckBytesNotNull(vector);
                Vector.Widen(
                    vector,
                    out Unsafe.AsRef<Vector<ushort>>(output),
                    out Unsafe.AsRef<Vector<ushort>>(output + Vector<ushort>.Count));

                input += Vector<byte>.Count;
                output += Vector<byte>.Count;
            } while (input <= end - Vector<byte>.Count);

            // Vector path done, loop back to do non-Vector
            // If is a exact multiple of vector size, bail now
        } while (input < end);

        return isValid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BytesOrdinalEqualsStringAndAscii(string previousValue, ReadOnlySpan<byte> newValue)
    {
        // previousValue is a previously materialized string which *must* have already passed validation.
        Debug.Assert(IsValidHeaderString(previousValue));

        return Ascii.Equals(previousValue, newValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WidenFourAsciiBytesToUtf16AndWriteToBuffer(char* output, byte* input, int value, Vector128<sbyte> zero)
    {
        // BMI2 could be used, but this variant is faster on both Intel and AMD.
        if (Sse2.X64.IsSupported)
        {
            var vecNarrow = Sse2.ConvertScalarToVector128Int32(value).AsSByte();
            var vecWide = Sse2.UnpackLow(vecNarrow, zero).AsUInt64();
            Unsafe.WriteUnaligned(output, Sse2.X64.ConvertToUInt64(vecWide));
        }
        else
        {
            output[0] = (char)input[0];
            output[1] = (char)input[1];
            output[2] = (char)input[2];
            output[3] = (char)input[3];
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
    private static bool CheckBytesInAsciiRange(Vector<sbyte> check)
    {
        // Vectorized byte range check, signed byte > 0 for 1-127
        return Vector.GreaterThanAll(check, Vector<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckBytesInAsciiRange(Vector256<sbyte> check, Vector256<sbyte> zero)
    {
        Debug.Assert(Avx2.IsSupported);

        var mask = Avx2.CompareGreaterThan(check, zero);
        return (uint)Avx2.MoveMask(mask) == 0xFFFF_FFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckBytesInAsciiRange(Vector128<sbyte> check, Vector128<sbyte> zero)
    {
        Debug.Assert(Sse2.IsSupported);

        var mask = Sse2.CompareGreaterThan(check, zero);
        return Sse2.MoveMask(mask) == 0xFFFF;
    }

    // Validate: bytes != 0 && bytes <= 127
    //  Subtract 1 from all bytes to move 0 to high bits
    //  bitwise or with self to catch all > 127 bytes
    //  mask off non high bits and check if 0

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
    private static bool CheckBytesInAsciiRange(long check)
    {
        const long HighBits = unchecked((long)0x8080808080808080L);
        return (((check - 0x0101010101010101L) | check) & HighBits) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckBytesInAsciiRange(int check)
    {
        const int HighBits = unchecked((int)0x80808080);
        return (((check - 0x01010101) | check) & HighBits) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckBytesInAsciiRange(short check)
    {
        const short HighBits = unchecked((short)0x8080);
        return (((short)(check - 0x0101) | check) & HighBits) == 0;
    }

    private static bool CheckBytesInAsciiRange(sbyte check)
        => check > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
    private static bool CheckBytesNotNull(Vector<byte> check)
    {
        // Vectorized byte range check, signed byte != null
        return !Vector.EqualsAny(check, Vector<byte>.Zero);
    }

    // Validate: bytes != 0
    //  Subtract 1 from all bytes to move 0 to high bits
    //  bitwise and with ~check so high bits are only set for bytes that were originally 0
    //  mask off non high bits and check if 0

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
    private static bool CheckBytesNotNull(long check)
    {
        const long HighBits = unchecked((long)0x8080808080808080L);
        return ((check - 0x0101010101010101L) & ~check & HighBits) == 0;
    }

    private static bool CheckBytesNotNull(int check)
    {
        const int HighBits = unchecked((int)0x80808080);
        return ((check - 0x01010101) & ~check & HighBits) == 0;
    }

    private static bool CheckBytesNotNull(short check)
    {
        const short HighBits = unchecked((short)0x8080);
        return ((check - 0x0101) & ~check & HighBits) == 0;
    }

    private static bool CheckBytesNotNull(sbyte check)
        => check != 0;
}
