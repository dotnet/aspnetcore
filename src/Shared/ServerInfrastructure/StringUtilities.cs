// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal static class StringUtilities
{
    private static readonly SpanAction<char, IntPtr> s_getAsciiOrUTF8StringNonNullCharacters = GetAsciiStringNonNullCharactersWithMarker;
    private static readonly SpanAction<char, IntPtr> s_getAsciiStringNonNullCharacters = GetAsciiStringNonNullCharacters;
    private static readonly SpanAction<char, IntPtr> s_getLatin1StringNonNullCharacters = GetLatin1StringNonNullCharacters;
    private static readonly SpanAction<char, (string? str, char separator, uint number)> s_populateSpanWithHexSuffix = PopulateSpanWithHexSuffix;

    public static unsafe string GetAsciiOrUTF8StringNonNullCharacters(this ReadOnlySpan<byte> span, Encoding defaultEncoding)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        fixed (byte* source = &MemoryMarshal.GetReference(span))
        {
            var resultString = string.Create(span.Length, (IntPtr)source, s_getAsciiOrUTF8StringNonNullCharacters);

            // If resultString is marked, perform UTF-8 encoding
            if (resultString[0] == '\0')
            {
                // null characters are considered invalid
                if (span.IndexOf((byte)0) != -1)
                {
                    throw new InvalidOperationException();
                }

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
    }

    private static unsafe void GetAsciiStringNonNullCharactersWithMarker(Span<char> buffer, IntPtr state)
    {
        fixed (char* output = &MemoryMarshal.GetReference(buffer))
        {
            // This version of AsciiUtilities returns false if there are any null ('\0') or non-Ascii
            // character (> 127) in the string.
            if (!TryGetAsciiString((byte*)state.ToPointer(), output, buffer.Length))
            {
                // Mark resultString for UTF-8 encoding
                output[0] = '\0';
            }
        }
    }

    public static unsafe string GetAsciiStringNonNullCharacters(this ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }

        fixed (byte* source = &MemoryMarshal.GetReference(span))
        {
            return string.Create(span.Length, (IntPtr)source, s_getAsciiStringNonNullCharacters);
        }
    }

    private static unsafe void GetAsciiStringNonNullCharacters(Span<char> buffer, IntPtr state)
    {
        fixed (char* output = &MemoryMarshal.GetReference(buffer))
        {
            // This version of AsciiUtilities returns false if there are any null ('\0') or non-Ascii
            // character (> 127) in the string.
            if (!TryGetAsciiString((byte*)state.ToPointer(), output, buffer.Length))
            {
                throw new InvalidOperationException();
            }
        }
    }

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

    private static unsafe void GetLatin1StringNonNullCharacters(Span<char> buffer, IntPtr state)
    {
        fixed (char* output = &MemoryMarshal.GetReference(buffer))
        {
            if (!TryGetLatin1String((byte*)state.ToPointer(), output, buffer.Length))
            {
                // null characters are considered invalid
                throw new InvalidOperationException();
            }
        }
    }

    public static unsafe bool TryGetAsciiString(byte* input, char* output, int count)
    {
        Debug.Assert(input != null);
        Debug.Assert(output != null);

        var end = input + count;

        Debug.Assert((long)end >= Vector256<sbyte>.Count);

        // PERF: so the JIT can reuse the zero from a register
        var zero = Vector128<sbyte>.Zero;

        if (Vector128.IsHardwareAccelerated && input <= end - Vector128<sbyte>.Count)
        {
            if (Vector256.IsHardwareAccelerated && input <= end - Vector256<sbyte>.Count)
            {
                var avxZero = Vector256<sbyte>.Zero;

                do
                {
                    var vector = Vector256.Load(input).AsSByte();
                    if (!CheckBytesInAsciiRange(vector, avxZero))
                    {
                        return false;
                    }

                    var (out0, out1) = Vector256.Widen(vector);

                    out0.Store((short*)output);
                    out1.Store((short*)output + Vector256<short>.Count);

                    input += Vector256<sbyte>.Count;
                    output += 2 * Vector256<short>.Count;
                } while (input <= end - Vector256<sbyte>.Count);

                if (input == end)
                {
                    return true;
                }
            }

            if (input <= end - Vector128<sbyte>.Count)
            {
                do
                {
                    var vector = Vector128.Load(input).AsSByte();
                    if (!CheckBytesInAsciiRange(vector, zero))
                    {
                        return false;
                    }

                    var (out0, out1) = Vector128.Widen(vector);

                    out0.Store((short*)output);
                    out1.Store((short*)output + Vector128<short>.Count);

                    input += Vector128<sbyte>.Count;
                    output += 2 * Vector128<short>.Count;
                } while (input <= end - Vector128<sbyte>.Count);

                if (input == end)
                {
                    return true;
                }
            }

            // Here we know that from the end at least Vector128<sbyte>.Count elements exists.
            // The operation is idempotent, so we can perform one read from the very end instead
            // of doing the scalar paths below.
            {
                var adjust = Vector128<sbyte>.Count - (end - input);
                Debug.Assert(adjust > 0 && adjust < Vector128<sbyte>.Count);

                input -= adjust;
                output -= adjust;

                var vector = Vector128.Load(input).AsSByte();
                if (!CheckBytesInAsciiRange(vector, zero))
                {
                    return false;
                }

                var (out0, out1) = Vector128.Widen(vector);

                out0.Store((short*)output);
                out1.Store((short*)output + Vector128<short>.Count);

                return true;
            }
        }

        if (Environment.Is64BitProcess) // Use Intrinsic switch for branch elimination
        {
            // 64-bit: Loop longs by default
            while (input <= end - sizeof(long))
            {
                var value = *(long*)input;
                if (!CheckBytesInAsciiRange(value))
                {
                    return false;
                }

                // BMI2 could be used, but this variant is faster on both Intel and AMD.
                if (Vector128.IsHardwareAccelerated)
                {
                    var vecNarrow = Vector128.CreateScalarUnsafe(value).AsSByte();
                    var vecWide = Vector128.Widen(vecNarrow).Lower.AsUInt64();
                    vecWide.Store((ulong*)output);
                }
                else
                {
                    output[0] = (char)input[0];
                    output[1] = (char)input[1];
                    output[2] = (char)input[2];
                    output[3] = (char)input[3];
                    output[4] = (char)input[4];
                    output[5] = (char)input[5];
                    output[6] = (char)input[6];
                    output[7] = (char)input[7];
                }

                input += sizeof(long);
                output += sizeof(long);
            }

            if (input <= end - sizeof(int))
            {
                var value = *(int*)input;
                if (!CheckBytesInAsciiRange(value))
                {
                    return false;
                }

                WidenFourAsciiBytesToUtf16AndWriteToBuffer(output, input, value);

                input += sizeof(int);
                output += sizeof(int);
            }
        }
        else
        {
            // 32-bit: Loop ints by default
            while (input <= end - sizeof(int))
            {
                var value = *(int*)input;
                if (!CheckBytesInAsciiRange(value))
                {
                    return false;
                }

                WidenFourAsciiBytesToUtf16AndWriteToBuffer(output, input, value);

                input += sizeof(int);
                output += sizeof(int);
            }
        }

        if (input <= end - sizeof(short))
        {
            if (!CheckBytesInAsciiRange(((short*)input)[0]))
            {
                return false;
            }

            output[0] = (char)input[0];
            output[1] = (char)input[1];

            input += sizeof(short);
            output += sizeof(short);
        }

        if (input < end)
        {
            if (!CheckBytesInAsciiRange(((sbyte*)input)[0]))
            {
                return false;
            }
            output[0] = (char)input[0];
        }

        return true;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool BytesOrdinalEqualsStringAndAscii(string previousValue, ReadOnlySpan<byte> newValue)
    {
        // previousValue is a previously materialized string which *must* have already passed validation.
        Debug.Assert(IsValidHeaderString(previousValue));

        // Ascii bytes => Utf-16 chars will be the same length.
        // The caller should have already compared lengths before calling this method.
        // However; let's double check, and early exit if they are not the same length.
        if (previousValue.Length != newValue.Length)
        {
            // Lengths don't match, so there cannot be an exact ascii conversion between the two.
            goto NotEqual;
        }

        // Note: Pointer comparison is unsigned, so we use the compare pattern (offset + length <= count)
        // rather than (offset <= count - length) which we'd do with signed comparison to avoid overflow.
        // This isn't problematic as we know the maximum length is max string length (from test above)
        // which is a signed value so half the size of the unsigned pointer value so we can safely add
        // a Vector<byte>.Count to it without overflowing.
        var count = (nint)newValue.Length;
        var offset = (nint)0;

        // Get references to the first byte in the span, and the first char in the string.
        ref var bytes = ref MemoryMarshal.GetReference(newValue);
        ref var str = ref MemoryMarshal.GetReference(previousValue.AsSpan());

        do
        {
            // If Vector not-accelerated or remaining less than vector size
            if (!Vector.IsHardwareAccelerated || (offset + Vector<byte>.Count) > count)
            {
                if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                {
                    // 64-bit: Loop longs by default
                    while ((offset + sizeof(long)) <= count)
                    {
                        if (!WidenFourAsciiBytesToUtf16AndCompareToChars(
                                ref Unsafe.Add(ref str, offset),
                                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref bytes, offset))) ||
                            !WidenFourAsciiBytesToUtf16AndCompareToChars(
                                ref Unsafe.Add(ref str, offset + 4),
                                Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref bytes, offset + 4))))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(long);
                    }
                    if ((offset + sizeof(int)) <= count)
                    {
                        if (!WidenFourAsciiBytesToUtf16AndCompareToChars(
                            ref Unsafe.Add(ref str, offset),
                            Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref bytes, offset))))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(int);
                    }
                }
                else
                {
                    // 32-bit: Loop ints by default
                    while ((offset + sizeof(int)) <= count)
                    {
                        if (!WidenFourAsciiBytesToUtf16AndCompareToChars(
                            ref Unsafe.Add(ref str, offset),
                            Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref bytes, offset))))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(int);
                    }
                }
                if ((offset + sizeof(short)) <= count)
                {
                    if (!WidenTwoAsciiBytesToUtf16AndCompareToChars(
                        ref Unsafe.Add(ref str, offset),
                        Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref bytes, offset))))
                    {
                        goto NotEqual;
                    }

                    offset += sizeof(short);
                }
                if (offset < count)
                {
                    var ch = (char)Unsafe.Add(ref bytes, offset);
                    if (((ch & 0x80) != 0) || Unsafe.Add(ref str, offset) != ch)
                    {
                        goto NotEqual;
                    }
                }

                // End of input reached, there are no inequalities via widening; so the input bytes are both ascii
                // and a match to the string if it was converted via Encoding.ASCII.GetString(...)
                return true;
            }

            // Create a comparision vector for all bits being equal
            var AllTrue = new Vector<short>(-1);
            // do/while as entry condition already checked, remaining length must be Vector<byte>.Count or larger.
            do
            {
                // Read a Vector length from the input as bytes
                var vector = Unsafe.ReadUnaligned<Vector<sbyte>>(ref Unsafe.Add(ref bytes, offset));
                if (!CheckBytesInAsciiRange(vector))
                {
                    goto NotEqual;
                }
                // Widen the bytes directly to chars (ushort) as if they were ascii.
                // As widening doubles the size we get two vectors back.
                Vector.Widen(vector, out var vector0, out var vector1);
                // Read two char vectors from the string to perform the match.
                var compare0 = Unsafe.ReadUnaligned<Vector<short>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref str, offset)));
                var compare1 = Unsafe.ReadUnaligned<Vector<short>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref str, offset + Vector<ushort>.Count)));

                // If the string is not ascii, then the widened bytes cannot match
                // as each widened byte element as chars will be in the range 0-255
                // so cannot match any higher unicode values.

                // Compare to our all bits true comparision vector
                if (!AllTrue.Equals(
                    // BitwiseAnd the two equals together
                    Vector.BitwiseAnd(
                        // Check equality for the two widened vectors
                        Vector.Equals(compare0, vector0),
                        Vector.Equals(compare1, vector1))))
                {
                    goto NotEqual;
                }

                offset += Vector<byte>.Count;
            } while ((offset + Vector<byte>.Count) <= count);

            // Vector path done, loop back to do non-Vector
            // If is a exact multiple of vector size, bail now
        } while (offset < count);

        // If we get here (input is exactly a multiple of Vector length) then there are no inequalities via widening;
        // so the input bytes are both ascii and a match to the string if it was converted via Encoding.ASCII.GetString(...)
        return true;
    NotEqual:
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WidenFourAsciiBytesToUtf16AndWriteToBuffer(char* output, byte* input, int value)
    {
        // BMI2 could be used, but this variant is faster on both Intel and AMD.
        if (Vector128.IsHardwareAccelerated)
        {
            var vecNarrow = Vector128.CreateScalarUnsafe(value).AsSByte();
            var vecWide = Vector128.Widen(vecNarrow).Lower.AsUInt64();
            Unsafe.WriteUnaligned(output, vecWide.ToScalar());
        }
        else
        {
            output[0] = (char)input[0];
            output[1] = (char)input[1];
            output[2] = (char)input[2];
            output[3] = (char)input[3];
        }
    }

    /// <summary>
    /// Given a DWORD which represents a buffer of 4 bytes, widens the buffer into 4 WORDs and
    /// compares them to the WORD buffer with machine endianness.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool WidenFourAsciiBytesToUtf16AndCompareToChars(ref char charStart, uint value)
    {
        if (!AllBytesInUInt32AreAscii(value))
        {
            return false;
        }

        // BMI2 could be used, but this variant is faster on both Intel and AMD.
        if (Vector128.IsHardwareAccelerated)
        {
            var vecNarrow = Vector128.CreateScalarUnsafe(value).AsSByte();
            var vecWide = Vector128.Widen(vecNarrow).Lower.AsUInt64();
            return Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref charStart)) ==
                vecWide.ToScalar();
        }
        else
        {
            if (BitConverter.IsLittleEndian)
            {
                return charStart == (char)(byte)value &&
                    Unsafe.Add(ref charStart, 1) == (char)(byte)(value >> 8) &&
                    Unsafe.Add(ref charStart, 2) == (char)(byte)(value >> 16) &&
                    Unsafe.Add(ref charStart, 3) == (char)(value >> 24);
            }
            else
            {
                return Unsafe.Add(ref charStart, 3) == (char)(byte)value &&
                    Unsafe.Add(ref charStart, 2) == (char)(byte)(value >> 8) &&
                    Unsafe.Add(ref charStart, 1) == (char)(byte)(value >> 16) &&
                    charStart == (char)(value >> 24);
            }
        }
    }

    /// <summary>
    /// Given a WORD which represents a buffer of 2 bytes, widens the buffer into 2 WORDs and
    /// compares them to the WORD buffer with machine endianness.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool WidenTwoAsciiBytesToUtf16AndCompareToChars(ref char charStart, ushort value)
    {
        if (!AllBytesInUInt16AreAscii(value))
        {
            return false;
        }

        // BMI2 could be used, but this variant is faster on both Intel and AMD.
        if (Vector128.IsHardwareAccelerated)
        {
            var vecNarrow = Vector128.CreateScalarUnsafe(value).AsSByte();
            var vecWide = Vector128.Widen(vecNarrow).Lower.AsUInt32();
            return Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref charStart)) ==
                vecWide.ToScalar();
        }
        else
        {
            if (BitConverter.IsLittleEndian)
            {
                return charStart == (char)(byte)value &&
                    Unsafe.Add(ref charStart, 1) == (char)(byte)(value >> 8);
            }
            else
            {
                return Unsafe.Add(ref charStart, 1) == (char)(byte)value &&
                    charStart == (char)(byte)(value >> 8);
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> iff all bytes in <paramref name="value"/> are ASCII.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllBytesInUInt32AreAscii(uint value)
    {
        return ((value & 0x80808080u) == 0);
    }

    /// <summary>
    /// Returns <see langword="true"/> iff all bytes in <paramref name="value"/> are ASCII.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllBytesInUInt16AreAscii(ushort value)
    {
        return ((value & 0x8080u) == 0);
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
        catch (DecoderFallbackException)
        {
            return false;
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

    private static void PopulateSpanWithHexSuffix(Span<char> buffer, (string? str, char separator, uint number) tuple)
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
    }

    // Vectorized byte range check, signed byte > 0 for 1-127

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
    private static bool CheckBytesInAsciiRange(Vector<sbyte> check)
    {
        // JIT will dead-code eliminated not needed branches.

        if (Vector256.IsHardwareAccelerated && Vector<sbyte>.Count == Vector256<sbyte>.Count)
        {
            return CheckBytesInAsciiRange(check.AsVector256(), Vector256<sbyte>.Zero);
        }

        if (Vector128.IsHardwareAccelerated && Vector<sbyte>.Count == Vector128<sbyte>.Count)
        {
            return CheckBytesInAsciiRange(check.AsVector128(), Vector128<sbyte>.Zero);
        }

        // Fallback safety net.
        return Vector.GreaterThanAll(check, Vector<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckBytesInAsciiRange(Vector256<sbyte> check, Vector256<sbyte> zero)
    {
        Debug.Assert(Vector256.IsHardwareAccelerated);

        var mask = Vector256.GreaterThan(check, zero);
        return mask.ExtractMostSignificantBits() == 0xFFFF_FFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckBytesInAsciiRange(Vector128<sbyte> check, Vector128<sbyte> zero)
    {
        Debug.Assert(Vector128.IsHardwareAccelerated);

        var mask = Vector128.GreaterThan(check, zero);
        return mask.ExtractMostSignificantBits() == 0xFFFF;
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
