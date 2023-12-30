// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Represents an <c>If-Range</c> header value which can either be a date/time or an entity-tag value.
/// </summary>
public class RangeConditionHeaderValue
{
    private static readonly HttpHeaderParser<RangeConditionHeaderValue> Parser
        = new GenericHeaderParser<RangeConditionHeaderValue>(false, GetRangeConditionLength);

    private DateTimeOffset? _lastModified;
    private EntityTagHeaderValue? _entityTag;

    private RangeConditionHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RangeConditionHeaderValue"/>.
    /// </summary>
    /// <param name="lastModified">A date value used to initialize the new instance.</param>
    public RangeConditionHeaderValue(DateTimeOffset lastModified)
    {
        _lastModified = lastModified;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RangeConditionHeaderValue"/>.
    /// </summary>
    /// <param name="entityTag">An entity tag uniquely representing the requested resource.</param>
    public RangeConditionHeaderValue(EntityTagHeaderValue entityTag)
    {
        ArgumentNullException.ThrowIfNull(entityTag);

        _entityTag = entityTag;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RangeConditionHeaderValue"/>.
    /// </summary>
    /// <param name="entityTag">An entity tag uniquely representing the requested resource.</param>
    public RangeConditionHeaderValue(string? entityTag)
        : this(new EntityTagHeaderValue(entityTag))
    {
    }

    /// <summary>
    /// Gets the LastModified date from header.
    /// </summary>
    public DateTimeOffset? LastModified
    {
        get { return _lastModified; }
    }

    /// <summary>
    /// Gets the <see cref="EntityTagHeaderValue"/> from header.
    /// </summary>
    public EntityTagHeaderValue? EntityTag
    {
        get { return _entityTag; }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (_entityTag == null)
        {
            return HeaderUtilities.FormatDate(_lastModified.GetValueOrDefault());
        }
        return _entityTag.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as RangeConditionHeaderValue;

        if (other == null)
        {
            return false;
        }

        if (_entityTag == null)
        {
            return (other._lastModified != null) && (_lastModified.GetValueOrDefault() == other._lastModified.GetValueOrDefault());
        }

        return _entityTag.Equals(other._entityTag);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_entityTag == null)
        {
            return _lastModified.GetValueOrDefault().GetHashCode();
        }

        return _entityTag.GetHashCode();
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="RangeConditionHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static RangeConditionHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return Parser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="RangeConditionHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="RangeConditionHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out RangeConditionHeaderValue? parsedValue)
    {
        var index = 0;
        return Parser.TryParseValue(input, ref index, out parsedValue!);
    }

    private static int GetRangeConditionLength(StringSegment input, int startIndex, out RangeConditionHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        // Make sure we have at least 2 characters
        if (StringSegment.IsNullOrEmpty(input) || (startIndex + 1 >= input.Length))
        {
            return 0;
        }

        var current = startIndex;

        // Caller must remove leading whitespaces.
        DateTimeOffset date = DateTimeOffset.MinValue;
        EntityTagHeaderValue? entityTag = null;

        // Entity tags are quoted strings optionally preceded by "W/". By looking at the first two character we
        // can determine whether the string is en entity tag or a date.
        var firstChar = input[current];
        var secondChar = input[current + 1];

        if ((firstChar == '\"') || (((firstChar == 'w') || (firstChar == 'W')) && (secondChar == '/')))
        {
            // trailing whitespaces are removed by GetEntityTagLength()
            var entityTagLength = EntityTagHeaderValue.GetEntityTagLength(input, current, out entityTag);

            if (entityTagLength == 0)
            {
                return 0;
            }

            current = current + entityTagLength;

            // RangeConditionHeaderValue only allows 1 value. There must be no delimiter/other chars after an
            // entity tag.
            if (current != input.Length)
            {
                return 0;
            }
        }
        else
        {
            if (!HttpRuleParser.TryStringToDate(input.Subsegment(current), out date))
            {
                return 0;
            }

            // If we got a valid date, then the parser consumed the whole string (incl. trailing whitespaces).
            current = input.Length;
        }

        parsedValue = new RangeConditionHeaderValue();
        if (entityTag == null)
        {
            parsedValue._lastModified = date;
        }
        else
        {
            parsedValue._entityTag = entityTag;
        }

        return current - startIndex;
    }
}
