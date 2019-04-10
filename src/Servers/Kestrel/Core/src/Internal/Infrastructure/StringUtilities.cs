// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class StringUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe bool TryGetAsciiString(byte* input, char* output, int count)
        {
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
                            isValid &= CheckBytesInAsciiRange(((long*)input)[0]);

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
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

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
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

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
                        isValid &= CheckBytesInAsciiRange(((short*)input)[0]);

                        output[0] = (char)input[0];
                        output[1] = (char)input[1];

                        input += sizeof(short);
                        output += sizeof(short);
                    }
                    if (input < end)
                    {
                        isValid &= CheckBytesInAsciiRange(((sbyte*)input)[0]);
                        output[0] = (char)input[0];
                    }

                    return isValid;
                }

                // do/while as entry condition already checked
                do
                {
                    var vector = Unsafe.AsRef<Vector<sbyte>>(input);
                    isValid &= CheckBytesInAsciiRange(vector);
                    Vector.Widen(
                        vector,
                        out Unsafe.AsRef<Vector<short>>(output),
                        out Unsafe.AsRef<Vector<short>>(output + Vector<short>.Count));

                    input += Vector<sbyte>.Count;
                    output += Vector<sbyte>.Count;
                } while (input <= end - Vector<sbyte>.Count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while (input < end);

            return isValid;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe static bool BytesOrdinalEqualsStringAndAscii(string previousValue, Span<byte> newValue)
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

            if (Bmi2.X64.IsSupported)
            {
                // BMI2 will work regardless of the processor's endianness.
                return Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref charStart)) ==
                    Bmi2.X64.ParallelBitDeposit(value, 0x00FF00FF_00FF00FFul);
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

            if (Bmi2.IsSupported)
            {
                // BMI2 will work regardless of the processor's endianness.
                return Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref charStart)) ==
                    Bmi2.ParallelBitDeposit(value, 0x00FF00FFu);
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
            catch (DecoderFallbackException) {
                return false;
            }
        }

        private static readonly char[] s_encode16Chars = "0123456789ABCDEF".ToCharArray();

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

            return string.Create(length, (str, separator, number), (buffer, tuple) =>
            {
                var (tupleStr, tupleSeparator, tupleNumber) = tuple;
                char[] encode16Chars = s_encode16Chars;

                var i = 0;
                if (tupleStr != null)
                {
                    tupleStr.AsSpan().CopyTo(buffer);
                    i = tupleStr.Length;
                }

                buffer[i + 8] = encode16Chars[tupleNumber & 0xF];
                buffer[i + 7] = encode16Chars[(tupleNumber >> 4) & 0xF];
                buffer[i + 6] = encode16Chars[(tupleNumber >> 8) & 0xF];
                buffer[i + 5] = encode16Chars[(tupleNumber >> 12) & 0xF];
                buffer[i + 4] = encode16Chars[(tupleNumber >> 16) & 0xF];
                buffer[i + 3] = encode16Chars[(tupleNumber >> 20) & 0xF];
                buffer[i + 2] = encode16Chars[(tupleNumber >> 24) & 0xF];
                buffer[i + 1] = encode16Chars[(tupleNumber >> 28) & 0xF];
                buffer[i] = tupleSeparator;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
        private static bool CheckBytesInAsciiRange(Vector<sbyte> check)
        {
            // Vectorized byte range check, signed byte > 0 for 1-127
            return Vector.GreaterThanAll(check, Vector<sbyte>.Zero);
        }

        // Validate: bytes != 0 && bytes <= 127
        //  Subtract 1 from all bytes to move 0 to high bits
        //  bitwise or with self to catch all > 127 bytes
        //  mask off high bits and check if 0

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
        private static bool CheckBytesInAsciiRange(long check)
        {
            const long HighBits = unchecked((long)0x8080808080808080L);
            return (((check - 0x0101010101010101L) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(int check)
        {
            const int HighBits = unchecked((int)0x80808080);
            return (((check - 0x01010101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(short check)
        {
            const short HighBits = unchecked((short)0x8080);
            return (((short)(check - 0x0101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(sbyte check)
            => check > 0;
    }
}
