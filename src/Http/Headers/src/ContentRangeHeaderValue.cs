// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Represents a <c>Content-Range</c> response HTTP header.
/// </summary>
public class ContentRangeHeaderValue
{
    private static readonly HttpHeaderParser<ContentRangeHeaderValue> Parser
        = new GenericHeaderParser<ContentRangeHeaderValue>(false, GetContentRangeLength);

    private StringSegment _unit;

    private ContentRangeHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContentRangeHeaderValue"/>.
    /// </summary>
    /// <param name="from">The start of the range.</param>
    /// <param name="to">The end of the range.</param>
    /// <param name="length">The total size of the document in bytes.</param>
    public ContentRangeHeaderValue(long from, long to, long length)
    {
        // Scenario: "Content-Range: bytes 12-34/5678"

        ArgumentOutOfRangeException.ThrowIfNegative(length);

        // "To" is inclusive. Per RFC 7233:
        // A Content-Range field value is invalid if it contains a byte-range-resp that has a
        // last-byte-pos value less than its first-byte-pos value, or a complete-length value
        // less than or equal to its last-byte-pos value.
        if ((to < 0) || (length <= to))
        {
            throw new ArgumentOutOfRangeException(nameof(to));
        }

        if ((from < 0) || (from > to))
        {
            throw new ArgumentOutOfRangeException(nameof(from));
        }

        From = from;
        To = to;
        Length = length;
        _unit = HeaderUtilities.BytesUnit;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContentRangeHeaderValue"/>.
    /// </summary>
    /// <param name="length">The total size of the document in bytes.</param>
    public ContentRangeHeaderValue(long length)
    {
        // Scenario: "Content-Range: bytes */1234"

        ArgumentOutOfRangeException.ThrowIfNegative(length);

        Length = length;
        _unit = HeaderUtilities.BytesUnit;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContentRangeHeaderValue"/>.
    /// </summary>
    /// <param name="from">The start of the range.</param>
    /// <param name="to">The end of the range.</param>
    public ContentRangeHeaderValue(long from, long to)
    {
        // Scenario: "Content-Range: bytes 12-34/*"

        ArgumentOutOfRangeException.ThrowIfNegative(to);
        if ((from < 0) || (from > to))
        {
            throw new ArgumentOutOfRangeException(nameof(@from));
        }

        From = from;
        To = to;
        _unit = HeaderUtilities.BytesUnit;
    }

    /// <summary>
    /// Gets or sets the unit in which ranges are specified.
    /// </summary>
    /// <value>Defaults to <c>bytes</c>.</value>
    public StringSegment Unit
    {
        get { return _unit; }
        set
        {
            HeaderUtilities.CheckValidToken(value, nameof(value));
            _unit = value;
        }
    }

    /// <summary>
    /// Gets the start of the range.
    /// </summary>
    public long? From { get; private set; }

    /// <summary>
    /// Gets the end of the range.
    /// </summary>
    public long? To { get; private set; }

    /// <summary>
    /// Gets the total size of the document.
    /// </summary>
    [NotNullIfNotNull(nameof(Length))]
    public long? Length { get; private set; }

    /// <summary>
    /// Gets a value that determines if <see cref="Length"/> has been specified.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Length))]
    public bool HasLength // e.g. "Content-Range: bytes 12-34/*"
    {
        get { return Length != null; }
    }

    /// <summary>
    /// Gets a value that determines if <see cref="From"/> and <see cref="To"/> have been specified.
    /// </summary>
    [MemberNotNullWhen(true, nameof(From), nameof(To))]
    public bool HasRange // e.g. "Content-Range: bytes */1234"
    {
        get { return From != null && To != null; }
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        var other = obj as ContentRangeHeaderValue;

        if (other == null)
        {
            return false;
        }

        return ((From == other.From) && (To == other.To) && (Length == other.Length) &&
            StringSegment.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var result = StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(Unit);

        if (HasRange)
        {
            result = result ^ From.GetHashCode() ^ To.GetHashCode();
        }

        if (HasLength)
        {
            result = result ^ Length.GetHashCode();
        }

        return result;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Unit.AsSpan());
        sb.Append(' ');

        if (HasRange)
        {
            sb.Append(From.GetValueOrDefault().ToString(NumberFormatInfo.InvariantInfo));
            sb.Append('-');
            sb.Append(To.GetValueOrDefault().ToString(NumberFormatInfo.InvariantInfo));
        }
        else
        {
            sb.Append('*');
        }

        sb.Append('/');
        if (HasLength)
        {
            sb.Append(Length.GetValueOrDefault().ToString(NumberFormatInfo.InvariantInfo));
        }
        else
        {
            sb.Append('*');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="ContentRangeHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static ContentRangeHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return Parser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="ContentRangeHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="ContentRangeHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out ContentRangeHeaderValue? parsedValue)
    {
        var index = 0;
        return Parser.TryParseValue(input, ref index, out parsedValue);
    }

    private static int GetContentRangeLength(StringSegment input, int startIndex, out ContentRangeHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
        {
            return 0;
        }

        // Parse the unit string: <unit> in '<unit> <from>-<to>/<length>'
        var unitLength = HttpRuleParser.GetTokenLength(input, startIndex);

        if (unitLength == 0)
        {
            return 0;
        }

        var unit = input.Subsegment(startIndex, unitLength);
        var current = startIndex + unitLength;
        var separatorLength = HttpRuleParser.GetWhitespaceLength(input, current);

        if (separatorLength == 0)
        {
            return 0;
        }

        current = current + separatorLength;

        if (current == input.Length)
        {
            return 0;
        }

        // Read range values <from> and <to> in '<unit> <from>-<to>/<length>'
        var fromStartIndex = current;
        if (!TryGetRangeLength(input, ref current, out var fromLength, out var toStartIndex, out var toLength))
        {
            return 0;
        }

        // After the range is read we expect the length separator '/'
        if ((current == input.Length) || (input[current] != '/'))
        {
            return 0;
        }

        current++; // Skip '/' separator
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        if (current == input.Length)
        {
            return 0;
        }

        // We may not have a length (e.g. 'bytes 1-2/*'). But if we do, parse the length now.
        var lengthStartIndex = current;
        if (!TryGetLengthLength(input, ref current, out var lengthLength))
        {
            return 0;
        }

        if (!TryCreateContentRange(input, unit, fromStartIndex, fromLength, toStartIndex, toLength,
            lengthStartIndex, lengthLength, out parsedValue))
        {
            return 0;
        }

        return current - startIndex;
    }

    private static bool TryGetLengthLength(StringSegment input, ref int current, out int lengthLength)
    {
        lengthLength = 0;

        if (input[current] == '*')
        {
            current++;
        }
        else
        {
            // Parse length value: <length> in '<unit> <from>-<to>/<length>'
            lengthLength = HttpRuleParser.GetNumberLength(input, current, false);

            if ((lengthLength == 0) || (lengthLength > HttpRuleParser.MaxInt64Digits))
            {
                return false;
            }

            current = current + lengthLength;
        }

        current = current + HttpRuleParser.GetWhitespaceLength(input, current);
        return true;
    }

    private static bool TryGetRangeLength(StringSegment input, ref int current, out int fromLength, out int toStartIndex, out int toLength)
    {
        fromLength = 0;
        toStartIndex = 0;
        toLength = 0;

        // Check if we have a value like 'bytes */133'. If yes, skip the range part and continue parsing the
        // length separator '/'.
        if (input[current] == '*')
        {
            current++;
        }
        else
        {
            // Parse first range value: <from> in '<unit> <from>-<to>/<length>'
            fromLength = HttpRuleParser.GetNumberLength(input, current, false);

            if ((fromLength == 0) || (fromLength > HttpRuleParser.MaxInt64Digits))
            {
                return false;
            }

            current = current + fromLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // After the first value, the '-' character must follow.
            if ((current == input.Length) || (input[current] != '-'))
            {
                // We need a '-' character otherwise this can't be a valid range.
                return false;
            }

            current++; // skip the '-' character
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if (current == input.Length)
            {
                return false;
            }

            // Parse second range value: <to> in '<unit> <from>-<to>/<length>'
            toStartIndex = current;
            toLength = HttpRuleParser.GetNumberLength(input, current, false);

            if ((toLength == 0) || (toLength > HttpRuleParser.MaxInt64Digits))
            {
                return false;
            }

            current = current + toLength;
        }

        current = current + HttpRuleParser.GetWhitespaceLength(input, current);
        return true;
    }

    private static bool TryCreateContentRange(
        StringSegment input,
        StringSegment unit,
        int fromStartIndex,
        int fromLength,
        int toStartIndex,
        int toLength,
        int lengthStartIndex,
        int lengthLength,
        [NotNullWhen(true)] out ContentRangeHeaderValue? parsedValue)
    {
        parsedValue = null;

        long from = 0;
        if ((fromLength > 0) && !HeaderUtilities.TryParseNonNegativeInt64(input.Subsegment(fromStartIndex, fromLength), out from))
        {
            return false;
        }

        long to = 0;
        if ((toLength > 0) && !HeaderUtilities.TryParseNonNegativeInt64(input.Subsegment(toStartIndex, toLength), out to))
        {
            return false;
        }

        // 'from' must not be greater than 'to'
        if ((fromLength > 0) && (toLength > 0) && (from > to))
        {
            return false;
        }

        long length = 0;
        if ((lengthLength > 0) && !HeaderUtilities.TryParseNonNegativeInt64(input.Subsegment(lengthStartIndex, lengthLength),
            out length))
        {
            return false;
        }

        // 'from' and 'to' must be less than 'length'
        if ((toLength > 0) && (lengthLength > 0) && (to >= length))
        {
            return false;
        }

        var result = new ContentRangeHeaderValue();
        result._unit = unit;

        if (fromLength > 0)
        {
            result.From = from;
            result.To = to;
        }

        if (lengthLength > 0)
        {
            result.Length = length;
        }

        parsedValue = result;
        return true;
    }
}
