// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Provides utilities to parse and modify HTTP header values.
/// </summary>
public static class HeaderUtilities
{
    private const int _qualityValueMaxCharCount = 10; // Little bit more permissive than RFC7231 5.3.1
    private const string QualityName = "q";
    internal const string BytesUnit = "bytes";

    internal static void SetQuality(IList<NameValueHeaderValue> parameters, double? value)
    {
        var qualityParameter = NameValueHeaderValue.Find(parameters, QualityName);
        if (value.HasValue)
        {
            // Note that even if we check the value here, we can't prevent a user from adding an invalid quality
            // value using Parameters.Add(). Even if we would prevent the user from adding an invalid value
            // using Parameters.Add() they could always add invalid values using HttpHeaders.AddWithoutValidation().
            // So this check is really for convenience to show users that they're trying to add an invalid
            // value.
            if ((value < 0) || (value > 1))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var qualityString = ((double)value).ToString("0.0##", NumberFormatInfo.InvariantInfo);
            if (qualityParameter != null)
            {
                qualityParameter.Value = qualityString;
            }
            else
            {
                parameters.Add(new NameValueHeaderValue(QualityName, qualityString));
            }
        }
        else
        {
            // Remove quality parameter
            if (qualityParameter != null)
            {
                parameters.Remove(qualityParameter);
            }
        }
    }

    internal static double? GetQuality(IList<NameValueHeaderValue>? parameters)
    {
        var qualityParameter = NameValueHeaderValue.Find(parameters, QualityName);
        if (qualityParameter != null)
        {
            // Note that the RFC requires decimal '.' regardless of the culture. I.e. using ',' as decimal
            // separator is considered invalid (even if the current culture would allow it).
            if (TryParseQualityDouble(qualityParameter.Value, 0, out var qualityValue, out _))
            {
                return qualityValue;
            }
        }
        return null;
    }

