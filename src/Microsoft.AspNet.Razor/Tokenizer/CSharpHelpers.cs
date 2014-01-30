// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNet.Razor.Tokenizer
{
    public static class CSharpHelpers
    {
        // CSharp Spec §2.4.2
        public static bool IsIdentifierStart(char character)
        {
            return Char.IsLetter(character) ||
                   character == '_' ||
#if NET45 
                   // No GetUnicodeCategory on Char in CoreCLR

                   Char.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
#else
                   CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
#endif
        }

        public static bool IsIdentifierPart(char character)
        {
            return Char.IsDigit(character) ||
                   IsIdentifierStart(character) ||
                   IsIdentifierPartByUnicodeCategory(character);
        }

        public static bool IsRealLiteralSuffix(char character)
        {
            return character == 'F' ||
                   character == 'f' ||
                   character == 'D' ||
                   character == 'd' ||
                   character == 'M' ||
                   character == 'm';
        }

        private static bool IsIdentifierPartByUnicodeCategory(char character)
        {
            UnicodeCategory category;
#if NET45 
            // No GetUnicodeCategory on Char in CoreCLR

            category = Char.GetUnicodeCategory(character);
#else
            category = CharUnicodeInfo.GetUnicodeCategory(character);
#endif

            return category == UnicodeCategory.NonSpacingMark || // Mn
                   category == UnicodeCategory.SpacingCombiningMark || // Mc
                   category == UnicodeCategory.ConnectorPunctuation || // Pc
                   category == UnicodeCategory.Format; // Cf
        }
    }
}
