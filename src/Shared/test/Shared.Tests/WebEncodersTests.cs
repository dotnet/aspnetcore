// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class WebEncodersTests
{
    [Theory]
    [InlineData("", 1, 0)]
    [InlineData("", 0, 1)]
    [InlineData("0123456789", 9, 2)]
    [InlineData("0123456789", Int32.MaxValue, 2)]
    [InlineData("0123456789", 9, -1)]
    public void Base64UrlDecode_BadOffsets(string input, int offset, int count)
    {
        // Act & assert
        Assert.ThrowsAny<ArgumentException>(() =>
        {
            var retVal = WebEncoders.Base64UrlDecode(input, offset, count);
        });
    }

    [Theory]
    [InlineData("x")]
    [InlineData("(x)")]
    public void Base64UrlDecode_MalformedInput(string input)
    {
        // Act & assert
        Assert.Throws<FormatException>(() =>
        {
            var retVal = WebEncoders.Base64UrlDecode(input);
        });
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("123456qwerty++//X+/x", "123456qwerty--__X-_x")]
    [InlineData("123456qwerty++//X+/xxw==", "123456qwerty--__X-_xxw")]
    [InlineData("123456qwerty++//X+/xxw0=", "123456qwerty--__X-_xxw0")]
    public void Base64UrlEncode_And_Decode(string base64Input, string expectedBase64Url)
    {
        // Arrange
        byte[] input = new byte[3].Concat(Convert.FromBase64String(base64Input)).Concat(new byte[2]).ToArray();

        // Act & assert - 1
        string actualBase64Url = WebEncoders.Base64UrlEncode(input, 3, input.Length - 5); // also helps test offsets
        Assert.Equal(expectedBase64Url, actualBase64Url);

        // Act & assert - 2
        // Verify that values round-trip
        byte[] roundTripped = WebEncoders.Base64UrlDecode("xx" + actualBase64Url + "yyy", 2, actualBase64Url.Length); // also helps test offsets
        string roundTrippedAsBase64 = Convert.ToBase64String(roundTripped);
        Assert.Equal(roundTrippedAsBase64, base64Input);
    }

    [Theory]
    [MemberData(nameof(Base64UrlEncodeDecodeData))]
    public void Base64UrlEncode_And_Decode_WithBufferOffsets(string base64Input, string expectedBase64Url, int _)
    {
        // Arrange
        var input = new byte[3].Concat(Convert.FromBase64String(base64Input)).Concat(new byte[2]).ToArray();
        var buffer = new char[30];
        var output = new char[30];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = '^';
            output[i] = '^';
        }

        // Act 1
        var numEncodedChars = WebEncoders.Base64UrlEncode(input, offset: 3, output: output, outputOffset: 4, count: input.Length - 5);

        // Assert 1
        var encodedString = new string(output, startIndex: 4, length: numEncodedChars);
        Assert.Equal(expectedBase64Url, encodedString);

        // Act 2
        var roundTripInput = new string(output);
        var roundTripped = WebEncoders.Base64UrlDecode(roundTripInput, offset: 4, buffer: buffer, bufferOffset: 5, count: numEncodedChars);

        // Assert 2, verify that values round-trip
        var roundTrippedAsBase64 = Convert.ToBase64String(roundTripped);
        Assert.Equal(roundTrippedAsBase64, base64Input);
    }

    [Theory]
    [MemberData(nameof(Base64UrlEncodeDecodeData))]
    public void TryBase64UrlEncode_And_Decode_WithBufferOffsets(string base64Input, string expectedBase64Url, int expectedBytesWritten)
    {
        // Arrange
        var input = new byte[3].Concat(Convert.FromBase64String(base64Input)).Concat(new byte[2]).ToArray();
        var outputBytes = new byte[30];
        var buffer = new char[30];
        var output = new char[30];
        for (var i = 0; i < output.Length; i++)
        {
            buffer[i] = '^';
            output[i] = '^';
        }

        // Act 1
        var numEncodedChars = WebEncoders.Base64UrlEncode(input, offset: 3, output: output, outputOffset: 4, count: input.Length - 5);

        // Assert 1
        var encodedString = new string(output, startIndex: 4, length: numEncodedChars);
        Assert.Equal(expectedBase64Url, encodedString);

        // Act 2
        var roundTripInput = new string(output);

        var roundTripped = WebEncoders.Base64UrlDecode(roundTripInput, offset: 4, buffer: buffer, bufferOffset: 5, count: numEncodedChars);
        var decodeResult = WebEncoders.TryBase64UrlDecode(roundTripInput, offset: 4, count: numEncodedChars, outputBytes, out var bytesWritten);
        Assert.Equal(expectedBytesWritten, bytesWritten);
        Assert.True(decodeResult);

        // Assert 2, verify that values round-trip. Only in case something was written to destination buffer
        if (bytesWritten > 0)
        {
            var roundTrippedAsBase64 = Convert.ToBase64String(outputBytes[..bytesWritten]);
            Assert.Equal(roundTrippedAsBase64, base64Input);
        }
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(10, 9, 2)]
    [InlineData(10, Int32.MaxValue, 2)]
    [InlineData(10, 9, -1)]
    public void Base64UrlEncode_BadOffsets(int inputLength, int offset, int count)
    {
        // Arrange
        byte[] input = new byte[inputLength];

        // Act & assert
        Assert.ThrowsAny<ArgumentException>(() =>
        {
            var retVal = WebEncoders.Base64UrlEncode(input, offset, count);
        });
    }

    public static TheoryData<string, string, int> Base64UrlEncodeDecodeData => new TheoryData<string, string, int>
    {
        { "", "", 0 },
        { "123456qwerty++//X+/x", "123456qwerty--__X-_x", 15 },
        { "123456qwerty++//X+/xxw==", "123456qwerty--__X-_xxw", 16 },
        { "123456qwerty++//X+/xxw0=", "123456qwerty--__X-_xxw0", 17 },

        { "TWFu", "TWFu", 3 },                        // "Man"
        { "TWE=", "TWE", 2 },                         // "Ma"
        { "TQ==", "TQ", 1 },                          // "M"
        { "YWJjZGVm+g==", "YWJjZGVm-g", 7 }, // abc+def
        { "SGVsbG8gd29ybGQ=", "SGVsbG8gd29ybGQ", 11 },// "Hello world"
        { "SGVsbG8td29ybGQ_", "SGVsbG8td29ybGQ_", 11 }, // "Hello-world" (url-safe)
        { "AAECAwQFBgcICQ==", "AAECAwQFBgcICQ", 10 }, // binary: 0x00 0x01 ... 0x09
        { "AAECAwQFBgcICQ", "AAECAwQFBgcICQ", 10 },   // same as above, no padding
        { "Zm9vYmFy", "Zm9vYmFy", 6 },                // "foobar"
        { "Zm9vYmFyIQ==", "Zm9vYmFyIQ", 7 },          // "foobar!"
    };
}
