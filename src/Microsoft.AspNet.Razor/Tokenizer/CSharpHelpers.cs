// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

            return category == UnicodeCategory.NonSpacingMark || // Mn
                   category == UnicodeCategory.SpacingCombiningMark || // Mc
                   category == UnicodeCategory.ConnectorPunctuation || // Pc
                   category == UnicodeCategory.Format; // Cf
        }
    }
}
