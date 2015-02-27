// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Moq;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class UnicodeEncoderBaseTests
    {
        [Fact]
        public void Ctor_WithCustomFilters()
        {
            // Arrange
            var filter = new CodePointFilter(UnicodeBlocks.None).AllowChars("ab").AllowChars('\0', '&', '\uFFFF', 'd');
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(filter);

            // Act & assert
            Assert.Equal("a", encoder.Encode("a"));
            Assert.Equal("b", encoder.Encode("b"));
            Assert.Equal("[U+0063]", encoder.Encode("c"));
            Assert.Equal("d", encoder.Encode("d"));
            Assert.Equal("[U+0000]", encoder.Encode("\0")); // we still always encode control chars
            Assert.Equal("[U+0026]", encoder.Encode("&")); // we still always encode HTML-special chars
            Assert.Equal("[U+FFFF]", encoder.Encode("\uFFFF")); // we still always encode non-chars and other forbidden chars
        }

        [Fact]
        public void Ctor_WithUnicodeBlocks()
        {
            // Arrange
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(new CodePointFilter(UnicodeBlocks.Latin1Supplement, UnicodeBlocks.MiscellaneousSymbols));

            // Act & assert
            Assert.Equal("[U+0061]", encoder.Encode("a"));
            Assert.Equal("\u00E9", encoder.Encode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("\u2601", encoder.Encode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Encode_AllRangesAllowed_StillEncodesForbiddenChars_Simple()
        {
            // Arrange
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            const string input = "Hello <>&\'\"+ there!";
            const string expected = "Hello [U+003C][U+003E][U+0026][U+0027][U+0022][U+002B] there!";

            // Act & assert
            Assert.Equal(expected, encoder.Encode(input));
        }

        [Fact]
        public void Encode_AllRangesAllowed_StillEncodesForbiddenChars_Extended()
        {
            // Arrange
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);

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
                        expected = String.Format(CultureInfo.InvariantCulture, "[U+{0:X4}]", i);
                    }
                    else
                    {
                        expected = input; // no encoding
                    }
                }

                string retVal = encoder.Encode(input);
                Assert.Equal(expected, retVal);
            }

            // Act & assert - astral chars
            for (int i = 0x10000; i <= 0x10FFFF; i++)
            {
                string input = Char.ConvertFromUtf32(i);
                string expected = String.Format(CultureInfo.InvariantCulture, "[U+{0:X}]", i);
                string retVal = encoder.Encode(input);
                Assert.Equal(expected, retVal);
            }
        }

        [Fact]
        public void Encode_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string expected = "a\uFFFDb\uFFFDc\uFFFD\uFFFDd\uFFFD[U+103FF]e\uFFFD";

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void Encode_EmptyStringInput_ReturnsEmptyString()
        {
            // Arrange
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);

            // Act & assert
            Assert.Equal("", encoder.Encode(""));
        }

        [Fact]
        public void Encode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance()
        {
            // Arrange
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            string input = "Hello, there!";

            // Act & assert
            Assert.Same(input, encoder.Encode(input));
        }

        [Fact]
        public void Encode_NullInput_ReturnsNull()
        {
            // Arrange
            UnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);

            // Act & assert
            Assert.Null(encoder.Encode(null));
        }

        [Fact]
        public void Encode_WithCharsRequiringEncodingAtBeginning()
        {
            Assert.Equal("[U+0026]Hello, there!", new CustomUnicodeEncoderBase(UnicodeBlocks.All).Encode("&Hello, there!"));
        }

        [Fact]
        public void Encode_WithCharsRequiringEncodingAtEnd()
        {
            Assert.Equal("Hello, there![U+0026]", new CustomUnicodeEncoderBase(UnicodeBlocks.All).Encode("Hello, there!&"));
        }

        [Fact]
        public void Encode_WithCharsRequiringEncodingInMiddle()
        {
            Assert.Equal("Hello, [U+0026]there!", new CustomUnicodeEncoderBase(UnicodeBlocks.All).Encode("Hello, &there!"));
        }

        [Fact]
        public void Encode_WithCharsRequiringEncodingInterspersed()
        {
            Assert.Equal("Hello, [U+003C]there[U+003E]!", new CustomUnicodeEncoderBase(UnicodeBlocks.All).Encode("Hello, <there>!"));
        }

        [Fact]
        public void Encode_CharArray_ParameterChecking_NegativeTestCases()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase();

            // Act & assert
            Assert.Throws<ArgumentNullException>(() => encoder.Encode((char[])null, 0, 0, new StringWriter()));
            Assert.Throws<ArgumentNullException>(() => encoder.Encode("abc".ToCharArray(), 0, 3, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc".ToCharArray(), -1, 2, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc".ToCharArray(), 2, 2, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc".ToCharArray(), 4, 0, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc".ToCharArray(), 2, -1, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc".ToCharArray(), 1, 3, new StringWriter()));
        }

        [Fact]
        public void Encode_CharArray_ZeroCount_DoesNotCallIntoTextWriter()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase();
            TextWriter output = new Mock<TextWriter>(MockBehavior.Strict).Object;

            // Act
            encoder.Encode("abc".ToCharArray(), 2, 0, output);

            // Assert
            // If we got this far (without TextWriter throwing), success!
        }

        [Fact]
        public void Encode_CharArray_AllCharsValid()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            StringWriter output = new StringWriter();

            // Act
            encoder.Encode("abc&xyz".ToCharArray(), 4, 2, output);

            // Assert
            Assert.Equal("xy", output.ToString());
        }

        [Fact]
        public void Encode_CharArray_AllCharsInvalid()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.None);
            StringWriter output = new StringWriter();

            // Act
            encoder.Encode("abc&xyz".ToCharArray(), 4, 2, output);

            // Assert
            Assert.Equal("[U+0078][U+0079]", output.ToString());
        }

        [Fact]
        public void Encode_CharArray_SomeCharsValid()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            StringWriter output = new StringWriter();

            // Act
            encoder.Encode("abc&xyz".ToCharArray(), 2, 3, output);

            // Assert
            Assert.Equal("c[U+0026]x", output.ToString());
        }

        [Fact]
        public void Encode_StringSubstring_ParameterChecking_NegativeTestCases()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase();

            // Act & assert
            Assert.Throws<ArgumentNullException>(() => encoder.Encode((string)null, 0, 0, new StringWriter()));
            Assert.Throws<ArgumentNullException>(() => encoder.Encode("abc", 0, 3, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc", -1, 2, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc", 2, 2, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc", 4, 0, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc", 2, -1, new StringWriter()));
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.Encode("abc", 1, 3, new StringWriter()));
        }

        [Fact]
        public void Encode_StringSubstring_ZeroCount_DoesNotCallIntoTextWriter()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase();
            TextWriter output = new Mock<TextWriter>(MockBehavior.Strict).Object;

            // Act
            encoder.Encode("abc", 2, 0, output);

            // Assert
            // If we got this far (without TextWriter throwing), success!
        }

        [Fact]
        public void Encode_StringSubstring_AllCharsValid()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            StringWriter output = new StringWriter();

            // Act
            encoder.Encode("abc&xyz", 4, 2, output);

            // Assert
            Assert.Equal("xy", output.ToString());
        }

        [Fact]
        public void Encode_StringSubstring_EntireString_AllCharsValid_ForwardDirectlyToOutput()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            var mockWriter = new Mock<TextWriter>(MockBehavior.Strict);
            mockWriter.Setup(o => o.Write("abc")).Verifiable();

            // Act
            encoder.Encode("abc", 0, 3, mockWriter.Object);

            // Assert
            mockWriter.Verify();
        }

        [Fact]
        public void Encode_StringSubstring_AllCharsInvalid()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.None);
            StringWriter output = new StringWriter();

            // Act
            encoder.Encode("abc&xyz", 4, 2, output);

            // Assert
            Assert.Equal("[U+0078][U+0079]", output.ToString());
        }

        [Fact]
        public void Encode_StringSubstring_SomeCharsValid()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            StringWriter output = new StringWriter();

            // Act
            encoder.Encode("abc&xyz", 2, 3, output);

            // Assert
            Assert.Equal("c[U+0026]x", output.ToString());
        }

        [Fact]
        public void Encode_StringSubstring_EntireString_SomeCharsValid()
        {
            // Arrange
            CustomUnicodeEncoderBase encoder = new CustomUnicodeEncoderBase(UnicodeBlocks.All);
            StringWriter output = new StringWriter();

            // Act
            const string input = "abc&xyz";
            encoder.Encode(input, 0, input.Length, output);

            // Assert
            Assert.Equal("abc[U+0026]xyz", output.ToString());
        }

        private static bool IsSurrogateCodePoint(int codePoint)
        {
            return (0xD800 <= codePoint && codePoint <= 0xDFFF);
        }

        private sealed class CustomCodePointFilter : ICodePointFilter
        {
            private readonly int[] _allowedCodePoints;

            public CustomCodePointFilter(params int[] allowedCodePoints)
            {
                _allowedCodePoints = allowedCodePoints;
            }

            public IEnumerable<int> GetAllowedCodePoints()
            {
                return _allowedCodePoints;
            }
        }

        private sealed class CustomUnicodeEncoderBase : UnicodeEncoderBase
        {
            // We pass a (known bad) value of 1 for 'max output chars per input char',
            // which also tests that the code behaves properly even if the original
            // estimate is incorrect.
            public CustomUnicodeEncoderBase(CodePointFilter filter)
                : base(filter, maxOutputCharsPerInputChar: 1)
            {
            }

            public CustomUnicodeEncoderBase(params UnicodeBlock[] allowedBlocks)
                : this(new CodePointFilter(allowedBlocks))
            {
            }

            protected override void WriteEncodedScalar(ref Writer writer, uint value)
            {
                writer.Write(String.Format(CultureInfo.InvariantCulture, "[U+{0:X4}]", value));
            }
        }
    }
}
