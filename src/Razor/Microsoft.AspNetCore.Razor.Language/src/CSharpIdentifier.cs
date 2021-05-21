// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal static class CSharpIdentifier
    {
        // CSharp Spec §2.4.2
        private static bool IsIdentifierStart(char character)
        {
            return char.IsLetter(character) ||
                character == '_' ||
                CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
        }

        public static bool IsIdentifierPart(char character)
        {
            return char.IsDigit(character) ||
                   IsIdentifierStart(character) ||
                   IsIdentifierPartByUnicodeCategory(character);
        }

        private static bool IsIdentifierPartByUnicodeCategory(char character)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            return category == UnicodeCategory.NonSpacingMark || // Mn
                category == UnicodeCategory.SpacingCombiningMark || // Mc
                category == UnicodeCategory.ConnectorPunctuation || // Pc
                category == UnicodeCategory.Format; // Cf
        }

        public static string SanitizeIdentifier(string inputName)
        {
            if (string.IsNullOrEmpty(inputName))
            {
                return inputName;
            }

            var builder = new StringBuilder(inputName.Length);
            AppendSanitized(builder, inputName);

            return builder.ToString();
        }

        public static void AppendSanitized(StringBuilder builder, StringSegment inputName)
        {
            if (!IsIdentifierStart(inputName[0]) && IsIdentifierPart(inputName[0]))
            {
                builder.Append('_');
            }

            for (var i = 0; i < inputName.Length; i++)
            {
                var ch = inputName[i];
                builder.Append(IsIdentifierPart(ch) ? ch : '_');
            }
        }
    }
}
