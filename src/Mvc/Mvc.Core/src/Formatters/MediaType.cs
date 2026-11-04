// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A media type value.
/// </summary>
public readonly struct MediaType
{
    private static readonly StringSegment QualityParameter = new StringSegment("q");

    private readonly ReadOnlyMediaTypeHeaderValue _mediaTypeHeaderValue;

    /// <summary>
    /// Initializes a <see cref="MediaType"/> instance.
    /// </summary>
    /// <param name="mediaType">The <see cref="string"/> with the media type.</param>
    public MediaType(string mediaType)
        : this(mediaType, 0, mediaType.Length)
    {
    }

    /// <summary>
    /// Initializes a <see cref="MediaType"/> instance.
    /// </summary>
    /// <param name="mediaType">The <see cref="StringSegment"/> with the media type.</param>
    public MediaType(StringSegment mediaType)
        : this(mediaType.Buffer ?? string.Empty, mediaType.Offset, mediaType.Length)
    {
    }

    /// <summary>
    /// Initializes a <see cref="MediaTypeParameterParser"/> instance.
    /// </summary>
    /// <param name="mediaType">The <see cref="string"/> with the media type.</param>
    /// <param name="offset">The offset in the <paramref name="mediaType"/> where the parsing starts.</param>
    /// <param name="length">The length of the media type to parse if provided.</param>
    public MediaType(string mediaType, int offset, int? length)
    {
        ArgumentNullException.ThrowIfNull(mediaType);

        if (offset < 0 || offset >= mediaType.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (length != null)
        {
            if (length < 0 || length > mediaType.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (offset > mediaType.Length - length)
            {
                throw new ArgumentException(Resources.FormatArgument_InvalidOffsetLength(nameof(offset), nameof(length)));
            }
        }

        _mediaTypeHeaderValue = new ReadOnlyMediaTypeHeaderValue(mediaType, offset, length);
    }

    /// <summary>
    /// Gets the type of the <see cref="MediaType"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/json"</c>, this property gives the value <c>"application"</c>.
    /// </example>
    public StringSegment Type => _mediaTypeHeaderValue.Type;

    /// <summary>
    /// Gets whether this <see cref="MediaType"/> matches all types.
    /// </summary>
    public bool MatchesAllTypes => _mediaTypeHeaderValue.MatchesAllTypes;

    /// <summary>
    /// Gets the subtype of the <see cref="MediaType"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property gives the value
    /// <c>"vnd.example+json"</c>.
    /// </example>
    public StringSegment SubType => _mediaTypeHeaderValue.SubType;

    /// <summary>
    /// Gets the subtype of the <see cref="MediaType"/>, excluding any structured syntax suffix.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property gives the value
    /// <c>"vnd.example"</c>.
    /// </example>
    public StringSegment SubTypeWithoutSuffix => _mediaTypeHeaderValue.SubTypeWithoutSuffix;

    /// <summary>
    /// Gets the structured syntax suffix of the <see cref="MediaType"/> if it has one.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property gives the value
    /// <c>"json"</c>.
    /// </example>
    public StringSegment SubTypeSuffix => _mediaTypeHeaderValue.SubTypeSuffix;

    /// <summary>
    /// Gets whether this <see cref="MediaType"/> matches all subtypes.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/*"</c>, this property is <c>true</c>.
    /// </example>
    /// <example>
    /// For the media type <c>"application/json"</c>, this property is <c>false</c>.
    /// </example>
    public bool MatchesAllSubTypes => _mediaTypeHeaderValue.MatchesAllSubTypes;

    /// <summary>
    /// Gets whether this <see cref="MediaType"/> matches all subtypes, ignoring any structured syntax suffix.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/*+json"</c>, this property is <c>true</c>.
    /// </example>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property is <c>false</c>.
    /// </example>
    public bool MatchesAllSubTypesWithoutSuffix => _mediaTypeHeaderValue.MatchesAllSubTypesWithoutSuffix;

    /// <summary>
    /// Gets the <see cref="System.Text.Encoding"/> of the <see cref="MediaType"/> if it has one.
    /// </summary>
    public Encoding? Encoding => _mediaTypeHeaderValue.Encoding;

    /// <summary>
    /// Gets the charset parameter of the <see cref="MediaType"/> if it has one.
    /// </summary>
    public StringSegment Charset => _mediaTypeHeaderValue.Charset;

    /// <summary>
    /// Determines whether the current <see cref="MediaType"/> contains a wildcard.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this <see cref="MediaType"/> contains a wildcard; otherwise <c>false</c>.
    /// </returns>
    public bool HasWildcard => _mediaTypeHeaderValue.HasWildcard;

    /// <summary>
    /// Determines whether the current <see cref="MediaType"/> is a subset of the <paramref name="set"/>
    /// <see cref="MediaType"/>.
    /// </summary>
    /// <param name="set">The set <see cref="MediaType"/>.</param>
    /// <returns>
    /// <c>true</c> if this <see cref="MediaType"/> is a subset of <paramref name="set"/>; otherwise <c>false</c>.
    /// </returns>
    public bool IsSubsetOf(MediaType set)
        => _mediaTypeHeaderValue.IsSubsetOf(set._mediaTypeHeaderValue);

    /// <summary>
    /// Gets the parameter <paramref name="parameterName"/> of the media type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <returns>
    /// The <see cref="StringSegment"/>for the given <paramref name="parameterName"/> if found; otherwise
    /// <c>null</c>.
    /// </returns>
    public StringSegment GetParameter(string parameterName)
        => _mediaTypeHeaderValue.GetParameter(parameterName);

    /// <summary>
    /// Gets the parameter <paramref name="parameterName"/> of the media type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <returns>
    /// The <see cref="StringSegment"/>for the given <paramref name="parameterName"/> if found; otherwise
    /// <c>null</c>.
    /// </returns>
    public StringSegment GetParameter(StringSegment parameterName)
        => _mediaTypeHeaderValue.GetParameter(parameterName);

    /// <summary>
    /// Replaces the encoding of the given <paramref name="mediaType"/> with the provided
    /// <paramref name="encoding"/>.
    /// </summary>
    /// <param name="mediaType">The media type whose encoding will be replaced.</param>
    /// <param name="encoding">The encoding that will replace the encoding in the <paramref name="mediaType"/>.
    /// </param>
    /// <returns>A media type with the replaced encoding.</returns>
    public static string ReplaceEncoding(string mediaType, Encoding encoding)
    {
        return ReplaceEncoding(new StringSegment(mediaType), encoding);
    }

    /// <summary>
    /// Replaces the encoding of the given <paramref name="mediaType"/> with the provided
    /// <paramref name="encoding"/>.
    /// </summary>
    /// <param name="mediaType">The media type whose encoding will be replaced.</param>
    /// <param name="encoding">The encoding that will replace the encoding in the <paramref name="mediaType"/>.
    /// </param>
    /// <returns>A media type with the replaced encoding.</returns>
    public static string ReplaceEncoding(StringSegment mediaType, Encoding encoding)
    {
        var parsedMediaType = new MediaType(mediaType);
        var charset = parsedMediaType.GetParameter("charset");

        if (charset.HasValue && charset.Equals(encoding.WebName, StringComparison.OrdinalIgnoreCase))
        {
            return mediaType.Value ?? string.Empty;
        }

        if (!charset.HasValue)
        {
            return CreateMediaTypeWithEncoding(mediaType, encoding);
        }

        var charsetOffset = charset.Offset - mediaType.Offset;
        var restOffset = charsetOffset + charset.Length;
        var restLength = mediaType.Length - restOffset;
        var finalLength = charsetOffset + encoding.WebName.Length + restLength;

        var builder = new StringBuilder(mediaType.Buffer, mediaType.Offset, charsetOffset, finalLength);
        builder.Append(encoding.WebName);
        builder.Append(mediaType.Buffer, restOffset, restLength);

        return builder.ToString();
    }

    /// <summary>
    /// Get an encoding for a mediaType.
    /// </summary>
    /// <param name="mediaType">The mediaType.</param>
    /// <returns>The encoding.</returns>
    public static Encoding? GetEncoding(string mediaType)
    {
        return GetEncoding(new StringSegment(mediaType));
    }

    /// <summary>
    /// Get an encoding for a mediaType.
    /// </summary>
    /// <param name="mediaType">The mediaType.</param>
    /// <returns>The encoding.</returns>
    public static Encoding? GetEncoding(StringSegment mediaType)
    {
        var parsedMediaType = new MediaType(mediaType);
        return parsedMediaType.Encoding;
    }

    /// <summary>
    /// Creates an <see cref="MediaTypeSegmentWithQuality"/> containing the media type in <paramref name="mediaType"/>
    /// and its associated quality.
    /// </summary>
    /// <param name="mediaType">The media type to parse.</param>
    /// <param name="start">The position at which the parsing starts.</param>
    /// <returns>The parsed media type with its associated quality.</returns>
    public static MediaTypeSegmentWithQuality CreateMediaTypeSegmentWithQuality(string mediaType, int start)
    {
        var parsedMediaType = new ReadOnlyMediaTypeHeaderValue(mediaType, start, length: null);

        // Short-circuit use of the MediaTypeParameterParser if constructor detected an invalid type or subtype.
        // Parser would set ParsingFailed==true in this case. But, we handle invalid parameters as a separate case.
        if (parsedMediaType.Type.Equals(default(StringSegment)) ||
            parsedMediaType.SubType.Equals(default(StringSegment)))
        {
            return default(MediaTypeSegmentWithQuality);
        }

        var quality = 1.0d;

        var parser = parsedMediaType.ParameterParser;
        while (parser.ParseNextParameter(out var parameter))
        {
            if (parameter.HasName(QualityParameter))
            {
                // If media type contains two `q` values i.e. it's invalid in an uncommon way, pick last value.
                quality = double.Parse(
                    parameter.Value.AsSpan(), NumberStyles.AllowDecimalPoint,
                    NumberFormatInfo.InvariantInfo);
            }
        }

        // We check if the parsed media type has a value at this stage when we have iterated
        // over all the parameters and we know if the parsing was successful.
        if (parser.ParsingFailed)
        {
            return default(MediaTypeSegmentWithQuality);
        }

        return new MediaTypeSegmentWithQuality(
            new StringSegment(mediaType, start, parser.CurrentOffset - start),
            quality);
    }

    private static string CreateMediaTypeWithEncoding(StringSegment mediaType, Encoding encoding)
    {
        return $"{mediaType.Value}; charset={encoding.WebName}";
    }

    private struct MediaTypeParameterParser
    {
        private readonly string _mediaTypeBuffer;
        private readonly int? _length;

        public MediaTypeParameterParser(string mediaTypeBuffer, int offset, int? length)
        {
            _mediaTypeBuffer = mediaTypeBuffer;
            _length = length;
            CurrentOffset = offset;
            ParsingFailed = false;
        }

        public int CurrentOffset { get; private set; }

        public bool ParsingFailed { get; private set; }

        public bool ParseNextParameter(out MediaTypeParameter result)
        {
            if (_mediaTypeBuffer == null)
            {
                ParsingFailed = true;
                result = default(MediaTypeParameter);
                return false;
            }

            var parameterLength = GetParameterLength(_mediaTypeBuffer, CurrentOffset, out result);
            CurrentOffset += parameterLength;

            if (parameterLength == 0)
            {
                ParsingFailed = _length != null && CurrentOffset < _length;
                return false;
            }

            return true;
        }

        private static int GetParameterLength(string input, int startIndex, out MediaTypeParameter parsedValue)
        {
            if (OffsetIsOutOfRange(startIndex, input.Length) || input[startIndex] != ';')
            {
                parsedValue = default(MediaTypeParameter);
                return 0;
            }

            var nameLength = GetNameLength(input, startIndex, out var name);

            var current = startIndex + nameLength;

            if (nameLength == 0 || OffsetIsOutOfRange(current, input.Length) || input[current] != '=')
            {
                if (current == input.Length && name.Equals("*", StringComparison.OrdinalIgnoreCase))
                {
                    // As a special case, we allow a trailing ";*" to indicate a wildcard
                    // string allowing any other parameters. It's the same as ";*=*".
                    var asterisk = new StringSegment("*");
                    parsedValue = new MediaTypeParameter(asterisk, asterisk);
                    return current - startIndex;
                }
                else
                {
                    parsedValue = default(MediaTypeParameter);
                    return 0;
                }
            }

            var valueLength = GetValueLength(input, current, out var value);

            parsedValue = new MediaTypeParameter(name, value);
            current += valueLength;

            return current - startIndex;
        }

        private static int GetNameLength(string input, int startIndex, out StringSegment name)
        {
            var current = startIndex;

            current++; // skip ';'
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            var nameLength = HttpRuleParser.GetTokenLength(input, current);
            if (nameLength == 0)
            {
                name = default(StringSegment);
                return 0;
            }

            name = new StringSegment(input, current, nameLength);

            current += nameLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            return current - startIndex;
        }

        private static int GetValueLength(string input, int startIndex, out StringSegment value)
        {
            var current = startIndex;

            current++; // skip '='.
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            var valueLength = HttpRuleParser.GetTokenLength(input, current);

            if (valueLength == 0)
            {
                // A value can either be a token or a quoted string. Check if it is a quoted string.
                var result = HttpRuleParser.GetQuotedStringLength(input, current, out valueLength);
                if (result != HttpParseResult.Parsed)
                {
                    // We have an invalid value. Reset the name and return.
                    value = default(StringSegment);
                    return 0;
                }

                // Quotation marks are not part of a quoted parameter value.
                value = new StringSegment(input, current + 1, valueLength - 2);
            }
            else
            {
                value = new StringSegment(input, current, valueLength);
            }

            current += valueLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            return current - startIndex;
        }

        private static bool OffsetIsOutOfRange(int offset, int length)
        {
            return offset < 0 || offset >= length;
        }
    }

    private readonly struct MediaTypeParameter : IEquatable<MediaTypeParameter>
    {
        public MediaTypeParameter(StringSegment name, StringSegment value)
        {
            Name = name;
            Value = value;
        }

        public StringSegment Name { get; }

        public StringSegment Value { get; }

        public bool HasName(string name)
        {
            return HasName(new StringSegment(name));
        }

        public bool HasName(StringSegment name)
        {
            return Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(MediaTypeParameter other)
        {
            return HasName(other.Name) && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => $"{Name}={Value}";
    }
}
