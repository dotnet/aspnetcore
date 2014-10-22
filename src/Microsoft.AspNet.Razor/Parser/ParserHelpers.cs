// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.Razor.Parser
{
    public static class ParserHelpers
    {
        public static bool IsNewLine(char value)
        {
            return value == '\r' // Carriage return
                   || value == '\n' // Linefeed
                   || value == '\u0085' // Next Line
                   || value == '\u2028' // Line separator
                   || value == '\u2029'; // Paragraph separator
        }

        public static bool IsNewLine(string value)
        {
            return (value.Length == 1 && (IsNewLine(value[0]))) ||
                   (String.Equals(value, "\r\n", StringComparison.Ordinal));
        }

        // Returns true if the character is Whitespace and NOT a newline
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Justification = "This would be a breaking change in a shipping API")]
        public static bool IsWhitespace(char value)
        {
            return value == ' ' ||
                   value == '\f' ||
                   value == '\t' ||
                   value == '\u000B' || // Vertical Tab
                   CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.SpaceSeparator;
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Justification = "This would be a breaking change in a shipping API")]
        public static bool IsWhitespaceOrNewLine(char value)
        {
            return IsWhitespace(value) || IsNewLine(value);
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

        public static bool IsHexDigit(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f');
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

        public static bool IsTerminatingCharToken(char value)
        {
            return IsNewLine(value) || value == '\'';
        }

        public static bool IsTerminatingQuotedStringToken(char value)
        {
            return IsNewLine(value) || value == '"';
        }

        public static bool IsDecimalDigit(char value)
        {
            return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.DecimalDigitNumber;
        }

        public static bool IsLetterOrDecimalDigit(char value)
        {
            return IsLetter(value) || IsDecimalDigit(value);
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

        public static bool IsFormatting(char value)
        {
            return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.Format;
        }

        public static bool IsCombining(char value)
        {
            UnicodeCategory cat= CharUnicodeInfo.GetUnicodeCategory(value);

            return cat == UnicodeCategory.SpacingCombiningMark || cat == UnicodeCategory.NonSpacingMark;

        }

        public static bool IsConnecting(char value)
        {
            return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.ConnectorPunctuation;
        }

        public static string SanitizeClassName(string inputName)
        {
            if (!IsIdentifierStart(inputName[0]) && IsIdentifierPart(inputName[0]))
            {
                inputName = "_" + inputName;
            }

            return new String((from value in inputName
                               select IsIdentifierPart(value) ? value : '_')
                                  .ToArray());
        }

        public static bool IsEmailPart(char character)
        {
            // Source: http://tools.ietf.org/html/rfc5322#section-3.4.1
            // We restrict the allowed characters to alpha-numerics and '_' in order to ensure we cover most of the cases where an
            // email address is intended without restricting the usage of code within JavaScript, CSS, and other contexts.
            return Char.IsLetter(character) || Char.IsDigit(character) || character == '_';
        }
    }
}
