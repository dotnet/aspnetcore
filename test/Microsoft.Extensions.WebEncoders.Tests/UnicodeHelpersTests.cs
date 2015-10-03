// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Microsoft.Extensions.WebEncoders
{
    public unsafe class UnicodeHelpersTests
    {
        private const int UnicodeReplacementChar = '\uFFFD';

        private static readonly UTF8Encoding _utf8EncodingThrowOnInvalidBytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        [Fact]
        public void GetDefinedCharacterBitmap_ReturnsSingletonInstance()
        {
            // Act
            uint[] retVal1 = UnicodeHelpers.GetDefinedCharacterBitmap();
            uint[] retVal2 = UnicodeHelpers.GetDefinedCharacterBitmap();

            // Assert
            Assert.Same(retVal1, retVal2);
        }

        public static TheoryData<int, string, int> Utf16ScalarValues
        {
            get
            {
                var dataset = new TheoryData<int, string, int>();
                dataset.Add(1, "a", (int)'a'); // normal BMP char, end of string
                dataset.Add(2, "ab", (int)'a'); // normal BMP char, not end of string
                dataset.Add(3, "\uDFFF", UnicodeReplacementChar); // trailing surrogate, end of string
                dataset.Add(4, "\uDFFFx", UnicodeReplacementChar); // trailing surrogate, not end of string
                dataset.Add(5, "\uD800", UnicodeReplacementChar); // leading surrogate, end of string
                dataset.Add(6, "\uD800x", UnicodeReplacementChar); // leading surrogate, not end of string, followed by non-surrogate
                dataset.Add(7, "\uD800\uD800", UnicodeReplacementChar); // leading surrogate, not end of string, followed by leading surrogate
                dataset.Add(8, "\uD800\uDFFF", 0x103FF); // leading surrogate, not end of string, followed by trailing surrogate

                return dataset;
            }
        }

        [Theory]
        [MemberData(nameof(Utf16ScalarValues))]
        public void GetScalarValueFromUtf16(int unused, string input, int expectedResult)
        {
            // The 'unused' parameter exists because the xunit runner can't distinguish
            // the individual malformed data test cases from each other without this
            // additional identifier.

            fixed (char* pInput = input)
            {
                Assert.Equal(expectedResult, UnicodeHelpers.GetScalarValueFromUtf16(pInput, endOfString: (input.Length == 1)));
            }
        }

        [Fact]
        public void GetUtf8RepresentationForScalarValue()
        {
            for (int i = 0; i <= 0x10FFFF; i++)
            {
                if (i <= 0xFFFF && Char.IsSurrogate((char)i))
                {
                    continue; // no surrogates
                }

                // Arrange
                byte[] expectedUtf8Bytes = _utf8EncodingThrowOnInvalidBytes.GetBytes(Char.ConvertFromUtf32(i));

                // Act
                List<byte> actualUtf8Bytes = new List<byte>(4);
                uint asUtf8 = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)i);
                do
                {
                    actualUtf8Bytes.Add((byte)asUtf8);
                } while ((asUtf8 >>= 8) != 0);

                // Assert
                Assert.Equal(expectedUtf8Bytes, actualUtf8Bytes);
            }
        }

        [Fact]
        public void IsCharacterDefined()
        {
            // Arrange
            bool[] definedChars = ReadListOfDefinedCharacters();
            List<string> errors = new List<string>();

            // Act & assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                bool expected = definedChars[i];
                bool actual = UnicodeHelpers.IsCharacterDefined((char)i);
                if (expected != actual)
                {
                    errors.Add($"Character U+{i:X4}: expected = {expected}, actual = {actual}");
                }
            }

            if (errors.Count > 0)
            {
                Assert.True(false, String.Join(Environment.NewLine, errors));
            }
        }

        private static bool[] ReadListOfDefinedCharacters()
        {
            HashSet<string> allowedCategories = new HashSet<string>();

            // Letters
            allowedCategories.Add("Lu");
            allowedCategories.Add("Ll");
            allowedCategories.Add("Lt");
            allowedCategories.Add("Lm");
            allowedCategories.Add("Lo");

            // Marks
            allowedCategories.Add("Mn");
            allowedCategories.Add("Mc");
            allowedCategories.Add("Me");

            // Numbers
            allowedCategories.Add("Nd");
            allowedCategories.Add("Nl");
            allowedCategories.Add("No");

            // Punctuation
            allowedCategories.Add("Pc");
            allowedCategories.Add("Pd");
            allowedCategories.Add("Ps");
            allowedCategories.Add("Pe");
            allowedCategories.Add("Pi");
            allowedCategories.Add("Pf");
            allowedCategories.Add("Po");

            // Symbols
            allowedCategories.Add("Sm");
            allowedCategories.Add("Sc");
            allowedCategories.Add("Sk");
            allowedCategories.Add("So");

            // Separators
            // With the exception of U+0020 SPACE, these aren't allowed

            // Other
            // We only allow one category of 'other' characters
            allowedCategories.Add("Cf");

            HashSet<string> seenCategories = new HashSet<string>();

            bool[] retVal = new bool[0x10000];

            var assembly = typeof(UnicodeHelpersTests).GetTypeInfo().Assembly;
            var resourceName = assembly.GetName().Name + ".UnicodeData.txt";
            string[] allLines = new StreamReader(assembly.GetManifestResourceStream(resourceName)).ReadAllLines();

            foreach (string line in allLines)
            {
                string[] splitLine = line.Split(';');
                uint codePoint = UInt32.Parse(splitLine[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                string name = splitLine[1];
                if (codePoint >= retVal.Length)
                {
                    continue; // don't care about supplementary chars
                }

                if (name.EndsWith(", First>", StringComparison.Ordinal) || name.EndsWith(", Last>", StringComparison.Ordinal))
                {
                    // ignore spans - we'll handle them separately
                    continue;
                }

                if (codePoint == (uint)' ')
                {
                    retVal[codePoint] = true; // we allow U+0020 SPACE as our only valid Zs (whitespace) char
                }
                else
                {
                    string category = splitLine[2];
                    if (allowedCategories.Contains(category))
                    {
                        retVal[codePoint] = true; // chars in this category are allowable
                        seenCategories.Add(category);
                    }
                }
            }

            // Handle known spans from Unicode 8.0's UnicodeData.txt

            // CJK Ideograph Extension A
            for (int i = '\u3400'; i <= '\u4DB5'; i++)
            {
                retVal[i] = true;
            }
            // CJK Ideograph
            for (int i = '\u4E00'; i <= '\u9FD5'; i++)
            {
                retVal[i] = true;
            }
            // Hangul Syllable
            for (int i = '\uAC00'; i <= '\uD7A3'; i++)
            {
                retVal[i] = true;
            }

            // Finally, we need to make sure we've seen every category which contains
            // allowed characters. This provides extra defense against having a typo
            // in the list of categories.
            Assert.Equal(allowedCategories.OrderBy(c => c), seenCategories.OrderBy(c => c));

            return retVal;
        }
    }
}
