// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class ComponentsBase64HelperTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "Zg")]
    [InlineData("fo", "Zm8")]
    [InlineData("foo", "Zm9v")]
    [InlineData("foob", "Zm9vYg")]
    [InlineData("fooba", "Zm9vYmE")]
    [InlineData("foobar", "Zm9vYmFy")]
    [InlineData("Hello, World!", "SGVsbG8sIFdvcmxkIQ")]
    [InlineData("The quick brown fox jumps over the lazy dog", "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw")]
    public void Base64UrlEncode_ProducesExpectedOutput(string input, string expectedBase64Url)
    {
        // Arrange
        var inputBytes = Encoding.UTF8.GetBytes(input);

        // Act
        var actualBase64Url = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlEncode(inputBytes);

        // Assert
        Assert.Equal(expectedBase64Url, actualBase64Url);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("Zg", "f")]
    [InlineData("Zm8", "fo")]
    [InlineData("Zm9v", "foo")]
    [InlineData("Zm9vYg", "foob")]
    [InlineData("Zm9vYmE", "fooba")]
    [InlineData("Zm9vYmFy", "foobar")]
    [InlineData("SGVsbG8sIFdvcmxkIQ", "Hello, World!")]
    [InlineData("VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw", "The quick brown fox jumps over the lazy dog")]
    public void Base64UrlDecode_ProducesExpectedOutput(string base64UrlInput, string expectedOutput)
    {
        // Act
        var actualBytes = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlDecode(base64UrlInput);
        var actualOutput = Encoding.UTF8.GetString(actualBytes);

        // Assert
        Assert.Equal(expectedOutput, actualOutput);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("f", "Zg")]
    [InlineData("fo", "Zm8")]
    [InlineData("foo", "Zm9v")]
    [InlineData("foob", "Zm9vYg")]
    [InlineData("fooba", "Zm9vYmE")]
    [InlineData("foobar", "Zm9vYmFy")]
    public void ToBase64Url_WithReadOnlySpan_ProducesExpectedOutput(string input, string expectedBase64Url)
    {
        // Arrange
        var inputBytes = Encoding.UTF8.GetBytes(input);

        // Act
        var actualBase64Url = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url((ReadOnlySpan<byte>)inputBytes);

        // Assert
        Assert.Equal(expectedBase64Url, actualBase64Url);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("f", "Zg")]
    [InlineData("fo", "Zm8")]
    [InlineData("foo", "Zm9v")]
    [InlineData("foob", "Zm9vYg")]
    [InlineData("fooba", "Zm9vYmE")]
    [InlineData("foobar", "Zm9vYmFy")]
    public void ToBase64Url_WithSpan_ProducesExpectedOutput(string input, string expectedBase64Url)
    {
        // Arrange
        var inputBytes = Encoding.UTF8.GetBytes(input);

        // Act
        var actualBase64Url = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url((Span<byte>)inputBytes);

        // Assert
        Assert.Equal(expectedBase64Url, actualBase64Url);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("f", 2)]
    [InlineData("fo", 3)]
    [InlineData("foo", 4)]
    [InlineData("foob", 6)]
    [InlineData("fooba", 7)]
    [InlineData("foobar", 8)]
    public void ToBase64Url_WithSpanOutput_ProducesExpectedOutput(string input, int expectedLength)
    {
        // Arrange
        var inputBytes = Encoding.UTF8.GetBytes(input);
        Span<char> output = stackalloc char[20]; // Sufficient space

        // Act
        var actualLength = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url(inputBytes, output);

        // Assert
        Assert.Equal(expectedLength, actualLength);

        if (expectedLength > 0)
        {
            var actualOutput = output[..actualLength].ToString();
            var expectedOutput = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url(inputBytes);
            Assert.Equal(expectedOutput, actualOutput);
        }
    }

    [Fact]
    public void Base64UrlEncode_WithNullInput_ReturnsEmptyString()
    {
        // Act
        var result = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlEncode(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Base64UrlDecode_WithNullInput_ReturnsEmptyArray()
    {
        // Act
        var result = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlDecode(null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Base64UrlDecode_WithEmptyString_ReturnsEmptyArray()
    {
        // Act
        var result = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlDecode("");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("any carnal pleasure.", "YW55IGNhcm5hbCBwbGVhc3VyZS4")]
    [InlineData("any carnal pleasure", "YW55IGNhcm5hbCBwbGVhc3VyZQ")]
    [InlineData("any carnal pleasur", "YW55IGNhcm5hbCBwbGVhc3Vy")]
    [InlineData("any carnal pleasu", "YW55IGNhcm5hbCBwbGVhc3U")]
    [InlineData("any carnal pleas", "YW55IGNhcm5hbCBwbGVhcw")]
    public void Base64UrlEncodeAndDecode_RoundTrip_PreservesOriginalData(string originalText, string expectedBase64Url)
    {
        // Arrange
        var originalBytes = Encoding.UTF8.GetBytes(originalText);

        // Act - Encode
        var encoded = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlEncode(originalBytes);

        // Assert - Check encoded value
        Assert.Equal(expectedBase64Url, encoded);

        // Act - Decode
        var decodedBytes = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlDecode(encoded);
        var decodedText = Encoding.UTF8.GetString(decodedBytes);

        // Assert - Check round-trip
        Assert.Equal(originalText, decodedText);
        Assert.Equal(originalBytes, decodedBytes);
    }

    [Theory]
    [InlineData(new byte[] { 0x00 }, "AA")]
    [InlineData(new byte[] { 0xFF }, "_w")]
    [InlineData(new byte[] { 0x00, 0xFF }, "AP8")]
    [InlineData(new byte[] { 0xFF, 0x00 }, "_wA")]
    [InlineData(new byte[] { 0x3E, 0x3F }, "Pj8")]
    [InlineData(new byte[] { 0xFC, 0xFD, 0xFE, 0xFF }, "_P3-_w")]
    public void Base64UrlEncode_WithSpecialBytes_ProducesCorrectUrlSafeOutput(byte[] input, string expectedOutput)
    {
        // Act
        var actual = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlEncode(input);

        // Assert
        Assert.Equal(expectedOutput, actual);

        // Verify it doesn't contain URL-unsafe characters
        Assert.DoesNotContain('+', actual);
        Assert.DoesNotContain('/', actual);
        Assert.DoesNotContain('=', actual);
    }

    [Theory]
    [InlineData("AA", new byte[] { 0x00 })]
    [InlineData("_w", new byte[] { 0xFF })]
    [InlineData("AP8", new byte[] { 0x00, 0xFF })]
    [InlineData("_wA", new byte[] { 0xFF, 0x00 })]
    [InlineData("Pj8", new byte[] { 0x3E, 0x3F })]
    [InlineData("_P3-_w", new byte[] { 0xFC, 0xFD, 0xFE, 0xFF })]
    public void Base64UrlDecode_WithUrlSafeCharacters_ProducesCorrectOutput(string input, byte[] expectedOutput)
    {
        // Act
        var actual = Microsoft.AspNetCore.Components.ComponentsBase64Helper.Base64UrlDecode(input);

        // Assert
        Assert.Equal(expectedOutput, actual);
    }

    [Fact]
    public void ToBase64Url_WithEmptyReadOnlySpan_ReturnsEmptyString()
    {
        // Arrange
        ReadOnlySpan<byte> emptySpan = ReadOnlySpan<byte>.Empty;

        // Act
        var result = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url(emptySpan);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToBase64Url_WithEmptySpan_ReturnsEmptyString()
    {
        // Arrange
        Span<byte> emptySpan = Span<byte>.Empty;

        // Act
        var result = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url(emptySpan);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToBase64Url_WithEmptySpanOutput_ReturnsZero()
    {
        // Arrange
        ReadOnlySpan<byte> emptyInput = ReadOnlySpan<byte>.Empty;
        Span<char> output = stackalloc char[10];

        // Act
        var result = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url(emptyInput, output);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(4, 6)]
    [InlineData(5, 7)]
    [InlineData(6, 8)]
    public void ToBase64Url_OutputLength_IsCorrect(int inputLength, int expectedOutputLength)
    {
        // Arrange
        var input = new byte[inputLength];
        for (int i = 0; i < inputLength; i++)
        {
            input[i] = (byte)(i + 1);
        }

        // Act
        var result = Microsoft.AspNetCore.Components.ComponentsBase64Helper.ToBase64Url(input);

        // Assert
        Assert.Equal(expectedOutputLength, result.Length);
    }
}
