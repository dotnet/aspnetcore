// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Representation of the media type header. See <see href="https://tools.ietf.org/html/rfc6838"/>.
/// </summary>
public class MediaTypeHeaderValue
{
    private const string BoundaryString = "boundary";
    private const string CharsetString = "charset";
    private const string MatchesAllString = "*/*";
    private const string QualityString = "q";
    private const string WildcardString = "*";

    private const char ForwardSlashCharacter = '/';
    private const char PeriodCharacter = '.';
    private const char PlusCharacter = '+';

    private static readonly char[] PeriodCharacterArray = [PeriodCharacter];

    private static readonly HttpHeaderParser<MediaTypeHeaderValue> SingleValueParser
        = new GenericHeaderParser<MediaTypeHeaderValue>(false, GetMediaTypeLength);
    private static readonly HttpHeaderParser<MediaTypeHeaderValue> MultipleValueParser
        = new GenericHeaderParser<MediaTypeHeaderValue>(true, GetMediaTypeLength);

    // Use a collection instead of a dictionary since we may have multiple parameters with the same name.
    private ObjectCollection<NameValueHeaderValue>? _parameters;
    private StringSegment _mediaType;
    private bool _isReadOnly;

    private MediaTypeHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a <see cref="MediaTypeHeaderValue"/> instance.
    /// </summary>
    /// <param name="mediaType">A <see cref="StringSegment"/> representation of a media type.
    /// The text provided must be a single media type without parameters. </param>
    public MediaTypeHeaderValue(StringSegment mediaType)
    {
        CheckMediaTypeFormat(mediaType, nameof(mediaType));
        _mediaType = mediaType;
    }

    /// <summary>
    /// Initializes a <see cref="MediaTypeHeaderValue"/> instance.
    /// </summary>
    /// <param name="mediaType">A <see cref="StringSegment"/> representation of a media type.
    /// The text provided must be a single media type without parameters. </param>
    /// <param name="quality">The <see cref="double"/> with the quality of the media type.</param>
    public MediaTypeHeaderValue(StringSegment mediaType, double quality)
        : this(mediaType)
    {
        Quality = quality;
    }

