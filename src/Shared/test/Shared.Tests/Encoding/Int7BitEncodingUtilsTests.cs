// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Shared.Tests.Encoding;

public class Int7BitEncodingUtilsTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(0b0_1111111, 1)]
    [InlineData(0b1_0000000, 2)]
    [InlineData(0b1111111_1111111, 2)]
    [InlineData(0b1_0000000_0000000, 3)]
    [InlineData(0b1111111_1111111_1111111, 3)]
    [InlineData(0b1_0000000_0000000_0000000, 4)]
    [InlineData(0b1111111_1111111_1111111_1111111, 4)]
    [InlineData(0b1_0000000_0000000_0000000_0000000, 5)]
    [InlineData(uint.MaxValue, 5)]
    public void Measure7BitEncodedUIntLength_ReturnsExceptedLength(uint value, int expectedSize)
    {
        var actualSize = value.Measure7BitEncodedUIntLength();
        Assert.Equal(expectedSize, actualSize);
    }

    [Theory]
    [InlineData(0, new byte[] { 0x00 })]
    [InlineData(1, new byte[] { 0x01 })]
    [InlineData(127, new byte[] { 0x7F })]
    [InlineData(128, new byte[] { 0x80, 0x01 })]
    [InlineData(255, new byte[] { 0xFF, 0x01 })]
    [InlineData(256, new byte[] { 0x80, 0x02 })]
    [InlineData(16383, new byte[] { 0xFF, 0x7F })]
    [InlineData(16384, new byte[] { 0x80, 0x80, 0x01 })]
    [InlineData(2097151, new byte[] { 0xFF, 0xFF, 0x7F })]
    [InlineData(2097152, new byte[] { 0x80, 0x80, 0x80, 0x01 })]
    [InlineData(268435455, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
    [InlineData(268435456, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x01 })]
    [InlineData(int.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x07 })]
    public void Read7BitEncodedInt_DecodesCorrectly(int expected, byte[] encoded)
    {
        ReadOnlySpan<byte> source = encoded;

        var bytesConsumed = source.Read7BitEncodedInt(out var value);

        Assert.Equal(expected, value);
        Assert.Equal(encoded.Length, bytesConsumed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(255)]
    [InlineData(16383)]
    [InlineData(16384)]
    [InlineData(2097151)]
    [InlineData(2097152)]
    [InlineData(268435455)]
    [InlineData(268435456)]
    [InlineData(int.MaxValue)]
    public void Read7BitEncodedInt_RoundTripsWithWrite(int value)
    {
        Span<byte> buffer = stackalloc byte[5];

        var bytesWritten = buffer.Write7BitEncodedInt(value);

        ReadOnlySpan<byte> source = buffer.Slice(0, bytesWritten);
        var bytesConsumed = source.Read7BitEncodedInt(out var decoded);

        Assert.Equal(value, decoded);
        Assert.Equal(bytesWritten, bytesConsumed);
    }

    [Fact]
    public void Read7BitEncodedInt_WithEmptySpan_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() =>
        {
            var source = ReadOnlySpan<byte>.Empty;
            return source.Read7BitEncodedInt(out _);
        });
    }

    [Fact]
    public void Read7BitEncodedInt_WithTruncatedData_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() =>
        {
            // This represents the start of a multi-byte encoded value but is incomplete
            // 0x80 has continuation bit set, meaning more bytes should follow
            ReadOnlySpan<byte> source = [0x80];

            return source.Read7BitEncodedInt(out _);
        });
    }

    [Fact]
    public void Read7BitEncodedInt_WithOverflow_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() =>
        {
            // 6 bytes with continuation bits set would overflow a 32-bit integer
            ReadOnlySpan<byte> source = [0x80, 0x80, 0x80, 0x80, 0x80, 0x01];

            return source.Read7BitEncodedInt(out _);
        });
    }

    [Fact]
    public void Read7BitEncodedInt_WithExtraDataAfterValue_ConsumesOnlyNeededBytes()
    {
        // Value 127 followed by extra bytes
        ReadOnlySpan<byte> source = [0x7F, 0xFF, 0xFF];

        var bytesConsumed = source.Read7BitEncodedInt(out var value);

        Assert.Equal(127, value);
        Assert.Equal(1, bytesConsumed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Hello")]
    [InlineData("Hello, World!")]
    [InlineData("UTF-8: \u00e9\u00e8\u00ea")]
    public void Read7BitEncodedString_DecodesCorrectly(string expected)
    {
        var stringBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        var lengthBytes = new byte[5];
        Span<byte> lengthSpan = lengthBytes;
        var lengthSize = lengthSpan.Write7BitEncodedInt(stringBytes.Length);

        var encodedBytes = new byte[lengthSize + stringBytes.Length];
        Array.Copy(lengthBytes, 0, encodedBytes, 0, lengthSize);
        Array.Copy(stringBytes, 0, encodedBytes, lengthSize, stringBytes.Length);

        ReadOnlySpan<byte> source = encodedBytes;

        var bytesConsumed = source.Read7BitEncodedString(out var value);

        Assert.Equal(expected, value);
        Assert.Equal(encodedBytes.Length, bytesConsumed);
    }

    [Fact]
    public void Read7BitEncodedString_WithEmptyString_ReturnsEmptyAndConsumesLengthByte()
    {
        // Length of 0
        ReadOnlySpan<byte> source = new byte[] { 0x00 };

        var bytesConsumed = source.Read7BitEncodedString(out var value);

        Assert.Equal(string.Empty, value);
        Assert.Equal(1, bytesConsumed);
    }

    [Fact]
    public void Read7BitEncodedString_WithTruncatedStringData_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() =>
        {
            // Length says 10 bytes, but only 3 bytes of data follow
            ReadOnlySpan<byte> source = [0x0A, 0x41, 0x42, 0x43];

            return source.Read7BitEncodedString(out _);
        });
    }

    [Fact]
    public void Read7BitEncodedString_WithTruncatedLengthPrefix_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() =>
        {
            // Continuation bit set but no more data
            ReadOnlySpan<byte> source = [0x80];

            return source.Read7BitEncodedString(out _);
        });
    }

    [Fact]
    public void Read7BitEncodedString_WithExtraDataAfterString_ConsumesOnlyNeededBytes()
    {
        // "Hi" (length 2) followed by extra bytes
        ReadOnlySpan<byte> source = new byte[] { 0x02, 0x48, 0x69, 0xFF, 0xFF };

        var bytesConsumed = source.Read7BitEncodedString(out var value);

        Assert.Equal("Hi", value);
        Assert.Equal(3, bytesConsumed); // 1 byte length + 2 bytes string
    }

    [Fact]
    public void Read7BitEncodedString_WithMultiByteLengthPrefix_DecodesCorrectly()
    {
        // Create a string that requires a multi-byte length prefix (> 127 bytes)
        var longString = new string('A', 200);
        var stringBytes = System.Text.Encoding.UTF8.GetBytes(longString);
        var lengthBytes = new byte[5];
        Span<byte> lengthSpan = lengthBytes;
        var lengthSize = lengthSpan.Write7BitEncodedInt(stringBytes.Length);

        Assert.True(lengthSize > 1); // Verify we're testing multi-byte length

        var encodedBytes = new byte[lengthSize + stringBytes.Length];
        Array.Copy(lengthBytes, 0, encodedBytes, 0, lengthSize);
        Array.Copy(stringBytes, 0, encodedBytes, lengthSize, stringBytes.Length);

        ReadOnlySpan<byte> source = encodedBytes;

        var bytesConsumed = source.Read7BitEncodedString(out var value);

        Assert.Equal(longString, value);
        Assert.Equal(encodedBytes.Length, bytesConsumed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Hello")]
    [InlineData("Hello, World!")]
    [InlineData("UTF-8: \u00e9\u00e8\u00ea")]
    public void Write7BitEncodedString_EncodesCorrectly(string value)
    {
        var expectedByteCount = Int7BitEncodingUtils.Measure7BitEncodedStringLength(value);
        Span<byte> buffer = stackalloc byte[expectedByteCount];

        var bytesWritten = buffer.Write7BitEncodedString(value);

        Assert.Equal(expectedByteCount, bytesWritten);

        // Verify by reading back
        ReadOnlySpan<byte> source = buffer.Slice(0, bytesWritten);
        var bytesConsumed = source.Read7BitEncodedString(out var decoded);

        Assert.Equal(value, decoded);
        Assert.Equal(bytesWritten, bytesConsumed);
    }

    [Fact]
    public void Write7BitEncodedString_WithNullString_WritesZeroLength()
    {
        Span<byte> buffer = stackalloc byte[10];

        var bytesWritten = buffer.Write7BitEncodedString(null!);

        Assert.Equal(1, bytesWritten);
        Assert.Equal(0, buffer[0]);
    }

    [Fact]
    public void Write7BitEncodedString_WithEmptyString_WritesZeroLength()
    {
        Span<byte> buffer = stackalloc byte[10];

        var bytesWritten = buffer.Write7BitEncodedString(string.Empty);

        Assert.Equal(1, bytesWritten);
        Assert.Equal(0, buffer[0]);
    }

    [Fact]
    public void Write7BitEncodedString_WithLongString_UsesMultiByteLengthPrefix()
    {
        var longString = new string('A', 200);
        var expectedByteCount = Int7BitEncodingUtils.Measure7BitEncodedStringLength(longString);
        var buffer = new byte[expectedByteCount];

        var bytesWritten = buffer.AsSpan().Write7BitEncodedString(longString);

        Assert.Equal(expectedByteCount, bytesWritten);

        // Verify the length prefix is multi-byte (200 > 127)
        Assert.True((buffer[0] & 0x80) != 0); // Continuation bit set

        // Verify by reading back
        ReadOnlySpan<byte> source = buffer;
        var bytesConsumed = source.Read7BitEncodedString(out var decoded);

        Assert.Equal(longString, decoded);
        Assert.Equal(bytesWritten, bytesConsumed);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("A", 2)]
    [InlineData("Hello", 6)]
    public void Measure7BitEncodedStringLength_ReturnsCorrectLength(string value, int expectedLength)
    {
        var actualLength = Int7BitEncodingUtils.Measure7BitEncodedStringLength(value);

        Assert.Equal(expectedLength, actualLength);
    }

    [Fact]
    public void Measure7BitEncodedStringLength_WithNullString_ReturnsOne()
    {
        var length = Int7BitEncodingUtils.Measure7BitEncodedStringLength(null!);

        Assert.Equal(1, length);
    }

    [Fact]
    public void Measure7BitEncodedStringLength_WithLongString_IncludesMultiByteLengthPrefix()
    {
        var longString = new string('A', 200);

        var length = Int7BitEncodingUtils.Measure7BitEncodedStringLength(longString);

        // 200 bytes for string + 2 bytes for length prefix (200 requires 2 bytes in 7-bit encoding)
        Assert.Equal(202, length);
    }

    [Fact]
    public void Measure7BitEncodedStringLength_WithUtf8String_CountsUtf8Bytes()
    {
        // Each of these characters is 2 bytes in UTF-8
        var utf8String = "\u00e9\u00e8\u00ea"; // 3 chars, 6 bytes

        var length = Int7BitEncodingUtils.Measure7BitEncodedStringLength(utf8String);

        // 6 bytes for string + 1 byte for length prefix
        Assert.Equal(7, length);
    }
}
