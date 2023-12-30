// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Represents an entity-tag (<c>etag</c>) header value.
/// </summary>
public class EntityTagHeaderValue
{
    // Note that the ETag header does not allow a * but we're not that strict: We allow both '*' and ETag values in a single value.
    // We can't guarantee that a single parsed value will be used directly in an ETag header.
    private static readonly HttpHeaderParser<EntityTagHeaderValue> SingleValueParser
        = new GenericHeaderParser<EntityTagHeaderValue>(false, GetEntityTagLength);
    // Note that if multiple ETag values are allowed (e.g. 'If-Match', 'If-None-Match'), according to the RFC
    // the value must either be '*' or a list of ETag values. It's not allowed to have both '*' and a list of
    // ETag values. We're not that strict: We allow both '*' and ETag values in a list. If the server sends such
    // an invalid list, we want to be able to represent it using the corresponding header property.
    private static readonly HttpHeaderParser<EntityTagHeaderValue> MultipleValueParser
        = new GenericHeaderParser<EntityTagHeaderValue>(true, GetEntityTagLength);

    private StringSegment _tag;
    private bool _isWeak;

    private EntityTagHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityTagHeaderValue"/>.
    /// </summary>
    /// <param name="tag">A <see cref="StringSegment"/> that contains an <see cref="EntityTagHeaderValue"/>.</param>
    public EntityTagHeaderValue(StringSegment tag)
        : this(tag, isWeak: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityTagHeaderValue"/>.
    /// </summary>
    /// <param name="tag">A <see cref="StringSegment"/> that contains an <see cref="EntityTagHeaderValue"/>.</param>
    /// <param name="isWeak">A value that indicates if this entity-tag header is a weak validator.</param>
    public EntityTagHeaderValue(StringSegment tag, bool isWeak)
    {
        if (StringSegment.IsNullOrEmpty(tag))
        {
            throw new ArgumentException("An empty string is not allowed.", nameof(tag));
        }

        if (!isWeak && StringSegment.Equals(tag, "*", StringComparison.Ordinal))
        {
            // * is valid, but W/* isn't.
            _tag = tag;
        }
        else if ((HttpRuleParser.GetQuotedStringLength(tag, 0, out var length) != HttpParseResult.Parsed) ||
            (length != tag.Length))
        {
            // Note that we don't allow 'W/' prefixes for weak ETags in the 'tag' parameter. If the user wants to
            // add a weak ETag, they can set 'isWeak' to true.
            throw new FormatException("Invalid ETag name");
        }

        _tag = tag;
        _isWeak = isWeak;
    }

    /// <summary>
    /// Gets the "any" etag.
    /// </summary>
    public static EntityTagHeaderValue Any { get; } = new EntityTagHeaderValue("*", isWeak: false);

    /// <summary>
    /// Gets the quoted tag.
    /// </summary>
    public StringSegment Tag => _tag;

    /// <summary>
    /// Gets a value that determines if the entity-tag header is a weak validator.
    /// </summary>
    public bool IsWeak => _isWeak;

    /// <inheritdoc />
    public override string ToString()
    {
        if (_isWeak)
        {
            return "W/" + _tag.ToString();
        }
        return _tag.ToString();
    }

    /// <summary>
    /// Check against another <see cref="EntityTagHeaderValue"/> for equality.
    /// This equality check should not be used to determine if two values match under the RFC specifications (<see href="https://tools.ietf.org/html/rfc7232#section-2.3.2"/>).
    /// </summary>
    /// <param name="obj">The other value to check against for equality.</param>
    /// <returns>
    /// <c>true</c> if the strength and tag of the two values match,
    /// <c>false</c> if the other value is null, is not an <see cref="EntityTagHeaderValue"/>, or if there is a mismatch of strength or tag between the two values.
    /// </returns>
    public override bool Equals(object? obj)
    {
        // Since the tag is a quoted-string we treat it case-sensitive.
        return obj is EntityTagHeaderValue other && _isWeak == other._isWeak && StringSegment.Equals(_tag, other._tag, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Since the tag is a quoted-string we treat it case-sensitive.
        return _tag.GetHashCode() ^ _isWeak.GetHashCode();
    }

    /// <summary>
    /// Compares against another <see cref="EntityTagHeaderValue"/> to see if they match under the RFC specifications (<see href="https://tools.ietf.org/html/rfc7232#section-2.3.2"/>).
    /// </summary>
    /// <param name="other">The other <see cref="EntityTagHeaderValue"/> to compare against.</param>
    /// <param name="useStrongComparison"><c>true</c> to use a strong comparison, <c>false</c> to use a weak comparison</param>
    /// <returns>
    /// <c>true</c> if the <see cref="EntityTagHeaderValue"/> match for the given comparison type,
    /// <c>false</c> if the other value is null or the comparison failed.
    /// </returns>
    public bool Compare(EntityTagHeaderValue? other, bool useStrongComparison)
    {
        if (other == null)
        {
            return false;
        }

        if (useStrongComparison)
        {
            return !IsWeak && !other.IsWeak && StringSegment.Equals(Tag, other.Tag, StringComparison.Ordinal);
        }
        else
        {
            return StringSegment.Equals(Tag, other.Tag, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="EntityTagHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static EntityTagHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return SingleValueParser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="EntityTagHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="EntityTagHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out EntityTagHeaderValue? parsedValue)
    {
        var index = 0;
        return SingleValueParser.TryParseValue(input, ref index, out parsedValue);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="EntityTagHeaderValue"/> values.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<EntityTagHeaderValue> ParseList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseValues(inputs);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="EntityTagHeaderValue"/> values using string parsing rules.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<EntityTagHeaderValue> ParseStrictList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseStrictValues(inputs);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="EntityTagHeaderValue"/>.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="EntityTagHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseList(IList<string>? inputs, [NotNullWhen(true)] out IList<EntityTagHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseValues(inputs, out parsedValues);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="EntityTagHeaderValue"/> using string parsing rules.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="EntityTagHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseStrictList(IList<string>? inputs, [NotNullWhen(true)] out IList<EntityTagHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseStrictValues(inputs, out parsedValues);
    }

    internal static int GetEntityTagLength(StringSegment input, int startIndex, out EntityTagHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
        {
            return 0;
        }

        // Caller must remove leading whitespaces. If not, we'll return 0.
        var isWeak = false;
        var current = startIndex;

        var firstChar = input[startIndex];
        if (firstChar == '*')
        {
            // We have '*' value, indicating "any" ETag.
            parsedValue = Any;
            current++;
        }
        else
        {
            // The RFC defines 'W/' as prefix, but we'll be flexible and also accept lower-case 'w'.
            if ((firstChar == 'W') || (firstChar == 'w'))
            {
                current++;
                // We need at least 3 more chars: the '/' character followed by two quotes.
                if ((current + 2 >= input.Length) || (input[current] != '/'))
                {
                    return 0;
                }
                isWeak = true;
                current++; // we have a weak-entity tag.
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }

            var tagStartIndex = current;
            if (HttpRuleParser.GetQuotedStringLength(input, current, out var tagLength) != HttpParseResult.Parsed)
            {
                return 0;
            }

            parsedValue = new EntityTagHeaderValue();
            if (tagLength == input.Length)
            {
                // Most of the time we'll have strong ETags without leading/trailing whitespaces.
                Contract.Assert(startIndex == 0);
                Contract.Assert(!isWeak);
                parsedValue._tag = input;
                parsedValue._isWeak = false;
            }
            else
            {
                parsedValue._tag = input.Subsegment(tagStartIndex, tagLength);
                parsedValue._isWeak = isWeak;
            }

            current = current + tagLength;
        }
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        return current - startIndex;
    }
}
