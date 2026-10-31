// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.WebEncoders.Testing;

public class HtmlTestEncoderTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("abcd", "HtmlEncode[[abcd]]")]
    [InlineData("<<''\"\">>", "HtmlEncode[[<<''\"\">>]]")]
    public void StringEncode_EncodesAsExpected(string input, string expectedOutput)
    {
        // Arrange
        var encoder = new HtmlTestEncoder();

        // Act
        var output = encoder.Encode(input);

        // Assert
        Assert.Equal(expectedOutput, output);
    }
}
