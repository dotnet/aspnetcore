// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Microsoft.Framework.WebEncoders
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

        [Theory]
        [InlineData(1, "a", (int)'a')] // normal BMP char, end of string
        [InlineData(2, "ab", (int)'a')] // normal BMP char, not end of string
        [InlineData(3, "\uDFFF", UnicodeReplacementChar)] // trailing surrogate, end of string
        [InlineData(4, "\uDFFFx", UnicodeReplacementChar)] // trailing surrogate, not end of string
        [InlineData(5, "\uD800", UnicodeReplacementChar)] // leading surrogate, end of string
        [InlineData(6, "\uD800x", UnicodeReplacementChar)] // leading surrogate, not end of string, followed by non-surrogate
        [InlineData(7, "\uD800\uD800", UnicodeReplacementChar)] // leading surrogate, not end of string, followed by leading surrogate
        [InlineData(8, "\uD800\uDFFF", 0x103FF)] // leading surrogate, not end of string, followed by trailing surrogate
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
                    string message = String.Format(CultureInfo.InvariantCulture, "Character U+{0:X4}: expected = {1}, actual = {2}", i, expected, actual);
                    errors.Add(message);
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
            string[] allLines = new StreamReader(typeof(UnicodeHelpersTests).GetTypeInfo().Assembly.GetManifestResourceStream("../../unicode/UnicodeData.txt")).ReadAllLines();

            foreach (string line in allLines)
            {
                string[] splitLine = line.Split(';');
                uint codePoint = UInt32.Parse(splitLine[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                if (codePoint >= retVal.Length)
                {
                    continue; // don't care about supplementary chars
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

            // Finally, we need to make sure we've seen every category which contains
            // allowed characters. This provides extra defense against having a typo
            // in the list of categories.
            Assert.Equal(allowedCategories.OrderBy(c => c), seenCategories.OrderBy(c => c));

            return retVal;
        }
    }
}
