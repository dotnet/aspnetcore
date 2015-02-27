// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class JavaScriptStringEncoderTests
    {
        [Fact]
        public void Ctor_WithCodePointFilter()
        {
            // Arrange
            var filter = new CodePointFilter(UnicodeBlocks.None).AllowChars("ab").AllowChars('\0', '&', '\uFFFF', 'd');
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(filter);

            // Act & assert
            Assert.Equal("a", encoder.JavaScriptStringEncode("a"));
            Assert.Equal("b", encoder.JavaScriptStringEncode("b"));
            Assert.Equal(@"\u0063", encoder.JavaScriptStringEncode("c"));
            Assert.Equal("d", encoder.JavaScriptStringEncode("d"));
            Assert.Equal(@"\u0000", encoder.JavaScriptStringEncode("\0")); // we still always encode control chars
            Assert.Equal(@"\u0026", encoder.JavaScriptStringEncode("&")); // we still always encode HTML-special chars
            Assert.Equal(@"\uFFFF", encoder.JavaScriptStringEncode("\uFFFF")); // we still always encode non-chars and other forbidden chars
        }

        [Fact]
        public void Ctor_WithUnicodeBlocks()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeBlocks.Latin1Supplement, UnicodeBlocks.MiscellaneousSymbols);

            // Act & assert
            Assert.Equal(@"\u0061", encoder.JavaScriptStringEncode("a"));
            Assert.Equal("\u00E9", encoder.JavaScriptStringEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("\u2601", encoder.JavaScriptStringEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Ctor_WithNoParameters_DefaultsToBasicLatin()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();

            // Act & assert
            Assert.Equal("a", encoder.JavaScriptStringEncode("a"));
            Assert.Equal(@"\u00E9", encoder.JavaScriptStringEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal(@"\u2601", encoder.JavaScriptStringEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Default_EquivalentToBasicLatin()
        {
            // Arrange
            JavaScriptStringEncoder controlEncoder = new JavaScriptStringEncoder(UnicodeBlocks.BasicLatin);
            JavaScriptStringEncoder testEncoder = JavaScriptStringEncoder.Default;

            // Act & assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                if (!IsSurrogateCodePoint(i))
                {
                    string input = new String((char)i, 1);
                    Assert.Equal(controlEncoder.JavaScriptStringEncode(input), testEncoder.JavaScriptStringEncode(input));
                }
            }
        }

        [Fact]
        public void Default_ReturnsSingletonInstance()
        {
            // Act
            JavaScriptStringEncoder encoder1 = JavaScriptStringEncoder.Default;
            JavaScriptStringEncoder encoder2 = JavaScriptStringEncoder.Default;

            // Assert
            Assert.Same(encoder1, encoder2);
        }

        [Theory]
        [InlineData("<", @"\u003C")]
        [InlineData(">", @"\u003E")]
        [InlineData("&", @"\u0026")]
        [InlineData("'", @"\u0027")]
        [InlineData("\"", @"\u0022")]
        [InlineData("+", @"\u002B")]
        [InlineData("\\", @"\\")]
        [InlineData("/", @"\/")]
        [InlineData("\b", @"\b")]
        [InlineData("\f", @"\f")]
        [InlineData("\n", @"\n")]
        [InlineData("\t", @"\t")]
        [InlineData("\r", @"\r")]
        public void JavaScriptStringEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple(string input, string expected)
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeBlocks.All);

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_AllRangesAllowed_StillEncodesForbiddenChars_Extended()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeBlocks.All);

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
                    if (input == "\b") { expected = @"\b"; }
                    else if (input == "\t") { expected = @"\t"; }
                    else if (input == "\n") { expected = @"\n"; }
                    else if (input == "\f") { expected = @"\f"; }
                    else if (input == "\r") { expected = @"\r"; }
                    else if (input == "\\") { expected = @"\\"; }
                    else if (input == "/") { expected = @"\/"; }
                    else
                    {
                        bool mustEncode = false;
                        switch (i)
                        {
                            case '<':
                            case '>':
                            case '&':
                            case '\"':
                            case '\'':
                            case '+':
                                mustEncode = true;
                                break;
                        }

                        if (i <= 0x001F || (0x007F <= i && i <= 0x9F))
                        {
                            mustEncode = true; // control char
                        }
                        else if (!UnicodeHelpers.IsCharacterDefined((char)i))
                        {
                            mustEncode = true; // undefined (or otherwise disallowed) char
                        }

                        if (mustEncode)
                        {
                            expected = String.Format(CultureInfo.InvariantCulture, @"\u{0:X4}", i);
                        }
                        else
                        {
                            expected = input; // no encoding
                        }
                    }
                }

                string retVal = encoder.JavaScriptStringEncode(input);
                Assert.Equal(expected, retVal);
            }

            // Act & assert - astral chars
            for (int i = 0x10000; i <= 0x10FFFF; i++)
            {
                string input = Char.ConvertFromUtf32(i);
                string expected = String.Format(CultureInfo.InvariantCulture, @"\u{0:X4}\u{1:X4}", (uint)input[0], (uint)input[1]);
                string retVal = encoder.JavaScriptStringEncode(input);
                Assert.Equal(expected, retVal);
            }
        }

        [Fact]
        public void JavaScriptStringEncode_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeBlocks.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string expected = "a\uFFFDb\uFFFDc\uFFFD\uFFFDd\uFFFD\\uD800\\uDFFFe\uFFFD"; // 'D800' 'DFFF' was preserved since it's valid

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_EmptyStringInput_ReturnsEmptyString()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();

            // Act & assert
            Assert.Equal("", encoder.JavaScriptStringEncode(""));
        }

        [Fact]
        public void JavaScriptStringEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();
            string input = "Hello, there!";

            // Act & assert
            Assert.Same(input, encoder.JavaScriptStringEncode(input));
        }

        [Fact]
        public void JavaScriptStringEncode_NullInput_ReturnsNull()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();

            // Act & assert
            Assert.Null(encoder.JavaScriptStringEncode(null));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingAtBeginning()
        {
            Assert.Equal(@"\u0026Hello, there!", new JavaScriptStringEncoder().JavaScriptStringEncode("&Hello, there!"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingAtEnd()
        {
            Assert.Equal(@"Hello, there!\u0026", new JavaScriptStringEncoder().JavaScriptStringEncode("Hello, there!&"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingInMiddle()
        {
            Assert.Equal(@"Hello, \u0026there!", new JavaScriptStringEncoder().JavaScriptStringEncode("Hello, &there!"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingInterspersed()
        {
            Assert.Equal(@"Hello, \u003Cthere\u003E!", new JavaScriptStringEncoder().JavaScriptStringEncode("Hello, <there>!"));
        }

        [Fact]
        public void JavaScriptStringEncode_CharArray()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();
            var output = new StringWriter();

            // Act
            encoder.JavaScriptStringEncode("Hello+world!".ToCharArray(), 3, 5, output);

            // Assert
            Assert.Equal(@"lo\u002Bwo", output.ToString());
        }

        [Fact]
        public void JavaScriptStringEncode_StringSubstring()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();
            var output = new StringWriter();

            // Act
            encoder.JavaScriptStringEncode("Hello+world!", 3, 5, output);

            // Assert
            Assert.Equal(@"lo\u002Bwo", output.ToString());
        }

        [Theory]
        [InlineData("\"", @"\u0022")]
        [InlineData("'", @"\u0027")]
        public void JavaScriptStringEncode_Quotes(string input, string expected)
        {
            // Per the design document, we provide additional defense-in-depth
            // against breaking out of HTML attributes by having the encoders
            // never emit the ' or " characters. This means that we want to
            // \u-escape these characters instead of using \' and \".

            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeBlocks.All);

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_DoesNotOutputHtmlSensitiveCharacters()
        {
            // Per the design document, we provide additional defense-in-depth
            // by never emitting HTML-sensitive characters unescaped.

            // Arrange
            JavaScriptStringEncoder javaScriptStringEncoder = new JavaScriptStringEncoder(UnicodeBlocks.All);
            HtmlEncoder htmlEncoder = new HtmlEncoder(UnicodeBlocks.All);

            // Act & assert
            for (int i = 0; i <= 0x10FFFF; i++)
            {
                if (IsSurrogateCodePoint(i))
                {
                    continue; // surrogates don't matter here
                }

                string javaScriptStringEncoded = javaScriptStringEncoder.JavaScriptStringEncode(Char.ConvertFromUtf32(i));
                string thenHtmlEncoded = htmlEncoder.HtmlEncode(javaScriptStringEncoded);
                Assert.Equal(javaScriptStringEncoded, thenHtmlEncoded); // should have contained no HTML-sensitive characters
            }
        }

        private static bool IsSurrogateCodePoint(int codePoint)
        {
            return (0xD800 <= codePoint && codePoint <= 0xDFFF);
        }
    }
}