    /// <summary>
    /// Gets or sets the value of the charset parameter. Returns <see cref="StringSegment.Empty"/>
    /// if there is no charset.
    /// </summary>
    public StringSegment Charset
    {
        get
        {
            return NameValueHeaderValue.Find(_parameters, CharsetString)?.Value ?? default;
        }
        set
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            // We don't prevent a user from setting whitespace-only charsets. Like we can't prevent a user from
            // setting a non-existing charset.
            var charsetParameter = NameValueHeaderValue.Find(_parameters, CharsetString);
            if (StringSegment.IsNullOrEmpty(value))
            {
                // Remove charset parameter
                if (charsetParameter != null)
                {
                    Parameters.Remove(charsetParameter);
                }
            }
            else
            {
                if (charsetParameter != null)
                {
                    charsetParameter.Value = value;
                }
                else
                {
                    Parameters.Add(new NameValueHeaderValue(CharsetString, value));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the value of the Encoding parameter. Setting the Encoding will set
    /// the <see cref="Charset"/> to <see cref="Encoding.WebName"/>.
    /// </summary>
    public Encoding? Encoding
    {
        get
        {
            var charset = Charset;

            // Check HasValue; IsNullOrEmpty lacks [MemberNotNullWhen(false, nameof(Value))].
            if (charset.HasValue && !StringSegment.IsNullOrEmpty(charset))
            {
                try
                {
                    return Encoding.GetEncoding(charset.Value);
                }
                catch (ArgumentException)
                {
                    // Invalid or not supported
                }
            }
            return null;
        }
        set
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            if (value == null)
            {
                Charset = null;
            }
            else
            {
                Charset = value.WebName;
            }
        }
    }

    /// <summary>
    /// Gets or sets the value of the boundary parameter. Returns <see cref="StringSegment.Empty"/>
    /// if there is no boundary.
    /// </summary>
    public StringSegment Boundary
    {
        get
        {
            return NameValueHeaderValue.Find(_parameters, BoundaryString)?.Value ?? default(StringSegment);
        }
        set
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            var boundaryParameter = NameValueHeaderValue.Find(_parameters, BoundaryString);
            if (StringSegment.IsNullOrEmpty(value))
            {
                // Remove charset parameter
                if (boundaryParameter != null)
                {
                    Parameters.Remove(boundaryParameter);
                }
            }
            else
            {
                if (boundaryParameter != null)
                {
                    boundaryParameter.Value = value;
                }
                else
                {
                    Parameters.Add(new NameValueHeaderValue(BoundaryString, value));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the media type's parameters. Returns an empty <see cref="IList{T}"/>
    /// if there are no parameters.
    /// </summary>
    public IList<NameValueHeaderValue> Parameters
    {
        get
        {
            if (_parameters == null)
            {
                if (IsReadOnly)
                {
                    _parameters = ObjectCollection<NameValueHeaderValue>.EmptyReadOnlyCollection;
                }
                else
                {
                    _parameters = new ObjectCollection<NameValueHeaderValue>();
                }
            }
            return _parameters;
        }
    }

    /// <summary>
    /// Gets or sets the value of the quality parameter. Returns null
    /// if there is no quality.
    /// </summary>
    public double? Quality
    {
        get => HeaderUtilities.GetQuality(_parameters);
        set
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            HeaderUtilities.SetQuality(Parameters, value);
        }
    }

    /// <summary>
    /// Gets or sets the value of the media type. Returns <see cref="StringSegment.Empty"/>
    /// if there is no media type.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/json"</c>, the property gives the value
    /// <c>"application/json"</c>.
    /// </example>
    public StringSegment MediaType
    {
        get { return _mediaType; }
        set
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            CheckMediaTypeFormat(value, nameof(value));
            _mediaType = value;
        }
    }

    /// <summary>
    /// Gets the type of the <see cref="MediaTypeHeaderValue"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/json"</c>, the property gives the value <c>"application"</c>.
    /// </example>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6838#section-4.2"/> for more details on the type.</remarks>
    public StringSegment Type
    {
        get
        {
            return _mediaType.Subsegment(0, _mediaType.IndexOf(ForwardSlashCharacter));
        }
    }

    /// <summary>
    /// Gets the subtype of the <see cref="MediaTypeHeaderValue"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, the property gives the value
    /// <c>"vnd.example+json"</c>.
    /// </example>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6838#section-4.2"/> for more details on the subtype.</remarks>
    public StringSegment SubType
    {
        get
        {
            return _mediaType.Subsegment(_mediaType.IndexOf(ForwardSlashCharacter) + 1);
        }
    }

    /// <summary>
    /// Gets subtype of the <see cref="MediaTypeHeaderValue"/>, excluding any structured syntax suffix. Returns <see cref="StringSegment.Empty"/>
    /// if there is no subtype without suffix.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, the property gives the value
    /// <c>"vnd.example"</c>.
    /// </example>
    public StringSegment SubTypeWithoutSuffix
    {
        get
        {
            var subType = SubType;
            var startOfSuffix = subType.LastIndexOf(PlusCharacter);
            if (startOfSuffix == -1)
            {
                return subType;
            }
            else
            {
                return subType.Subsegment(0, startOfSuffix);
            }
        }
    }

    /// <summary>
    /// Gets the structured syntax suffix of the <see cref="MediaTypeHeaderValue"/> if it has one.
    /// See <see href="https://tools.ietf.org/html/rfc6838#section-4.8">The RFC documentation on structured syntaxes.</see>
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, the property gives the value
    /// <c>"json"</c>.
    /// </example>
    public StringSegment Suffix
    {
        get
        {
            var subType = SubType;
            var startOfSuffix = subType.LastIndexOf(PlusCharacter);
            if (startOfSuffix == -1)
            {
                return default(StringSegment);
            }
            else
            {
                return subType.Subsegment(startOfSuffix + 1);
            }
        }
    }

    /// <summary>
    /// Get a <see cref="IList{T}"/> of facets of the <see cref="MediaTypeHeaderValue"/>. Facets are a
    /// period separated list of StringSegments in the <see cref="SubTypeWithoutSuffix"/>.
    /// See <see href="https://tools.ietf.org/html/rfc6838#section-3">The RFC documentation on facets.</see>
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, the property gives the value:
    /// <c>{"vnd", "example"}</c>
    /// </example>
    public IEnumerable<StringSegment> Facets
    {
        get
        {
            return SubTypeWithoutSuffix.Split(PeriodCharacterArray);
        }
    }

    /// <summary>
    /// Gets whether this <see cref="MediaTypeHeaderValue"/> matches all types.
    /// </summary>
    public bool MatchesAllTypes => MediaType.Equals(MatchesAllString, StringComparison.Ordinal);

    /// <summary>
    /// Gets whether this <see cref="MediaTypeHeaderValue"/> matches all subtypes.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/*"</c>, this property is <c>true</c>.
    /// </example>
    /// <example>
    /// For the media type <c>"application/json"</c>, this property is <c>false</c>.
    /// </example>
    public bool MatchesAllSubTypes => SubType.Equals(WildcardString, StringComparison.Ordinal);

    /// <summary>
    /// Gets whether this <see cref="MediaTypeHeaderValue"/> matches all subtypes, ignoring any structured syntax suffix.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/*+json"</c>, this property is <c>true</c>.
    /// </example>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, this property is <c>false</c>.
    /// </example>
    public bool MatchesAllSubTypesWithoutSuffix =>
        SubTypeWithoutSuffix.Equals(WildcardString, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether the <see cref="MediaTypeHeaderValue"/> is readonly.
    /// </summary>
    public bool IsReadOnly
    {
        get { return _isReadOnly; }
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="MediaTypeHeaderValue"/> is a subset of
    /// <paramref name="otherMediaType"/>. A "subset" is defined as the same or a more specific media type
    /// according to the precedence described in <see href="https://www.ietf.org/rfc/rfc2068.txt"/> section 14.1, Accept.
    /// </summary>
    /// <param name="otherMediaType">The <see cref="MediaTypeHeaderValue"/> to compare.</param>
    /// <returns>
    /// A value indicating whether this <see cref="MediaTypeHeaderValue"/> is a subset of
    /// <paramref name="otherMediaType"/>.
    /// </returns>
    /// <remarks>
    /// For example "multipart/mixed; boundary=1234" is a subset of "multipart/mixed; boundary=1234",
    /// "multipart/mixed", "multipart/*", and "*/*" but not "multipart/mixed; boundary=2345" or
    /// "multipart/message; boundary=1234".
    /// </remarks>
    public bool IsSubsetOf(MediaTypeHeaderValue otherMediaType)
    {
        if (otherMediaType == null)
        {
            return false;
        }

        // "text/plain" is a subset of "text/plain", "text/*" and "*/*". "*/*" is a subset only of "*/*".
        return MatchesType(otherMediaType) &&
            MatchesSubtype(otherMediaType) &&
            MatchesParameters(otherMediaType);
    }

    /// <summary>
    /// Performs a deep copy of this object and all of it's NameValueHeaderValue sub components,
    /// while avoiding the cost of re-validating the components.
    /// </summary>
    /// <returns>A deep copy.</returns>
    public MediaTypeHeaderValue Copy()
    {
        var other = new MediaTypeHeaderValue();
        other._mediaType = _mediaType;

        if (_parameters != null)
        {
            other._parameters = new ObjectCollection<NameValueHeaderValue>(
                _parameters.Select(item => item.Copy()));
        }
        return other;
    }

    /// <summary>
    /// Performs a deep copy of this object and all of it's NameValueHeaderValue sub components,
    /// while avoiding the cost of re-validating the components. This copy is read-only.
    /// </summary>
    /// <returns>A deep, read-only, copy.</returns>
    public MediaTypeHeaderValue CopyAsReadOnly()
    {
        if (IsReadOnly)
        {
            return this;
        }

        var other = new MediaTypeHeaderValue();
        other._mediaType = _mediaType;
        if (_parameters != null)
        {
            other._parameters = new ObjectCollection<NameValueHeaderValue>(
                _parameters.Select(item => item.CopyAsReadOnly()), isReadOnly: true);
        }
        other._isReadOnly = true;
        return other;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="otherMediaType"/> is a subset of
    /// this <see cref="MediaTypeHeaderValue"/> in terms of type/subType. A "subset" is defined as the same or a more specific media type
    /// according to the precedence described in <see href="https://www.ietf.org/rfc/rfc2068.txt"/> section 14.1, Accept.
    /// </summary>
    /// <param name="otherMediaType">The <see cref="StringSegment"/> to compare.</param>
    /// <returns>
    /// A value indicating whether <paramref name="otherMediaType"/> is a subset of
    /// this <see cref="MediaTypeHeaderValue"/>.
    /// </returns>
    /// <remarks>
    /// For example "multipart/mixed" is a subset of "multipart/mixed",
    /// "multipart/*", and "*/*" but not "multipart/message."
    /// </remarks>
    public bool MatchesMediaType(StringSegment otherMediaType)
    {
        if (StringSegment.IsNullOrEmpty(otherMediaType))
        {
            return false;
        }
        GetMediaTypeExpressionLength(otherMediaType, 0, out var mediaType);

        return MatchesType(mediaType) && MatchesSubtype(mediaType);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(_mediaType.AsSpan());
        NameValueHeaderValue.ToString(_parameters, separator: ';', leadingSeparator: true, destination: builder);
        return builder.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as MediaTypeHeaderValue;

        if (other == null)
        {
            return false;
        }

        return _mediaType.Equals(other._mediaType, StringComparison.OrdinalIgnoreCase) &&
            HeaderUtilities.AreEqualCollections(_parameters, other._parameters);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // The media-type string is case-insensitive.
        return StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_mediaType) ^ NameValueHeaderValue.GetHashCode(_parameters);
    }

    /// <summary>
    /// Takes a media type and parses it into the <see cref="MediaTypeHeaderValue" /> and its associated parameters.
    /// </summary>
    /// <param name="input">The <see cref="StringSegment"/> with the media type.</param>
    /// <returns>The parsed <see cref="MediaTypeHeaderValue"/>.</returns>
    public static MediaTypeHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return SingleValueParser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Takes a media type, which can include parameters, and parses it into the <see cref="MediaTypeHeaderValue" /> and its associated parameters.
    /// </summary>
    /// <param name="input">The <see cref="StringSegment"/> with the media type. The media type constructed here must not have an y</param>
    /// <param name="parsedValue">The parsed <see cref="MediaTypeHeaderValue"/></param>
    /// <returns>True if the value was successfully parsed.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out MediaTypeHeaderValue? parsedValue)
    {
        var index = 0;
        return SingleValueParser.TryParseValue(input, ref index, out parsedValue!);
    }

    /// <summary>
    /// Takes an <see cref="IList{T}"/> of <see cref="string"/> and parses it into the <see cref="MediaTypeHeaderValue"></see> and its associated parameters.
    /// </summary>
    /// <param name="inputs">A list of media types</param>
    /// <returns>The parsed <see cref="MediaTypeHeaderValue"/>.</returns>
    public static IList<MediaTypeHeaderValue> ParseList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseValues(inputs);
    }

    /// <summary>
    /// Takes an <see cref="IList{T}"/> of <see cref="string"/> and parses it into the <see cref="MediaTypeHeaderValue"></see> and its associated parameters.
    /// Throws if there is invalid data in a string.
    /// </summary>
    /// <param name="inputs">A list of media types</param>
    /// <returns>The parsed <see cref="MediaTypeHeaderValue"/>.</returns>
    public static IList<MediaTypeHeaderValue> ParseStrictList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseStrictValues(inputs);
    }

    /// <summary>
    /// Takes an <see cref="IList{T}"/> of <see cref="string"/> and parses it into the <see cref="MediaTypeHeaderValue"></see> and its associated parameters.
    /// </summary>
    /// <param name="inputs">A list of media types</param>
    /// <param name="parsedValues">The parsed <see cref="MediaTypeHeaderValue"/>.</param>
    /// <returns>True if the value was successfully parsed.</returns>
    public static bool TryParseList(IList<string>? inputs, [NotNullWhen(true)] out IList<MediaTypeHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseValues(inputs, out parsedValues);
    }

    /// <summary>
    /// Takes an <see cref="IList{T}"/> of <see cref="string"/> and parses it into the <see cref="MediaTypeHeaderValue"></see> and its associated parameters.
    /// </summary>
    /// <param name="inputs">A list of media types</param>
    /// <param name="parsedValues">The parsed <see cref="MediaTypeHeaderValue"/>.</param>
    /// <returns>True if the value was successfully parsed.</returns>
    public static bool TryParseStrictList(IList<string>? inputs, [NotNullWhen(true)] out IList<MediaTypeHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseStrictValues(inputs, out parsedValues);
    }

    private static int GetMediaTypeLength(StringSegment input, int startIndex, out MediaTypeHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
        {
            return 0;
        }

        // Caller must remove leading whitespace. If not, we'll return 0.
        var mediaTypeLength = MediaTypeHeaderValue.GetMediaTypeExpressionLength(input, startIndex, out var mediaType);

        if (mediaTypeLength == 0)
        {
            return 0;
        }

        var current = startIndex + mediaTypeLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);
        MediaTypeHeaderValue? mediaTypeHeader;

        // If we're not done and we have a parameter delimiter, then we have a list of parameters.
        if ((current < input.Length) && (input[current] == ';'))
        {
            mediaTypeHeader = new MediaTypeHeaderValue();
            mediaTypeHeader._mediaType = mediaType;

            current++; // skip delimiter.
            var parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                mediaTypeHeader.Parameters);

            parsedValue = mediaTypeHeader;
            return current + parameterLength - startIndex;
        }

        // We have a media type without parameters.
        mediaTypeHeader = new MediaTypeHeaderValue();
        mediaTypeHeader._mediaType = mediaType;
        parsedValue = mediaTypeHeader;
        return current - startIndex;
    }

    private static int GetMediaTypeExpressionLength(StringSegment input, int startIndex, out StringSegment mediaType)
    {
        Contract.Requires((input.Length > 0) && (startIndex < input.Length));

        // This method just parses the "type/subtype" string, it does not parse parameters.
        mediaType = null;

        // Parse the type, i.e. <type> in media type string "<type>/<subtype>; param1=value1; param2=value2"
        var typeLength = HttpRuleParser.GetTokenLength(input, startIndex);

        if (typeLength == 0)
        {
            return 0;
        }

        var current = startIndex + typeLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        // Parse the separator between type and subtype
        if ((current >= input.Length) || (input[current] != '/'))
        {
            return 0;
        }
        current++; // skip delimiter.
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        // Parse the subtype, i.e. <subtype> in media type string "<type>/<subtype>; param1=value1; param2=value2"
        var subtypeLength = HttpRuleParser.GetTokenLength(input, current);

        if (subtypeLength == 0)
        {
            return 0;
        }

        // If there is no whitespace between <type> and <subtype> in <type>/<subtype> get the media type using
        // one Substring call. Otherwise get substrings for <type> and <subtype> and combine them.
        var mediaTypeLength = current + subtypeLength - startIndex;
        if (typeLength + subtypeLength + 1 == mediaTypeLength)
        {
            mediaType = input.Subsegment(startIndex, mediaTypeLength);
        }
        else
        {
            mediaType = string.Concat(input.AsSpan().Slice(startIndex, typeLength), "/", input.AsSpan().Slice(current, subtypeLength));
        }

        return mediaTypeLength;
    }

    private static void CheckMediaTypeFormat(StringSegment mediaType, string parameterName)
    {
        if (StringSegment.IsNullOrEmpty(mediaType))
        {
            throw new ArgumentException("An empty string is not allowed.", parameterName);
        }

        // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) is allowed.
        // Also no LWS between type and subtype is allowed.
        var mediaTypeLength = GetMediaTypeExpressionLength(mediaType, 0, out var tempMediaType);
        if ((mediaTypeLength == 0) || (tempMediaType.Length != mediaType.Length))
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Invalid media type '{0}'.", mediaType));
        }
    }

