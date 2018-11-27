// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal static class ParserHelpers
    {
        public static char[] NewLineCharacters = new[]
        {
            '\r', // Carriage return
            '\n', // Linefeed
            '\u0085', // Next Line
            '\u2028', // Line separator
            '\u2029' // Paragraph separator
        };

        public static bool IsNewLine(char value)
        {
            return NewLineCharacters.Contains(value);
        }

        public static bool IsNewLine(string value)
        {
            return (value.Length == 1 && (IsNewLine(value[0]))) ||
                   (string.Equals(value, Environment.NewLine, StringComparison.Ordinal));
        }

        public static bool IsIdentifier(string value)
        {
            return IsIdentifier(value, requireIdentifierStart: true);
        }

        public static bool IsIdentifier(string value, bool requireIdentifierStart)
        {
            IEnumerable<char> identifierPart = value;
            if (requireIdentifierStart)
            {
                identifierPart = identifierPart.Skip(1);
            }
            return (!requireIdentifierStart || IsIdentifierStart(value[0])) && identifierPart.All(IsIdentifierPart);
        }

        public static bool IsIdentifierStart(char value)
        {
            return value == '_' || IsLetter(value);
        }

        public static bool IsIdentifierPart(char value)
        {
            return IsLetter(value)
                || IsDecimalDigit(value)
                || IsConnecting(value)
                || IsCombining(value)
                || IsFormatting(value);
        }

        public static bool IsFormatting(char value)
        {
            return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.Format;
        }

        public static bool IsCombining(char value)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(value);

            return cat == UnicodeCategory.SpacingCombiningMark || cat == UnicodeCategory.NonSpacingMark;
        }

        public static bool IsConnecting(char value)
        {
            return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.ConnectorPunctuation;
        }

        public static bool IsWhitespace(char value)
        {
            return value == ' ' ||
                   value == '\f' ||
                   value == '\t' ||
                   value == '\u000B' || // Vertical Tab
                   CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.SpaceSeparator;
        }

        public static bool IsLetter(char value)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(value);

            return cat == UnicodeCategory.UppercaseLetter
                   || cat == UnicodeCategory.LowercaseLetter
                   || cat == UnicodeCategory.TitlecaseLetter
                   || cat == UnicodeCategory.ModifierLetter
                   || cat == UnicodeCategory.OtherLetter
                   || cat == UnicodeCategory.LetterNumber;
        }

        public static bool IsDecimalDigit(char value)
        {
            return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.DecimalDigitNumber;
        }
    }
}
