// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Headers;

/// <summary>
/// A media type value.
/// </summary>
internal readonly struct ReadOnlyMediaTypeHeaderValue
{
    /// <summary>
    /// Initializes a <see cref="ReadOnlyMediaTypeHeaderValue"/> instance.
    /// </summary>
    /// <param name="mediaType">The <see cref="string"/> with the media type.</param>
    public ReadOnlyMediaTypeHeaderValue(string mediaType)
        : this(mediaType, 0, mediaType.Length)
    {
    }

    /// <summary>
    /// Initializes a <see cref="ReadOnlyMediaTypeHeaderValue"/> instance.
    /// </summary>
    /// <param name="mediaType">The <see cref="StringSegment"/> with the media type.</param>
    public ReadOnlyMediaTypeHeaderValue(StringSegment mediaType)
        : this(mediaType.Buffer ?? string.Empty, mediaType.Offset, mediaType.Length)
    {
    }

    /// <summary>
    /// Initializes a <see cref="MediaTypeParameterParser"/> instance.
    /// </summary>
    /// <param name="mediaType">The <see cref="string"/> with the media type.</param>
    /// <param name="offset">The offset in the <paramref name="mediaType"/> where the parsing starts.</param>
    /// <param name="length">The length of the media type to parse if provided.</param>
    public ReadOnlyMediaTypeHeaderValue(string mediaType, int offset, int? length)
    {
        ParameterParser = default(MediaTypeParameterParser);

        var typeLength = GetTypeLength(mediaType, offset, out var type);
        if (typeLength == 0)
        {
            Type = new StringSegment();
            SubType = new StringSegment();
            SubTypeWithoutSuffix = new StringSegment();
            SubTypeSuffix = new StringSegment();
            return;
        }
        else
        {
            Type = type;
        }

        var subTypeLength = GetSubtypeLength(mediaType, offset + typeLength, out var subType);
        if (subTypeLength == 0)
        {
            SubType = new StringSegment();
            SubTypeWithoutSuffix = new StringSegment();
            SubTypeSuffix = new StringSegment();
            return;
        }
        else
        {
            SubType = subType;

            if (TryGetSuffixLength(subType, out var subtypeSuffixLength))
            {
                SubTypeWithoutSuffix = subType.Subsegment(0, subType.Length - subtypeSuffixLength - 1);
                SubTypeSuffix = subType.Subsegment(subType.Length - subtypeSuffixLength, subtypeSuffixLength);
            }
            else
            {
                SubTypeWithoutSuffix = SubType;
                SubTypeSuffix = new StringSegment();
            }
        }

        ParameterParser = new MediaTypeParameterParser(mediaType, offset + typeLength + subTypeLength, length);
    }

    // All GetXXXLength methods work in the same way. They expect to be on the right position for
    // the token they are parsing, for example, the beginning of the media type or the delimiter
    // from a previous token, like '/', ';' or '='.
    // Each method consumes the delimiter token if any, the leading whitespace, then the given token
    // itself, and finally the trailing whitespace.
    private static int GetTypeLength(string input, int offset, out StringSegment type)
    {
        if (offset < 0 || offset >= input.Length)
        {
            type = default(StringSegment);
            return 0;
        }

        var current = offset + HttpRuleParser.GetWhitespaceLength(input, offset);

        // Parse the type, i.e. <type> in media type string "<type>/<subtype>; param1=value1; param2=value2"
        var typeLength = HttpRuleParser.GetTokenLength(input, current);
        if (typeLength == 0)
        {
            type = default(StringSegment);
            return 0;
        }

        type = new StringSegment(input, current, typeLength);

        current += typeLength;
        current += HttpRuleParser.GetWhitespaceLength(input, current);

        return current - offset;
    }

    private static int GetSubtypeLength(string input, int offset, out StringSegment subType)
    {
        var current = offset;

        // Parse the separator between type and subtype
        if (current < 0 || current >= input.Length || input[current] != '/')
        {
            subType = default(StringSegment);
            return 0;
        }

        current++; // skip delimiter.
        current += HttpRuleParser.GetWhitespaceLength(input, current);

        var subtypeLength = HttpRuleParser.GetTokenLength(input, current);
        if (subtypeLength == 0)
        {
            subType = default(StringSegment);
            return 0;
        }

        subType = new StringSegment(input, current, subtypeLength);

        current += subtypeLength;
        current += HttpRuleParser.GetWhitespaceLength(input, current);

        return current - offset;
    }

    private static bool TryGetSuffixLength(StringSegment subType, out int suffixLength)
    {
        // Find the last instance of '+', if there is one
        var startPos = subType.Offset + subType.Length - 1;
        for (var currentPos = startPos; currentPos >= subType.Offset; currentPos--)
        {
            if (subType.Buffer![currentPos] == '+')
            {
                suffixLength = startPos - currentPos;
                return true;
            }
        }

        suffixLength = 0;
        return false;
    }

    /// <summary>
    /// Gets the type of the <see cref="ReadOnlyMediaTypeHeaderValue"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/json"</c>, this property gives the value <c>"application"</c>.
    /// </example>
    public StringSegment Type { get; }

    /// <summary>
    /// Gets whether this <see cref="ReadOnlyMediaTypeHeaderValue"/> matches all types.
    /// </summary>
    public bool MatchesAllTypes => Type.Equals("*", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the subtype of the <see cref="ReadOnlyMediaTypeHeaderValue"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property gives the value
    /// <c>"vnd.example+json"</c>.
    /// </example>
    public StringSegment SubType { get; }

    /// <summary>
    /// Gets the subtype of the <see cref="ReadOnlyMediaTypeHeaderValue"/>, excluding any structured syntax suffix.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property gives the value
    /// <c>"vnd.example"</c>.
    /// </example>
    public StringSegment SubTypeWithoutSuffix { get; }

    /// <summary>
    /// Gets the structured syntax suffix of the <see cref="ReadOnlyMediaTypeHeaderValue"/> if it has one.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property gives the value
    /// <c>"json"</c>.
    /// </example>
    public StringSegment SubTypeSuffix { get; }

    /// <summary>
    /// Gets whether this <see cref="ReadOnlyMediaTypeHeaderValue"/> matches all subtypes.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/*"</c>, this property is <c>true</c>.
    /// </example>
    /// <example>
    /// For the media type <c>"application/json"</c>, this property is <c>false</c>.
    /// </example>
    public bool MatchesAllSubTypes => SubType.Equals("*", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this <see cref="ReadOnlyMediaTypeHeaderValue"/> matches all subtypes, ignoring any structured syntax suffix.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/*+json"</c>, this property is <c>true</c>.
    /// </example>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property is <c>false</c>.
    /// </example>
    public bool MatchesAllSubTypesWithoutSuffix => SubTypeWithoutSuffix.Equals("*", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the <see cref="System.Text.Encoding"/> of the <see cref="ReadOnlyMediaTypeHeaderValue"/> if it has one.
    /// </summary>
    public Encoding? Encoding => GetEncodingFromCharset(GetParameter("charset"));

    /// <summary>
    /// Gets the charset parameter of the <see cref="ReadOnlyMediaTypeHeaderValue"/> if it has one.
    /// </summary>
    public StringSegment Charset => GetParameter("charset");

    /// <summary>
    /// Determines whether the current <see cref="ReadOnlyMediaTypeHeaderValue"/> contains a wildcard.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this <see cref="ReadOnlyMediaTypeHeaderValue"/> contains a wildcard; otherwise <c>false</c>.
    /// </returns>
    public bool HasWildcard
    {
        get
        {
            return MatchesAllTypes ||
                MatchesAllSubTypesWithoutSuffix ||
                GetParameter("*").Equals("*", StringComparison.OrdinalIgnoreCase);
        }
    }

    public MediaTypeParameterParser ParameterParser { get; }

    /// <summary>
    /// Determines whether the current <see cref="ReadOnlyMediaTypeHeaderValue"/> is a subset of the <paramref name="set"/>
    /// <see cref="ReadOnlyMediaTypeHeaderValue"/>.
    /// </summary>
    /// <param name="set">The set <see cref="ReadOnlyMediaTypeHeaderValue"/>.</param>
    /// <returns>
    /// <c>true</c> if this <see cref="ReadOnlyMediaTypeHeaderValue"/> is a subset of <paramref name="set"/>; otherwise <c>false</c>.
    /// </returns>
    public bool IsSubsetOf(ReadOnlyMediaTypeHeaderValue set)
    {
        return MatchesType(set) &&
            MatchesSubtype(set) &&
            ContainsAllParameters(set.ParameterParser);
    }

    /// <summary>
    /// Gets the parameter <paramref name="parameterName"/> of the media type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <returns>
    /// The <see cref="StringSegment"/>for the given <paramref name="parameterName"/> if found; otherwise
    /// <c>null</c>.
    /// </returns>
    public StringSegment GetParameter(string parameterName)
    {
        return GetParameter(new StringSegment(parameterName));
    }

    /// <summary>
    /// Gets the parameter <paramref name="parameterName"/> of the media type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <returns>
    /// The <see cref="StringSegment"/>for the given <paramref name="parameterName"/> if found; otherwise
    /// <c>null</c>.
    /// </returns>
    public StringSegment GetParameter(StringSegment parameterName)
    {
        var parametersParser = ParameterParser;

        while (parametersParser.ParseNextParameter(out var parameter))
        {
            if (parameter.HasName(parameterName))
            {
                return parameter.Value;
            }
        }

        return new StringSegment();
    }

    /// <summary>
    /// Gets the last parameter <paramref name="parameterName"/> of the media type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <param name="parameterValue">The value for the last parameter</param>
    /// <returns>
    /// <see langword="true"/> if parsing succeeded.
    /// </returns>
    public bool TryGetLastParameter(StringSegment parameterName, out StringSegment parameterValue)
    {
        var parametersParser = ParameterParser;

        parameterValue = default;
        while (parametersParser.ParseNextParameter(out var parameter))
        {
            if (parameter.HasName(parameterName))
            {
                parameterValue = parameter.Value;
            }
        }

        return !parametersParser.ParsingFailed;
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
        var parsedMediaType = new ReadOnlyMediaTypeHeaderValue(mediaType);
        return parsedMediaType.Encoding;
    }

    private static Encoding? GetEncodingFromCharset(StringSegment charset)
    {
        if (charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
        {
            // This is an optimization for utf-8 that prevents the Substring caused by
            // charset.Value
            return Encoding.UTF8;
        }

        try
        {
            // charset.Value might be an invalid encoding name as in charset=invalid.
            // For that reason, we catch the exception thrown by Encoding.GetEncoding
            // and return null instead.
            return charset.HasValue ? Encoding.GetEncoding(charset.Value) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private bool MatchesType(ReadOnlyMediaTypeHeaderValue set)
    {
        return set.MatchesAllTypes ||
            set.Type.Equals(Type, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSubtype(ReadOnlyMediaTypeHeaderValue set)
    {
        if (set.MatchesAllSubTypes)
        {
            return true;
        }

        if (set.SubTypeSuffix.HasValue)
        {
            if (SubTypeSuffix.HasValue)
            {
                // Both the set and the media type being checked have suffixes, so both parts must match.
                return MatchesSubtypeWithoutSuffix(set) && MatchesSubtypeSuffix(set);
            }
            else
            {
                // The set has a suffix, but the media type being checked doesn't. We never consider this to match.
                return false;
            }
        }
        else
        {
            // If this subtype or suffix matches the subtype of the set,
            // it is considered a subtype.
            // Ex: application/json > application/val+json
            return MatchesEitherSubtypeOrSuffix(set);
        }
    }

    private bool MatchesSubtypeWithoutSuffix(ReadOnlyMediaTypeHeaderValue set)
    {
        return set.MatchesAllSubTypesWithoutSuffix ||
            set.SubTypeWithoutSuffix.Equals(SubTypeWithoutSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSubtypeSuffix(ReadOnlyMediaTypeHeaderValue set)
    {
        // We don't have support for wildcards on suffixes alone (e.g., "application/entity+*")
        // because there's no clear use case for it.
        return set.SubTypeSuffix.Equals(SubTypeSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesEitherSubtypeOrSuffix(ReadOnlyMediaTypeHeaderValue set)
    {
        return set.SubType.Equals(SubType, StringComparison.OrdinalIgnoreCase) ||
            set.SubType.Equals(SubTypeSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool ContainsAllParameters(MediaTypeParameterParser setParameters)
    {
        var parameterFound = true;
        while (setParameters.ParseNextParameter(out var setParameter) && parameterFound)
        {
            if (setParameter.HasName("q"))
            {
                // "q" and later parameters are not involved in media type matching. Quoting the RFC: The first
                // "q" parameter (if any) separates the media-range parameter(s) from the accept-params.
                break;
            }

            if (setParameter.HasName("*"))
            {
                // A parameter named "*" has no effect on media type matching, as it is only used as an indication
                // that the entire media type string should be treated as a wildcard.
                continue;
            }

            // Copy the parser as we need to iterate multiple times over it.
            // We can do this because it's a struct
            var subSetParameters = ParameterParser;
            parameterFound = false;
            while (subSetParameters.ParseNextParameter(out var subSetParameter) && !parameterFound)
            {
                parameterFound = subSetParameter.Equals(setParameter);
            }
        }

        return parameterFound;
    }

    public struct MediaTypeParameterParser
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

    public readonly struct MediaTypeParameter : IEquatable<MediaTypeParameter>
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
