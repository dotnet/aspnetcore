// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// A string header value with an optional quality.
/// </summary>
public class StringWithQualityHeaderValue
{
    private static readonly HttpHeaderParser<StringWithQualityHeaderValue> SingleValueParser
        = new GenericHeaderParser<StringWithQualityHeaderValue>(false, GetStringWithQualityLength);
    private static readonly HttpHeaderParser<StringWithQualityHeaderValue> MultipleValueParser
        = new GenericHeaderParser<StringWithQualityHeaderValue>(true, GetStringWithQualityLength);

    private StringSegment _value;
    private double? _quality;

    private StringWithQualityHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="StringWithQualityHeaderValue"/>.
    /// </summary>
    /// <param name="value">The <see cref="StringSegment"/> used to initialize the new instance.</param>
    public StringWithQualityHeaderValue(StringSegment value)
    {
        HeaderUtilities.CheckValidToken(value, nameof(value));

        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="StringWithQualityHeaderValue"/>.
    /// </summary>
    /// <param name="value">The <see cref="StringSegment"/> used to initialize the new instance.</param>
    /// <param name="quality">The quality factor.</param>
    public StringWithQualityHeaderValue(StringSegment value, double quality)
    {
        HeaderUtilities.CheckValidToken(value, nameof(value));

        if ((quality < 0) || (quality > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(quality));
        }

        _value = value;
        _quality = quality;
    }

    /// <summary>
    /// Gets the string header value.
    /// </summary>
    public StringSegment Value => _value;

    /// <summary>
    /// Gets the quality factor.
    /// </summary>
    public double? Quality => _quality;

    /// <inheritdoc />
    public override string ToString()
    {
        if (_quality.HasValue)
        {
            return _value + "; q=" + _quality.GetValueOrDefault().ToString("0.0##", NumberFormatInfo.InvariantInfo);
        }

        return _value.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as StringWithQualityHeaderValue;

        if (other == null)
        {
            return false;
        }

        if (!StringSegment.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_quality.HasValue)
        {
            // Note that we don't consider double.Epsilon here. We really consider two values equal if they're
            // actually equal. This makes sure that we also get the same hashcode for two values considered equal
            // by Equals().
            return other._quality.HasValue && (_quality.GetValueOrDefault() == other._quality.Value);
        }

        // If we don't have a quality value, then 'other' must also have no quality assigned in order to be
        // considered equal.
        return !other._quality.HasValue;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var result = StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_value);

        if (_quality.HasValue)
        {
            result = result ^ _quality.GetValueOrDefault().GetHashCode();
        }

        return result;
    }

    /// <summary>
    /// Parses the specified <paramref name="input"/> as a <see cref="StringWithQualityHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <returns>The parsed value.</returns>
    public static StringWithQualityHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return SingleValueParser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="StringWithQualityHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="StringWithQualityHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out StringWithQualityHeaderValue? parsedValue)
    {
        var index = 0;
        return SingleValueParser.TryParseValue(input, ref index, out parsedValue);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="StringWithQualityHeaderValue"/> values.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<StringWithQualityHeaderValue> ParseList(IList<string>? input)
    {
        return MultipleValueParser.ParseValues(input);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="StringWithQualityHeaderValue"/> values using string parsing rules.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<StringWithQualityHeaderValue> ParseStrictList(IList<string>? input)
    {
        return MultipleValueParser.ParseStrictValues(input);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="StringWithQualityHeaderValue"/>.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="StringWithQualityHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseList(IList<string>? input, [NotNullWhen(true)] out IList<StringWithQualityHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseValues(input, out parsedValues);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="StringWithQualityHeaderValue"/> using string parsing rules.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="StringWithQualityHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseStrictList(IList<string>? input, [NotNullWhen(true)] out IList<StringWithQualityHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseStrictValues(input, out parsedValues);
    }

    private static int GetStringWithQualityLength(StringSegment input, int startIndex, out StringWithQualityHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
        {
            return 0;
        }

        // Parse the value string: <value> in '<value>; q=<quality>'
        var valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

        if (valueLength == 0)
        {
            return 0;
        }

        StringWithQualityHeaderValue result = new StringWithQualityHeaderValue();
        result._value = input.Subsegment(startIndex, valueLength);
        var current = startIndex + valueLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        if ((current == input.Length) || (input[current] != ';'))
        {
            parsedValue = result;
            return current - startIndex; // we have a valid token, but no quality.
        }

        current++; // skip ';' separator
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        // If we found a ';' separator, it must be followed by a quality information
        if (!TryReadQuality(input, result, ref current))
        {
            return 0;
        }

        parsedValue = result;
        return current - startIndex;
    }

    private static bool TryReadQuality(StringSegment input, StringWithQualityHeaderValue result, ref int index)
    {
        var current = index;

        // See if we have a quality value by looking for "q"
        if ((current == input.Length) || ((input[current] != 'q') && (input[current] != 'Q')))
        {
            return false;
        }

        current++; // skip 'q' identifier
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        // If we found "q" it must be followed by "="
        if ((current == input.Length) || (input[current] != '='))
        {
            return false;
        }

        current++; // skip '=' separator
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        if (current == input.Length)
        {
            return false;
        }

        if (!HeaderUtilities.TryParseQualityDouble(input, current, out var quality, out var qualityLength))
        {
            return false;
        }

        result._quality = quality;

        current = current + qualityLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        index = current;
        return true;
    }
}
