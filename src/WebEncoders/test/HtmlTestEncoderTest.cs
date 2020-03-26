// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Extensions.WebEncoders.Testing
{
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
}
