// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static class StringUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe bool TryGetAsciiString(byte* input, char* output, int count)
        {
            Debug.Assert(input != null);
            Debug.Assert(output != null);

            var end = input + count;

            Debug.Assert((long)end >= Vector256<sbyte>.Count);

            // PERF: so the JIT can reuse the zero from a register
            Vector128<sbyte> zero = Vector128<sbyte>.Zero;

            if (Sse2.IsSupported)
            {
                if (Avx2.IsSupported && input <= end - Vector256<sbyte>.Count)
                {
                    Vector256<sbyte> avxZero = Vector256<sbyte>.Zero;

                    do
                    {
                        var vector = Avx.LoadVector256(input).AsSByte();
                        if (!CheckBytesInAsciiRange(vector, avxZero))
                        {
                            return false;
                        }

                        var tmp0 = Avx2.UnpackLow(vector, avxZero);
                        var tmp1 = Avx2.UnpackHigh(vector, avxZero);

                        // Bring into the right order
                        var out0 = Avx2.Permute2x128(tmp0, tmp1, 0x20);
                        var out1 = Avx2.Permute2x128(tmp0, tmp1, 0x31);

                        Avx.Store((ushort*)output, out0.AsUInt16());
                        Avx.Store((ushort*)output + Vector256<ushort>.Count, out1.AsUInt16());

                        input += Vector256<sbyte>.Count;
                        output += Vector256<sbyte>.Count;
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
                        var vector = Sse2.LoadVector128(input).AsSByte();
                        if (!CheckBytesInAsciiRange(vector, zero))
                        {
                            return false;
                        }

                        var c0 = Sse2.UnpackLow(vector, zero).AsUInt16();
                        var c1 = Sse2.UnpackHigh(vector, zero).AsUInt16();

                        Sse2.Store((ushort*)output, c0);
                        Sse2.Store((ushort*)output + Vector128<ushort>.Count, c1);

                        input += Vector128<sbyte>.Count;
                        output += Vector128<sbyte>.Count;
                    } while (input <= end - Vector128<sbyte>.Count);

                    if (input == end)
                    {
                        return true;
                    }
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                while (input <= end - Vector<sbyte>.Count)
                {
                    var vector = Unsafe.AsRef<Vector<sbyte>>(input);
                    if (!CheckBytesInAsciiRange(vector))
                    {
                        return false;
                    }

                    Vector.Widen(
                        vector,
                        out Unsafe.AsRef<Vector<short>>(output),
                        out Unsafe.AsRef<Vector<short>>(output + Vector<short>.Count));

                    input += Vector<sbyte>.Count;
                    output += Vector<sbyte>.Count;
                }

                if (input == end)
                {
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
                    if (Sse2.X64.IsSupported)
                    {
                        Vector128<sbyte> vecNarrow = Sse2.X64.ConvertScalarToVector128Int64(value).AsSByte();
                        Vector128<ulong> vecWide = Sse2.UnpackLow(vecNarrow, zero).AsUInt64();
                        Sse2.Store((ulong*)output, vecWide);
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

                    WidenFourAsciiBytesToUtf16AndWriteToBuffer(output, input, value, zero);

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

                    WidenFourAsciiBytesToUtf16AndWriteToBuffer(output, input, value, zero);

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
                    if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
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
        public unsafe static bool BytesOrdinalEqualsStringAndAscii(string previousValue, ReadOnlySpan<byte> newValue)
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

            // Use IntPtr values rather than int, to avoid unnecessary 32 -> 64 movs on 64-bit.
            // Unfortunately this means we also need to cast to byte* for comparisons as IntPtr doesn't
            // support operator comparisons (e.g. <=, >, etc).
            //
            // Note: Pointer comparison is unsigned, so we use the compare pattern (offset + length <= count)
            // rather than (offset <= count - length) which we'd do with signed comparison to avoid overflow.
            // This isn't problematic as we know the maximum length is max string length (from test above)
            // which is a signed value so half the size of the unsigned pointer value so we can safely add
            // a Vector<byte>.Count to it without overflowing.
            var count = (IntPtr)newValue.Length;
            var offset = (IntPtr)0;

            // Get references to the first byte in the span, and the first char in the string.
            ref var bytes = ref MemoryMarshal.GetReference(newValue);
            ref var str = ref MemoryMarshal.GetReference(previousValue.AsSpan());

            do
            {
                // If Vector not-accelerated or remaining less than vector size
                if (!Vector.IsHardwareAccelerated || (byte*)(offset + Vector<byte>.Count) > (byte*)count)
                {
                    if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                    {
                        // 64-bit: Loop longs by default
                        while ((byte*)(offset + sizeof(long)) <= (byte*)count)
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
                        if ((byte*)(offset + sizeof(int)) <= (byte*)count)
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
                        while ((byte*)(offset + sizeof(int)) <= (byte*)count)
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
                    if ((byte*)(offset + sizeof(short)) <= (byte*)count)
                    {
                        if (!WidenTwoAsciiBytesToUtf16AndCompareToChars(
                            ref Unsafe.Add(ref str, offset),
                            Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref bytes, offset))))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(short);
                    }
                    if ((byte*)offset < (byte*)count)
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
                } while ((byte*)(offset + Vector<byte>.Count) <= (byte*)count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while ((byte*)offset < (byte*)count);

            // If we get here (input is exactly a multiple of Vector length) then there are no inequalities via widening;
            // so the input bytes are both ascii and a match to the string if it was converted via Encoding.ASCII.GetString(...)
            return true;
        NotEqual:
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WidenFourAsciiBytesToUtf16AndWriteToBuffer(char* output, byte* input, int value, Vector128<sbyte> zero)
        {
            // BMI2 could be used, but this variant is faster on both Intel and AMD.
            if (Sse2.X64.IsSupported)
            {
                Vector128<sbyte> vecNarrow = Sse2.ConvertScalarToVector128Int32(value).AsSByte();
                Vector128<ulong> vecWide = Sse2.UnpackLow(vecNarrow, zero).AsUInt64();
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

        /// <summary>
        /// Given a DWORD which represents a buffer of 4 bytes, widens the buffer into 4 WORDs and
        /// compares them to the WORD buffer with machine endianness.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static bool WidenFourAsciiBytesToUtf16AndCompareToChars(ref char charStart, uint value)
        {
            if (!AllBytesInUInt32AreAscii(value))
            {
                return false;
            }

            // BMI2 could be used, but this variant is faster on both Intel and AMD.
            if (Sse2.X64.IsSupported)
            {
                Vector128<byte> vecNarrow = Sse2.ConvertScalarToVector128UInt32(value).AsByte();
                Vector128<ulong> vecWide = Sse2.UnpackLow(vecNarrow, Vector128<byte>.Zero).AsUInt64();
                return Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref charStart)) ==
                    Sse2.X64.ConvertToUInt64(vecWide);
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
            if (Sse2.IsSupported)
            {
                Vector128<byte> vecNarrow = Sse2.ConvertScalarToVector128UInt32(value).AsByte();
                Vector128<uint> vecWide = Sse2.UnpackLow(vecNarrow, Vector128<byte>.Zero).AsUInt32();
                return Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref charStart)) ==
                    Sse2.ConvertToUInt32(vecWide);
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

        private unsafe static bool IsValidHeaderString(string value)
        {
            // Method for Debug.Assert to ensure BytesOrdinalEqualsStringAndAscii
            // is not called with an unvalidated string comparitor.
            try
            {
                if (value is null) return false;
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetByteCount(value);
                return !value.Contains('\0');
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }
        private static readonly SpanAction<char, (string str, char separator, uint number)> s_populateSpanWithHexSuffix = PopulateSpanWithHexSuffix;

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

        private static void PopulateSpanWithHexSuffix(Span<char> buffer, (string str, char separator, uint number) tuple)
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
                // These must be explicity typed as ReadOnlySpan<byte>
                // They then become a non-allocating mappings to the data section of the assembly.
                // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static 
                ReadOnlySpan<byte> shuffleMaskData = new byte[16]
                {
                    0xF, 0xF, 3, 0xF,
                    0xF, 0xF, 2, 0xF,
                    0xF, 0xF, 1, 0xF,
                    0xF, 0xF, 0, 0xF
                };

                ReadOnlySpan<byte> asciiUpperCaseData = new byte[16]
                {
                    (byte)'0', (byte)'1', (byte)'2', (byte)'3',
                    (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                    (byte)'8', (byte)'9', (byte)'A', (byte)'B',
                    (byte)'C', (byte)'D', (byte)'E', (byte)'F'
                };

                // Load from data section memory into Vector128 registers
                var shuffleMask = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(shuffleMaskData));
                var asciiUpperCase = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(asciiUpperCaseData));

                var lowNibbles = Ssse3.Shuffle(Vector128.CreateScalarUnsafe(tupleNumber).AsByte(), shuffleMask);
                var highNibbles = Sse2.ShiftRightLogical(Sse2.ShiftRightLogical128BitLane(lowNibbles, 2).AsInt32(), 4).AsByte();
                var indices = Sse2.And(Sse2.Or(lowNibbles, highNibbles), Vector128.Create((byte)0xF));
                // Lookup the hex values at the positions of the indices
                var hex = Ssse3.Shuffle(asciiUpperCase, indices);
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
                ReadOnlySpan<byte> hexEncodeMap = new byte[] { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F' };
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
}
