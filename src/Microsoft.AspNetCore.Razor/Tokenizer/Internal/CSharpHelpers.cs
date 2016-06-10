// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Internal
{
    public static class CSharpHelpers
    {
        // CSharp Spec §2.4.2
        public static bool IsIdentifierStart(char character)
        {
            return Char.IsLetter(character) ||
                   character == '_' ||
                   CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
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
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            return category == UnicodeCategory.NonSpacingMark || // Mn
                   category == UnicodeCategory.SpacingCombiningMark || // Mc
                   category == UnicodeCategory.ConnectorPunctuation || // Pc
                   category == UnicodeCategory.Format; // Cf
        }
    }
}
