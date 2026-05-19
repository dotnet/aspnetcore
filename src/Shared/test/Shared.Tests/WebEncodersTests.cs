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
    [InlineData("", "")]
    [InlineData("123456qwerty++//X+/x", "123456qwerty--__X-_x")]
    [InlineData("123456qwerty++//X+/xxw==", "123456qwerty--__X-_xxw")]
    [InlineData("123456qwerty++//X+/xxw0=", "123456qwerty--__X-_xxw0")]
    public void Base64UrlEncode_And_Decode_WithBufferOffsets(string base64Input, string expectedBase64Url)
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
        var numEncodedChars =
            WebEncoders.Base64UrlEncode(input, offset: 3, output: output, outputOffset: 4, count: input.Length - 5);

        // Assert 1
        var encodedString = new string(output, startIndex: 4, length: numEncodedChars);
        Assert.Equal(expectedBase64Url, encodedString);

        // Act 2
        var roundTripInput = new string(output);
        var roundTripped =
            WebEncoders.Base64UrlDecode(roundTripInput, offset: 4, buffer: buffer, bufferOffset: 5, count: numEncodedChars);

        // Assert 2, verify that values round-trip
        var roundTrippedAsBase64 = Convert.ToBase64String(roundTripped);
        Assert.Equal(roundTrippedAsBase64, base64Input);
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
}
