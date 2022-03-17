// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WebUtilities;

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

    [Fact]
    public void DataOfVariousLengthRoundTripCorrectly()
    {
        for (int length = 0; length != 256; ++length)
        {
            var data = new byte[length];
            for (int index = 0; index != length; ++index)
            {
                data[index] = (byte)(5 + length + (index * 23));
            }
            string text = WebEncoders.Base64UrlEncode(data);
            byte[] result = WebEncoders.Base64UrlDecode(text);

            for (int index = 0; index != length; ++index)
            {
                Assert.Equal(data[index], result[index]);
            }
        }
    }
}
