// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class HtmlEncoderTests
    {
        [Fact]
        public void Ctor_WithCodePointFilter()
        {
            // Arrange
            var filter = new CodePointFilter(UnicodeBlocks.None).AllowChars("ab").AllowChars('\0', '&', '\uFFFF', 'd');
            HtmlEncoder encoder = new HtmlEncoder(filter);

            // Act & assert
            Assert.Equal("a", encoder.HtmlEncode("a"));
            Assert.Equal("b", encoder.HtmlEncode("b"));
            Assert.Equal("&#x63;", encoder.HtmlEncode("c"));
            Assert.Equal("d", encoder.HtmlEncode("d"));
            Assert.Equal("&#x0;", encoder.HtmlEncode("\0")); // we still always encode control chars
            Assert.Equal("&amp;", encoder.HtmlEncode("&")); // we still always encode HTML-special chars
            Assert.Equal("&#xFFFF;", encoder.HtmlEncode("\uFFFF")); // we still always encode non-chars and other forbidden chars
        }

        [Fact]
        public void Ctor_WithUnicodeBlocks()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder(UnicodeBlocks.Latin1Supplement, UnicodeBlocks.MiscellaneousSymbols);

            // Act & assert
            Assert.Equal("&#x61;", encoder.HtmlEncode("a"));
            Assert.Equal("\u00E9", encoder.HtmlEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("\u2601", encoder.HtmlEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Ctor_WithNoParameters_DefaultsToBasicLatin()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder();

            // Act & assert
            Assert.Equal("a", encoder.HtmlEncode("a"));
            Assert.Equal("&#xE9;", encoder.HtmlEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("&#x2601;", encoder.HtmlEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Default_EquivalentToBasicLatin()
        {
            // Arrange
            HtmlEncoder controlEncoder = new HtmlEncoder(UnicodeBlocks.BasicLatin);
            HtmlEncoder testEncoder = HtmlEncoder.Default;

            // Act & assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                if (!IsSurrogateCodePoint(i))
                {
                    string input = new String((char)i, 1);
                    Assert.Equal(controlEncoder.HtmlEncode(input), testEncoder.HtmlEncode(input));
                }
            }
        }

        [Fact]
        public void Default_ReturnsSingletonInstance()
        {
            // Act
            HtmlEncoder encoder1 = HtmlEncoder.Default;
            HtmlEncoder encoder2 = HtmlEncoder.Default;

            // Assert
            Assert.Same(encoder1, encoder2);
        }

        [Theory]
        [InlineData("<", "&lt;")]
        [InlineData(">", "&gt;")]
        [InlineData("&", "&amp;")]
        [InlineData("'", "&#x27;")]
        [InlineData("\"", "&quot;")]
        [InlineData("+", "&#x2B;")]
        public void HtmlEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple(string input, string expected)
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder(UnicodeBlocks.All);

            // Act
            string retVal = encoder.HtmlEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void HtmlEncode_AllRangesAllowed_StillEncodesForbiddenChars_Extended()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder(UnicodeBlocks.All);

            // Act & assert - BMP chars
            for (int i = 0; i <= 0xFFFF; i++)
            {
                string input = new String((char)i, 1);
                string expected;
                if (IsSurrogateCodePoint(i))
                {
                    expected = "\uFFFD"; // unpaired surrogate -> Unicode replacement char
                }
                else
                {
                    if (input == "<") { expected = "&lt;"; }
                    else if (input == ">") { expected = "&gt;"; }
                    else if (input == "&") { expected = "&amp;"; }
                    else if (input == "\"") { expected = "&quot;"; }
                    else
                    {
                        bool mustEncode = false;
                        if (i == '\'' || i == '+')
                        {
                            mustEncode = true; // apostrophe, plus
                        }
                        else if (i <= 0x001F || (0x007F <= i && i <= 0x9F))
                        {
                            mustEncode = true; // control char
                        }
                        else if (!UnicodeHelpers.IsCharacterDefined((char)i))
                        {
                            mustEncode = true; // undefined (or otherwise disallowed) char
                        }

                        if (mustEncode)
                        {
                            expected = String.Format(CultureInfo.InvariantCulture, "&#x{0:X};", i);
                        }
                        else
                        {
                            expected = input; // no encoding
                        }
                    }
                }

                string retVal = encoder.HtmlEncode(input);
                Assert.Equal(expected, retVal);
            }

            // Act & assert - astral chars
            for (int i = 0x10000; i <= 0x10FFFF; i++)
            {
                string input = Char.ConvertFromUtf32(i);
                string expected = String.Format(CultureInfo.InvariantCulture, "&#x{0:X};", i);
                string retVal = encoder.HtmlEncode(input);
                Assert.Equal(expected, retVal);
            }
        }

        [Fact]
        public void HtmlEncode_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder(UnicodeBlocks.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string expected = "a\uFFFDb\uFFFDc\uFFFD\uFFFDd\uFFFD&#x103FF;e\uFFFD";

            // Act
            string retVal = encoder.HtmlEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void HtmlEncode_EmptyStringInput_ReturnsEmptyString()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder();

            // Act & assert
            Assert.Equal("", encoder.HtmlEncode(""));
        }

        [Fact]
        public void HtmlEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder();
            string input = "Hello, there!";

            // Act & assert
            Assert.Same(input, encoder.HtmlEncode(input));
        }

        [Fact]
        public void HtmlEncode_NullInput_ReturnsNull()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder();

            // Act & assert
            Assert.Null(encoder.HtmlEncode(null));
        }

        [Fact]
        public void HtmlEncode_WithCharsRequiringEncodingAtBeginning()
        {
            Assert.Equal("&amp;Hello, there!", new HtmlEncoder().HtmlEncode("&Hello, there!"));
        }

        [Fact]
        public void HtmlEncode_WithCharsRequiringEncodingAtEnd()
        {
            Assert.Equal("Hello, there!&amp;", new HtmlEncoder().HtmlEncode("Hello, there!&"));
        }

        [Fact]
        public void HtmlEncode_WithCharsRequiringEncodingInMiddle()
        {
            Assert.Equal("Hello, &amp;there!", new HtmlEncoder().HtmlEncode("Hello, &there!"));
        }

        [Fact]
        public void HtmlEncode_WithCharsRequiringEncodingInterspersed()
        {
            Assert.Equal("Hello, &lt;there&gt;!", new HtmlEncoder().HtmlEncode("Hello, <there>!"));
        }

        [Fact]
        public void HtmlEncode_CharArray()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder();
            var output = new StringWriter();

            // Act
            encoder.HtmlEncode("Hello+world!".ToCharArray(), 3, 5, output);

            // Assert
            Assert.Equal("lo&#x2B;wo", output.ToString());
        }

        [Fact]
        public void HtmlEncode_StringSubstring()
        {
            // Arrange
            HtmlEncoder encoder = new HtmlEncoder();
            var output = new StringWriter();

            // Act
            encoder.HtmlEncode("Hello+world!", 3, 5, output);

            // Assert
            Assert.Equal("lo&#x2B;wo", output.ToString());
        }

        private static bool IsSurrogateCodePoint(int codePoint)
        {
            return (0xD800 <= codePoint && codePoint <= 0xDFFF);
        }
    }
}
