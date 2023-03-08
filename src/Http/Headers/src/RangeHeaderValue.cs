// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Represents a <c>Range</c> header value.
/// <para>
/// The <see cref="RangeHeaderValue"/> class provides support for the Range header as defined in
/// <see href="https://tools.ietf.org/html/rfc2616">RFC 2616</see>.
/// </para>
/// </summary>
public class RangeHeaderValue
{
    private static readonly HttpHeaderParser<RangeHeaderValue> Parser
        = new GenericHeaderParser<RangeHeaderValue>(false, GetRangeLength);

    private StringSegment _unit;
    private ICollection<RangeItemHeaderValue>? _ranges;

    /// <summary>
    /// Initializes a new instance of <see cref="RangeHeaderValue"/>.
    /// </summary>
    public RangeHeaderValue()
    {
        _unit = HeaderUtilities.BytesUnit;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RangeHeaderValue"/>.
    /// </summary>
    /// <param name="from">The position at which to start sending data.</param>
    /// <param name="to">The position at which to stop sending data.</param>
    public RangeHeaderValue(long? from, long? to)
    {
        // convenience ctor: "Range: bytes=from-to"
        _unit = HeaderUtilities.BytesUnit;
        Ranges.Add(new RangeItemHeaderValue(from, to));
    }

    /// <summary>
    /// Gets or sets the unit from the header.
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
    /// Gets the ranges specified in the header.
    /// </summary>
    public ICollection<RangeItemHeaderValue> Ranges
    {
        get
        {
            if (_ranges == null)
            {
                _ranges = new ObjectCollection<RangeItemHeaderValue>();
            }
            return _ranges;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(_unit.AsSpan());
        sb.Append('=');

        var first = true;
        foreach (var range in Ranges)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.Append(", ");
            }

            sb.Append(range.From);
            sb.Append('-');
            sb.Append(range.To);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as RangeHeaderValue;

        if (other == null)
        {
            return false;
        }

        return StringSegment.Equals(_unit, other._unit, StringComparison.OrdinalIgnoreCase) &&
            HeaderUtilities.AreEqualCollections(Ranges, other.Ranges);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var result = StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_unit);

        foreach (var range in Ranges)
        {
            result = result ^ range.GetHashCode();
        }

        return result;
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="RangeHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static RangeHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return Parser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="RangeHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="RangeHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out RangeHeaderValue? parsedValue)
    {
        var index = 0;
        return Parser.TryParseValue(input, ref index, out parsedValue);
    }

    private static int GetRangeLength(StringSegment input, int startIndex, out RangeHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
        {
            return 0;
        }

        // Parse the unit string: <unit> in '<unit>=<from1>-<to1>, <from2>-<to2>'
        var unitLength = HttpRuleParser.GetTokenLength(input, startIndex);

        if (unitLength == 0)
        {
            return 0;
        }

        RangeHeaderValue result = new RangeHeaderValue();
        result._unit = input.Subsegment(startIndex, unitLength);
        var current = startIndex + unitLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        if ((current == input.Length) || (input[current] != '='))
        {
            return 0;
        }

        current++; // skip '=' separator
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        var rangesLength = RangeItemHeaderValue.GetRangeItemListLength(input, current, result.Ranges);

        if (rangesLength == 0)
        {
            return 0;
        }

        current = current + rangesLength;
        Contract.Assert(current == input.Length, "GetRangeItemListLength() should consume the whole string or fail.");

        parsedValue = result;
        return current - startIndex;
    }
}