    internal static void CheckValidToken(StringSegment value, string parameterName)
    {
        if (StringSegment.IsNullOrEmpty(value))
        {
            throw new ArgumentException("An empty string is not allowed.", parameterName);
        }

        if (HttpRuleParser.GetTokenLength(value, 0) != value.Length)
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Invalid token '{0}'.", value));
        }
    }

    internal static bool AreEqualCollections<T>(ICollection<T>? x, ICollection<T>? y)
    {
        return AreEqualCollections(x, y, null);
    }

    internal static bool AreEqualCollections<T>(ICollection<T>? x, ICollection<T>? y, IEqualityComparer<T>? comparer)
    {
        if (x == null)
        {
            return (y == null) || (y.Count == 0);
        }

        if (y == null)
        {
            return (x.Count == 0);
        }

        if (x.Count != y.Count)
        {
            return false;
        }

        if (x.Count == 0)
        {
            return true;
        }

        // We have two unordered lists. So comparison is an O(n*m) operation which is expensive. Usually
        // headers have 1-2 parameters (if any), so this comparison shouldn't be too expensive.
        var alreadyFound = new bool[x.Count];
        var i = 0;
        foreach (var xItem in x)
        {
            Contract.Assert(xItem != null);

            i = 0;
            var found = false;
            foreach (var yItem in y)
            {
                if (!alreadyFound[i])
                {
                    if (((comparer == null) && xItem.Equals(yItem)) ||
                        ((comparer != null) && comparer.Equals(xItem, yItem)))
                    {
                        alreadyFound[i] = true;
                        found = true;
                        break;
                    }
                }
                i++;
            }

            if (!found)
            {
                return false;
            }
        }

        // Since we never re-use a "found" value in 'y', we expected 'alreadyFound' to have all fields set to 'true'.
        // Otherwise the two collections can't be equal and we should not get here.
        Contract.Assert(Contract.ForAll(alreadyFound, value => { return value; }),
            "Expected all values in 'alreadyFound' to be true since collections are considered equal.");

        return true;
    }

    internal static int GetNextNonEmptyOrWhitespaceIndex(
        StringSegment input,
        int startIndex,
        bool skipEmptyValues,
        out bool separatorFound)
    {
        Contract.Requires(startIndex <= input.Length); // it's OK if index == value.Length.

        separatorFound = false;
        var current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);

        if ((current == input.Length) || (input[current] != ','))
        {
            return current;
        }

        // If we have a separator, skip the separator and all following whitespaces. If we support
        // empty values, continue until the current character is neither a separator nor a whitespace.
        separatorFound = true;
        current++; // skip delimiter.
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        if (skipEmptyValues)
        {
            while ((current < input.Length) && (input[current] == ','))
            {
                current++; // skip delimiter.
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }
        }

        return current;
    }

    private static int AdvanceCacheDirectiveIndex(int current, string headerValue)
    {
        // Skip until the next potential name
        current += HttpRuleParser.GetWhitespaceLength(headerValue, current);

        // Skip the value if present
        if (current < headerValue.Length && headerValue[current] == '=')
        {
            current++; // skip '='
            current += NameValueHeaderValue.GetValueLength(headerValue, current);
        }

        // Find the next delimiter
        current = headerValue.IndexOf(',', current);

        if (current == -1)
        {
            // If no delimiter found, skip to the end
            return headerValue.Length;
        }

        current++; // skip ','
        current += HttpRuleParser.GetWhitespaceLength(headerValue, current);

        return current;
    }

    /// <summary>
    /// Try to find a target header value among the set of given header values and parse it as a
    /// <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="headerValues">
    /// The <see cref="StringValues"/> containing the set of header values to search.
    /// </param>
    /// <param name="targetValue">
    /// The target header value to look for.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the parsed <see cref="TimeSpan"/>, if the parsing succeeded, or
    /// null if the parsing failed. The conversion fails if the <paramref name="targetValue"/> was not
    /// found or could not be parsed as a <see cref="TimeSpan"/>. This parameter is passed uninitialized;
    /// any value originally supplied in result will be overwritten.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="targetValue"/> is found and successfully parsed; otherwise,
    /// <see langword="false" />.
    /// </returns>
    // e.g. { "headerValue=10, targetHeaderValue=30" }
    public static bool TryParseSeconds(StringValues headerValues, string targetValue, [NotNullWhen(true)] out TimeSpan? value)
    {
        if (StringValues.IsNullOrEmpty(headerValues) || string.IsNullOrEmpty(targetValue))
        {
            value = null;
            return false;
        }

        for (var i = 0; i < headerValues.Count; i++)
        {
            var segment = headerValues[i] ?? string.Empty;

            // Trim leading white space
            var current = HttpRuleParser.GetWhitespaceLength(segment, 0);

            while (current < segment.Length)
            {
                long seconds;
                var initial = current;
                var tokenLength = HttpRuleParser.GetTokenLength(headerValues[i], current);
                if (tokenLength == targetValue.Length
                    && string.Compare(headerValues[i], current, targetValue, 0, tokenLength, StringComparison.OrdinalIgnoreCase) == 0
                    && TryParseNonNegativeInt64FromHeaderValue(current + tokenLength, segment, out seconds))
                {
                    // Token matches target value and seconds were parsed
                    value = TimeSpan.FromSeconds(seconds);
                    return true;
                }

                current = AdvanceCacheDirectiveIndex(current + tokenLength, segment);

                // Ensure index was advanced
                if (current <= initial)
                {
                    Debug.Assert(false, $"Index '{nameof(current)}' not advanced, this is a bug.");
                    value = null;
                    return false;
                }
            }
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Check if a target directive exists among the set of given cache control directives.
    /// </summary>
    /// <param name="cacheControlDirectives">
    /// The <see cref="StringValues"/> containing the set of cache control directives.
    /// </param>
    /// <param name="targetDirectives">
    /// The target cache control directives to look for.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="targetDirectives"/> is contained in <paramref name="cacheControlDirectives"/>;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool ContainsCacheDirective(StringValues cacheControlDirectives, string targetDirectives)
    {
        if (StringValues.IsNullOrEmpty(cacheControlDirectives) || string.IsNullOrEmpty(targetDirectives))
        {
            return false;
        }

        for (var i = 0; i < cacheControlDirectives.Count; i++)
        {
            var segment = cacheControlDirectives[i] ?? string.Empty;

            // Trim leading white space
            var current = HttpRuleParser.GetWhitespaceLength(segment, 0);

            while (current < segment.Length)
            {
                var initial = current;

                var tokenLength = HttpRuleParser.GetTokenLength(segment, current);
                if (tokenLength == targetDirectives.Length
                    && string.Compare(segment, current, targetDirectives, 0, tokenLength, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Token matches target value
                    return true;
                }

                current = AdvanceCacheDirectiveIndex(current + tokenLength, segment);

                // Ensure index was advanced
                if (current <= initial)
                {
                    Debug.Assert(false, $"Index '{nameof(current)}' not advanced, this is a bug.");
                    return false;
                }
            }
        }

        return false;
    }

    private static bool TryParseNonNegativeInt64FromHeaderValue(int startIndex, string headerValue, out long result)
    {
        // Trim leading whitespace
        startIndex += HttpRuleParser.GetWhitespaceLength(headerValue, startIndex);

        // Match and skip '=', it also can't be the last character in the headerValue
        if (startIndex >= headerValue.Length - 1 || headerValue[startIndex] != '=')
        {
            result = 0;
            return false;
        }
        startIndex++;

        // Trim trailing whitespace
        startIndex += HttpRuleParser.GetWhitespaceLength(headerValue, startIndex);

        // Try parse the number
        if (TryParseNonNegativeInt64(new StringSegment(headerValue, startIndex, HttpRuleParser.GetNumberLength(headerValue, startIndex, false)), out result))
        {
            return true;
        }

        result = 0;
        return false;
    }

    /// <summary>
    /// Try to convert a string representation of a positive number to its 64-bit signed integer equivalent.
    /// A return value indicates whether the conversion succeeded or failed.
    /// </summary>
    /// <param name="value">
    /// A string containing a number to convert.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the 64-bit signed integer value equivalent of the number contained
    /// in the string, if the conversion succeeded, or zero if the conversion failed. The conversion fails if
    /// the string is null or String.Empty, is not of the correct format, is negative, or represents a number
    /// greater than Int64.MaxValue. This parameter is passed uninitialized; any value originally supplied in
    /// result will be overwritten.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseNonNegativeInt32(StringSegment value, out int result)
    {
        if (string.IsNullOrEmpty(value.Buffer) || value.Length == 0)
        {
            result = 0;
            return false;
        }

        return int.TryParse(value.AsSpan(), NumberStyles.None, NumberFormatInfo.InvariantInfo, out result);
    }

    /// <summary>
    /// Try to convert a <see cref="StringSegment"/> representation of a positive number to its 64-bit signed
    /// integer equivalent. A return value indicates whether the conversion succeeded or failed.
    /// </summary>
    /// <param name="value">
    /// A <see cref="StringSegment"/> containing a number to convert.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the 64-bit signed integer value equivalent of the number contained
    /// in the string, if the conversion succeeded, or zero if the conversion failed. The conversion fails if
    /// the <see cref="StringSegment"/> is null or String.Empty, is not of the correct format, is negative, or
    /// represents a number greater than Int64.MaxValue. This parameter is passed uninitialized; any value
    /// originally supplied in result will be overwritten.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseNonNegativeInt64(StringSegment value, out long result)
    {
        if (string.IsNullOrEmpty(value.Buffer) || value.Length == 0)
        {
            result = 0;
            return false;
        }
        return long.TryParse(value.AsSpan(), NumberStyles.None, NumberFormatInfo.InvariantInfo, out result);
    }

    // Strict and fast RFC9110 12.4.2 Quality value parser (and without memory allocation)
    // See https://tools.ietf.org/html/rfc9110#section-12.4.2
    // Check is made to verify if the value is between 0 and 1 (and it returns False if the check fails).
    internal static bool TryParseQualityDouble(StringSegment input, int startIndex, out double quality, out int length)
    {
        quality = 0;
        length = 0;

        var inputLength = input.Length;
        var current = startIndex;
        var limit = startIndex + _qualityValueMaxCharCount;
        var decPart = 0;
        var decPow = 1;

        if (current >= inputLength)
        {
            return false;
        }

        var ch = input[current];

        int intPart;
        if (ch >= '0' && ch <= '1') // Only values between 0 and 1 are accepted, according to RFC
        {
            intPart = ch - '0';
            current++;
        }
        else
        {
            // The RFC doesn't allow decimal values starting with dot. I.e. value ".123" is invalid. It must be in the
            // form "0.123".
            return false;
        }

        if (current < inputLength)
        {
            ch = input[current];

            if (ch >= '0' && ch <= '9')
            {
                // The RFC accepts only one digit before the dot
                return false;
            }

            if (ch == '.')
            {
                current++;

                while (current < inputLength)
                {
                    ch = input[current];
                    if (ch >= '0' && ch <= '9')
                    {
                        if (current >= limit)
                        {
                            return false;
                        }

                        decPart = decPart * 10 + ch - '0';
                        decPow *= 10;
                        current++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        if (decPart != 0)
        {
            quality = intPart + decPart / (double)decPow;
        }
        else
        {
            quality = intPart;
        }

        if (quality > 1)
        {
            // reset quality
            quality = 0;
            return false;
        }

        length = current - startIndex;
        return true;
    }

    /// <summary>
    /// Converts the non-negative 64-bit numeric value to its equivalent string representation.
    /// </summary>
    /// <param name="value">
    /// The number to convert.
    /// </param>
    /// <returns>
    /// The string representation of the value of this instance, consisting of a sequence of digits ranging from 0 to 9 with no leading zeroes.
    /// </returns>
    public static string FormatNonNegativeInt64(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The value to be formatted must be non-negative.");
        }

        if (value == 0)
        {
            return "0";
        }

        return ((ulong)value).ToString(NumberFormatInfo.InvariantInfo);
    }

    /// <summary>
    /// Converts the 64-bit numeric value to its equivalent string representation.
    /// </summary>
    /// <param name="value">
    /// The number to convert.
    /// </param>
    /// <returns>
    /// The string representation of the value of this instance, consisting of a sequence of digits ranging from 0 to 9 with no leading zeroes.
    /// In case of negative numeric value it will have a leading minus sign.
    /// </returns>
    internal static string FormatInt64(long value)
    {
        return value switch
        {
            0 => "0",
            1 => "1",
            -1 => "-1",
            _ => value.ToString(NumberFormatInfo.InvariantInfo)
        };
    }

    /// <summary>
    ///Attempts to parse the specified <paramref name="input"/> as a <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The parsed value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="input"/> can be parsed as a date, otherwise <see langword="false" />.
    /// </returns>
    public static bool TryParseDate(StringSegment input, out DateTimeOffset result)
    {
        return HttpRuleParser.TryStringToDate(input, out result);
    }

    /// <summary>
    /// Formats the <paramref name="dateTime"/> using the RFC1123 format specifier.
    /// </summary>
    /// <param name="dateTime">The date to format.</param>
    /// <returns>The formatted date.</returns>
    public static string FormatDate(DateTimeOffset dateTime)
    {
        return FormatDate(dateTime, quoted: false);
    }

    /// <summary>
    /// Formats the <paramref name="dateTime"/> using the RFC1123 format specifier and optionally quotes it.
    /// </summary>
    /// <param name="dateTime">The date to format.</param>
    /// <param name="quoted">Determines if the formatted date should be quoted.</param>
    /// <returns>The formatted date.</returns>
    public static string FormatDate(DateTimeOffset dateTime, bool quoted)
    {
        if (quoted)
        {
            return string.Create(31, dateTime, (span, dt) =>
            {
                span[0] = span[30] = '"';
                dt.TryFormat(span.Slice(1), out _, "r", CultureInfo.InvariantCulture);
            });
        }

        return dateTime.ToString("r", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Removes quotes from the specified <paramref name="input"/> if quoted.
    /// </summary>
    /// <param name="input">The input to remove quotes from.</param>
    /// <returns>The value without quotes.</returns>
    public static StringSegment RemoveQuotes(StringSegment input)
    {
        if (IsQuoted(input))
        {
            input = input.Subsegment(1, input.Length - 2);
        }
        return input;
    }

    /// <summary>
    /// Determines if the specified <paramref name="input"/> is quoted.
    /// </summary>
    /// <param name="input">The value to inspect.</param>
    /// <returns><see langword="true"/> if the value is quoted, otherwise <see langword="false"/>.</returns>
    public static bool IsQuoted(StringSegment input)
    {
        return !StringSegment.IsNullOrEmpty(input) && input.Length >= 2 && input[0] == '"' && input[input.Length - 1] == '"';
    }

    /// <summary>
    /// Given a quoted-string as defined by <see href="https://tools.ietf.org/html/rfc7230#section-3.2.6">the RFC specification</see>,
    /// removes quotes and unescapes backslashes and quotes. This assumes that the input is a valid quoted-string.
    /// </summary>
    /// <param name="input">The quoted-string to be unescaped.</param>
    /// <returns>An unescaped version of the quoted-string.</returns>
    public static StringSegment UnescapeAsQuotedString(StringSegment input)
    {
        input = RemoveQuotes(input);

        // First pass to calculate the size of the string
        var backSlashCount = CountBackslashesForDecodingQuotedString(input);

        if (backSlashCount == 0)
        {
            return input;
        }

        return string.Create(input.Length - backSlashCount, input, (span, segment) =>
        {
            var spanIndex = 0;
            var spanLength = span.Length;
            for (var i = 0; i < segment.Length && (uint)spanIndex < (uint)spanLength; i++)
            {
                int nextIndex = i + 1;
                if ((uint)nextIndex < (uint)segment.Length && segment[i] == '\\')
                {
                    // If there is an backslash character as the last character in the string,
                    // we will assume that it should be included literally in the unescaped string
                    // Ex: "hello\\" => "hello\\"
                    // Also, if a sender adds a quoted pair like '\\''n',
                    // we will assume it is over escaping and just add a n to the string.
                    // Ex: "he\\llo" => "hello"
                    span[spanIndex] = segment[nextIndex];
                    i++;
                }
                else
                {
                    span[spanIndex] = segment[i];
                }

                spanIndex++;
            }
        });
    }

    private static int CountBackslashesForDecodingQuotedString(StringSegment input)
    {
        var numberBackSlashes = 0;
        for (var i = 0; i < input.Length; i++)
        {
            if (i < input.Length - 1 && input[i] == '\\')
            {
                // If there is an backslash character as the last character in the string,
                // we will assume that it should be included literally in the unescaped string
                // Ex: "hello\\" => "hello\\"
                // Also, if a sender adds a quoted pair like '\\''n',
                // we will assume it is over escaping and just add a n to the string.
                // Ex: "he\\llo" => "hello"
                if (input[i + 1] == '\\')
                {
                    // Only count escaped backslashes once
                    i++;
                }
                numberBackSlashes++;
            }
        }
        return numberBackSlashes;
    }

    /// <summary>
    /// Escapes a <see cref="StringSegment"/> as a quoted-string, which is defined by
    /// <see href="https://tools.ietf.org/html/rfc7230#section-3.2.6">the RFC specification</see>.
    /// </summary>
    /// <remarks>
    /// This will add a backslash before each backslash and quote and add quotes
    /// around the input. Assumes that the input does not have quotes around it,
    /// as this method will add them. Throws if the input contains any invalid escape characters,
    /// as defined by rfc7230.
    /// </remarks>
    /// <param name="input">The input to be escaped.</param>
    /// <returns>An escaped version of the quoted-string.</returns>
    public static StringSegment EscapeAsQuotedString(StringSegment input)
    {
        // By calling this, we know that the string requires quotes around it to be a valid token.
        var backSlashCount = CountAndCheckCharactersNeedingBackslashesWhenEncoding(input);

        // 2 for quotes
        return string.Create(input.Length + backSlashCount + 2, input, (span, segment) =>
        {
            // Helps to elide the bounds check for span[0]
            span[span.Length - 1] = span[0] = '\"';

            var spanIndex = 1;
            for (var i = 0; i < segment.Length; i++)
            {
                if (segment[i] == '\\' || segment[i] == '\"')
                {
                    span[spanIndex++] = '\\';
                }
                else if ((segment[i] <= 0x1F || segment[i] == 0x7F) && segment[i] != 0x09)
                {
                    // Control characters are not allowed in a quoted-string, which include all characters
                    // below 0x1F (except for 0x09 (TAB)) and 0x7F.
                    throw new FormatException($"Invalid control character '{segment[i]}' in input.");
                }
                span[spanIndex++] = segment[i];
            }
        });
    }

    private static int CountAndCheckCharactersNeedingBackslashesWhenEncoding(StringSegment input)
    {
        var numberOfCharactersNeedingEscaping = 0;
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '\\' || input[i] == '\"')
            {
                numberOfCharactersNeedingEscaping++;
            }
        }
        return numberOfCharactersNeedingEscaping;
    }

    internal static void ThrowIfReadOnly(bool isReadOnly)
    {
        if (isReadOnly)
        {
            throw new InvalidOperationException("The object cannot be modified because it is read-only.");
        }
    }
}
