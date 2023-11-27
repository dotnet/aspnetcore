// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Numerics;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class AsciiDecodingTests
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

    [Fact]
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
            Test(span.Slice(i));
        }

        static void Test(Span<byte> asciiBytes)
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
                Assert.True(StringUtilities.BytesOrdinalEqualsStringAndAscii(s, asciiBytes), s);
            }
        }
    }
}
