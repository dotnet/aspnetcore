// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class NullHtmlEncoderTest
    {
        [Fact]
        public void MaxOutputCharactersPerInputCharacter_Returns1()
        {
            // Arrange
            var encoder = NullHtmlEncoder.Default;

            // Act
            var result = encoder.MaxOutputCharactersPerInputCharacter;

            // Assert
            Assert.Equal(1, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("abcd")]
        [InlineData("<<''\"\">>")]
        public void Encode_String_DoesNotEncode(string value)
        {
            // Arrange
            var encoder = NullHtmlEncoder.Default;

            // Act
            var result = encoder.Encode(value);

            // Assert
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("abcd")]
        [InlineData("<<''\"\">>")]
        public void Encode_StringToTextWriter_DoesNotEncode(string value)
        {
            // Arrange
            var encoder = NullHtmlEncoder.Default;

            // Act
            string result;
            using (var writer = new StringWriter())
            {
                encoder.Encode(writer, value);
                result = writer.ToString();
            }

            // Assert
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData("", 0, 0, "")]
        [InlineData("abcd", 0, 0, "")]
        [InlineData("<<''\"\">>", 0, 0, "")]
        [InlineData("abcd", 0, 1, "a")]
        [InlineData("<<''\"\">>", 0, 1, "<")]
        [InlineData("abcd", 0, 4, "abcd")]
        [InlineData("<<''\"\">>", 0, 8, "<<''\"\">>")]
        [InlineData("abcd", 2, 0, "")]
        [InlineData("<<''\"\">>", 2, 0, "")]
        [InlineData("abcd", 2, 2, "cd")]
        [InlineData("<<''\"\">>", 2, 2, "''")]
        [InlineData("abcd", 3, 0, "")]
        [InlineData("<<''\"\">>", 7, 0, "")]
        [InlineData("abcd", 3, 1, "d")]
        [InlineData("<<''\"\">>", 7, 1, ">")]
        public void Encode_StringToTextWriter_DoesNotEncodeSubstring(
            string value,
            int startIndex,
            int characterCount,
            string expectedResult)
        {
            // Arrange
            var encoder = NullHtmlEncoder.Default;

            // Act
            string result;
            using (var writer = new StringWriter())
            {
                encoder.Encode(writer, value, startIndex, characterCount);
                result = writer.ToString();
            }

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", 0, 0, "")]
        [InlineData("abcd", 0, 0, "")]
        [InlineData("<<''\"\">>", 0, 0, "")]
        [InlineData("abcd", 0, 1, "a")]
        [InlineData("<<''\"\">>", 0, 1, "<")]
        [InlineData("abcd", 0, 4, "abcd")]
        [InlineData("<<''\"\">>", 0, 8, "<<''\"\">>")]
        [InlineData("abcd", 2, 0, "")]
        [InlineData("<<''\"\">>", 2, 0, "")]
        [InlineData("abcd", 2, 2, "cd")]
        [InlineData("<<''\"\">>", 2, 2, "''")]
        [InlineData("abcd", 3, 0, "")]
        [InlineData("<<''\"\">>", 7, 0, "")]
        [InlineData("abcd", 3, 1, "d")]
        [InlineData("<<''\"\">>", 7, 1, ">")]
        public void Encode_CharsToTextWriter_DoesNotEncodeSubstring(
            string value,
            int startIndex,
            int characterCount,
            string expectedResult)
        {
            // Arrange
            var encoder = NullHtmlEncoder.Default;
            var chars = value.ToCharArray();

            // Act
            string result;
            using (var writer = new StringWriter())
            {
                encoder.Encode(writer, chars, startIndex, characterCount);
                result = writer.ToString();
            }

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        [InlineData((int)'\n')]
        [InlineData((int)'\r')]
        [InlineData((int)'<')]
        [InlineData((int)'>')]
        [InlineData((int)'\'')]
        [InlineData((int)'"')]
        public void WillEncode_ReturnsFalse(int unicodeScalar)
        {
            // Arrange
            var encoder = NullHtmlEncoder.Default;

            // Act
            var result = encoder.WillEncode(unicodeScalar);

            // Assert
            Assert.False(result);
        }
    }
}