    private bool MatchesType(MediaTypeHeaderValue set)
    {
        return set.MatchesAllTypes ||
            set.Type.Equals(Type, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesType(StringSegment mediaType)
    {
        var type = mediaType.Subsegment(0, mediaType.IndexOf(ForwardSlashCharacter));

        return MatchesAllTypes ||
            Type.Equals(type, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSubtype(MediaTypeHeaderValue set)
    {
        if (set.MatchesAllSubTypes)
        {
            return true;
        }

        if (set.Suffix.HasValue)
        {
            if (Suffix.HasValue)
            {
                return MatchesSubtypeWithoutSuffix(set) && MatchesSubtypeSuffix(set);
            }
            else
            {
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

    private bool MatchesSubtype(StringSegment mediaType)
    {
        if (MatchesAllSubTypes)
        {
            return true;
        }

        var subType = mediaType.Subsegment(mediaType.IndexOf(ForwardSlashCharacter) + 1);

        StringSegment suffix;
        var startOfSuffix = subType.LastIndexOf(PlusCharacter);
        if (startOfSuffix == -1)
        {
            suffix = default(StringSegment);
        }
        else
        {
            suffix = subType.Subsegment(startOfSuffix + 1);
        }

        if (Suffix.HasValue)
        {
            if (suffix.HasValue)
            {
                return MatchesSubtypeWithoutSuffix(subType, startOfSuffix) && MatchesSubtypeSuffix(suffix);
            }
            else
            {
                return false;
            }
        }
        else
        {
            // If this subtype or suffix matches the subtype of the mediaType,
            // it is considered a subtype.
            // Ex: application/json > application/val+json
            return MatchesEitherSubtypeOrSuffix(subType, suffix);
        }
    }

    private bool MatchesSubtypeWithoutSuffix(MediaTypeHeaderValue set)
    {
        return set.MatchesAllSubTypesWithoutSuffix ||
            set.SubTypeWithoutSuffix.Equals(SubTypeWithoutSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSubtypeWithoutSuffix(StringSegment subType, int startOfSuffix)
    {
        StringSegment subTypeWithoutSuffix;
        if (startOfSuffix == -1)
        {
            subTypeWithoutSuffix = subType;
        }
        else
        {
            subTypeWithoutSuffix = subType.Subsegment(0, startOfSuffix);
        }
        return SubTypeWithoutSuffix.Equals(WildcardString, StringComparison.OrdinalIgnoreCase) ||
            SubTypeWithoutSuffix.Equals(subTypeWithoutSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesEitherSubtypeOrSuffix(MediaTypeHeaderValue set)
    {
        return set.SubType.Equals(SubType, StringComparison.OrdinalIgnoreCase) ||
            set.SubType.Equals(Suffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesEitherSubtypeOrSuffix(StringSegment subType, StringSegment suffix)
    {
        return subType.Equals(SubType, StringComparison.OrdinalIgnoreCase) ||
            SubType.Equals(suffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesParameters(MediaTypeHeaderValue set)
    {
        if (set._parameters != null && set._parameters.Count != 0)
        {
            // Make sure all parameters in the potential superset are included locally. Fine to have additional
            // parameters locally; they make this one more specific.
            foreach (var parameter in set._parameters)
            {
                if (parameter.Name.Equals(WildcardString, StringComparison.OrdinalIgnoreCase))
                {
                    // A parameter named "*" has no effect on media type matching, as it is only used as an indication
                    // that the entire media type string should be treated as a wildcard.
                    continue;
                }

                if (parameter.Name.Equals(QualityString, StringComparison.OrdinalIgnoreCase))
                {
                    // "q" and later parameters are not involved in media type matching. Quoting the RFC: The first
                    // "q" parameter (if any) separates the media-range parameter(s) from the accept-params.
                    break;
                }

                var localParameter = NameValueHeaderValue.Find(_parameters, parameter.Name);
                if (localParameter == null)
                {
                    // Not found.
                    return false;
                }

                if (!StringSegment.Equals(parameter.Value, localParameter.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool MatchesSubtypeSuffix(MediaTypeHeaderValue set)
    {
        // We don't have support for wildcards on suffixes alone (e.g., "application/entity+*")
        // because there's no clear use case for it.
        return set.Suffix.Equals(Suffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSubtypeSuffix(StringSegment suffix)
    {
        // We don't have support for wildcards on suffixes alone (e.g., "application/entity+*")
        // because there's no clear use case for it.
        return Suffix.Equals(suffix, StringComparison.OrdinalIgnoreCase);
    }
}
