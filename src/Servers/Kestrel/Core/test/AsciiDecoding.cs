// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class AsciiDecodingTests : LoggedTest
    {
        [Fact]
        private void FullAsciiRangeSupported()
        {
            var byteRange = Enumerable.Range(1, 127).Select(x => (byte)x);

            var byteArray = byteRange
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .ToArray();

            var span = new Span<byte>(byteArray);

            for (var i = 0; i <= byteArray.Length; i++)
            {
                // Test all the lengths to hit all the different length paths e.g. Vector, long, short, char
                Test(span.Slice(i));
            }

            static void Test(Span<byte> asciiBytes)
            {
                var s = asciiBytes.GetAsciiStringNonNullCharacters();

                Assert.True(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, asciiBytes));
                Assert.Equal(s.Length, asciiBytes.Length);

                for (var i = 0; i < asciiBytes.Length; i++)
                {
                    var sb = (byte)s[i];
                    var b = asciiBytes[i];

                    Assert.Equal(sb, b);
                }
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x80)]
        private void ExceptionThrownForZeroOrNonAscii(byte b)
        {
            for (var length = 1; length < Vector<sbyte>.Count * 4; length++)
            {
                for (var position = 0; position < length; position++)
                {
                    var byteRange = Enumerable.Range(1, length).Select(x => (byte)x).ToArray();
                    byteRange[position] = b;

                    Assert.Throws<InvalidOperationException>(() => new Span<byte>(byteRange).GetAsciiStringNonNullCharacters());
                }
            }
        }

        [Fact]
        private void LargeAllocationProducesCorrectResults()
        {
            var byteRange = Enumerable.Range(0, 16384 + 64).Select(x => (byte)((x & 0x7f) | 0x01)).ToArray();
            var expectedByteRange = byteRange.Concat(byteRange).ToArray();

            var span = new Span<byte>(expectedByteRange);
            var s = span.GetAsciiStringNonNullCharacters();

            Assert.Equal(expectedByteRange.Length, s.Length);

            for (var i = 0; i < expectedByteRange.Length; i++)
            {
                var sb = (byte)((s[i] & 0x7f) | 0x01);
                var b = expectedByteRange[i];
            }

            Assert.True(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, span));
        }

        [Fact]
        private void DifferentLengthsAreNotEqual()
        {
            var byteRange = Enumerable.Range(0, 4096).Select(x => (byte)((x & 0x7f) | 0x01)).ToArray();
            var expectedByteRange = byteRange.Concat(byteRange).ToArray();

            for (var i = 1; i < byteRange.Length; i++)
            {
                var span = new Span<byte>(expectedByteRange);
                var s = span.GetAsciiStringNonNullCharacters();

                Assert.True(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, span));

                // One off end
                Assert.False(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, span.Slice(0, span.Length - 1)));
                Assert.False(StringUtilities.BytesOrdinalEqualsStringAndAscii(s.Substring(0, s.Length - 1), span));

                // One off start
                Assert.False(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, span.Slice(1, span.Length - 1)));
                Assert.False(StringUtilities.BytesOrdinalEqualsStringAndAscii(s.Substring(1, s.Length - 1), span));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllBytesInUInt32AreAscii(uint value)
        {
            return ((value & 0x80808080u) == 0);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllBytesInUInt16AreAscii(ushort value)
        {
            return ((value & 0x8080u) == 0);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
        private static bool CheckBytesInAsciiRange(Vector<sbyte> check)
        {
            // Vectorized byte range check, signed byte > 0 for 1-127
            return Vector.GreaterThanAll(check, Vector<sbyte>.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool BytesOrdinalEqualsStringAndAsciiCustom(string previousValue, ReadOnlySpan<byte> newValue, Extensions.Logging.ILogger logger)
        {
            // previousValue is a previously materialized string which *must* have already passed validation.
            //Debug.Assert(IsValidHeaderString(previousValue));

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
                                logger.LogInformation("1");
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
                                logger.LogInformation("2");
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
                                logger.LogInformation("3");
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
                            logger.LogInformation("4");
                            goto NotEqual;
                        }

                        offset += sizeof(short);
                    }
                    if (offset < count)
                    {
                        var ch = (char)Unsafe.Add(ref bytes, offset);
                        if (((ch & 0x80) != 0) || Unsafe.Add(ref str, offset) != ch)
                        {
                            logger.LogInformation("5");
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
                        logger.LogInformation("6");
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
                        logger.LogInformation("7");
                        logger.LogInformation($"offset: {offset}, vector: {vector}, compare0: {compare0}, vector0: {vector0}, compare1: {compare1}, vector1: {vector1}");
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

        [Fact]
        [Repeat(50000)]
        private void AsciiBytesEqualAsciiStrings()
        {
            var byteRange = Enumerable.Range(1, 127).Select(x => (byte)x);

            var byteArray = byteRange
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .ToArray();

            var span = new Span<byte>(byteArray);

            for (var i = 0; i <= byteArray.Length; i++)
            {
                // Test all the lengths to hit all the different length paths e.g. Vector, long, short, char
                Test(span.Slice(i), Logger);
            }

            static void Test(Span<byte> asciiBytes, Extensions.Logging.ILogger Logger)
            {
                var s = asciiBytes.GetAsciiStringNonNullCharacters();

                // Should start as equal
                Assert.True(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, asciiBytes));

                for (var i = 0; i < asciiBytes.Length; i++)
                {
                    var b = asciiBytes[i];

                    // Change one byte, ensure is not equal
                    asciiBytes[i] = (byte)(b + 1);
                    Assert.False(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, asciiBytes));

                    // Change byte back for next iteration, ensure is equal again
                    asciiBytes[i] = b;
                    var result = BytesOrdinalEqualsStringAndAsciiCustom(s, asciiBytes, Logger);
                    if (!result)
                    {
                        Assert.True(result, $"Ordinal string comparison: {string.Equals(s, Encoding.ASCII.GetString(asciiBytes), StringComparison.Ordinal)}" +
                            $"\nExpected length:{s.Length}" +
                            $"\nExpected string:{EscapeControlCodesString(s)}" +
                            $"\nActual length:{asciiBytes.Length}" +
                            $"\nActual string:{EscapeControlCodesBytes(asciiBytes)}");
                    }
                }
            }

            static string EscapeControlCodesString(string input)
            {
                var builder = new StringBuilder();

                foreach (var c in input)
                {
                    builder.Append(EscapeControlCode((byte)c));
                }

                return builder.ToString();
            }

            static string EscapeControlCodesBytes(Span<byte> input)
            {
                var builder = new StringBuilder();

                foreach (var c in input)
                {
                    builder.Append(EscapeControlCode(c));
                }

                return builder.ToString();
            }

            static string EscapeControlCode(byte c) => "0x" + c.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
