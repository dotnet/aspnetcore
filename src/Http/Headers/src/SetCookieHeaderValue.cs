// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Represents the <c>Set-Cookie</c> header.
/// <para>
/// See http://tools.ietf.org/html/rfc6265 for the Set-Cookie header specification.
/// </para>
/// </summary>
public class SetCookieHeaderValue
{
    private const string ExpiresToken = "expires";
    private const string MaxAgeToken = "max-age";
    private const string DomainToken = "domain";
    private const string PathToken = "path";
    private const string SecureToken = "secure";
    // RFC Draft: https://tools.ietf.org/html/draft-ietf-httpbis-cookie-same-site-00
    private const string SameSiteToken = "samesite";
    private static readonly string SameSiteNoneToken = SameSiteMode.None.ToString().ToLowerInvariant();
    private static readonly string SameSiteLaxToken = SameSiteMode.Lax.ToString().ToLowerInvariant();
    private static readonly string SameSiteStrictToken = SameSiteMode.Strict.ToString().ToLowerInvariant();

    private const string HttpOnlyToken = "httponly";
    private const string SeparatorToken = "; ";
    private const string EqualsToken = "=";
    private const int ExpiresDateLength = 29;
    private const string ExpiresDateFormat = "r";

    private static readonly HttpHeaderParser<SetCookieHeaderValue> SingleValueParser
        = new GenericHeaderParser<SetCookieHeaderValue>(false, GetSetCookieLength);
    private static readonly HttpHeaderParser<SetCookieHeaderValue> MultipleValueParser
        = new GenericHeaderParser<SetCookieHeaderValue>(true, GetSetCookieLength);

    private StringSegment _name;
    private StringSegment _value;
    private List<StringSegment>? _extensions;

    private SetCookieHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SetCookieHeaderValue"/>.
    /// </summary>
    /// <param name="name">The cookie name.</param>
    public SetCookieHeaderValue(StringSegment name)
        : this(name, StringSegment.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SetCookieHeaderValue"/>.
    /// </summary>
    /// <param name="name">The cookie name.</param>
    /// <param name="value">The cookie value.</param>
    public SetCookieHeaderValue(StringSegment name, StringSegment value)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets or sets the cookie name.
    /// </summary>
    public StringSegment Name
    {
        get { return _name; }
        set
        {
            CookieHeaderValue.CheckNameFormat(value, nameof(value));
            _name = value;
        }
    }

    /// <summary>
    /// Gets or sets the cookie value.
    /// </summary>
    public StringSegment Value
    {
        get { return _value; }
        set
        {
            CookieHeaderValue.CheckValueFormat(value, nameof(value));
            _value = value;
        }
    }

    /// <summary>
    /// Gets or sets a value for the <c>Expires</c> cookie attribute.
    /// <para>
    /// The Expires attribute indicates the maximum lifetime of the cookie,
    /// represented as the date and time at which the cookie expires.
    /// </para>
    /// </summary>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6265#section-4.1.2.1"/>.</remarks>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Gets or sets a value for the <c>Max-Age</c> cookie attribute.
    /// <para>
    /// The Max-Age attribute indicates the maximum lifetime of the cookie,
    /// represented as the number of seconds until the cookie expires.
    /// </para>
    /// </summary>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6265#section-4.1.2.2"/>.</remarks>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets a value for the <c>Domain</c> cookie attribute.
    /// <para>
    /// The Domain attribute specifies those hosts to which the cookie will
    /// be sent.
    /// </para>
    /// </summary>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6265#section-4.1.2.3"/>.</remarks>
    public StringSegment Domain { get; set; }

    /// <summary>
    /// Gets or sets a value for the <c>Path</c> cookie attribute.
    /// <para>
    /// The path attribute specifies those hosts to which the cookie will
    /// be sent.
    /// </para>
    /// </summary>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6265#section-4.1.2.4"/>.</remarks>
    public StringSegment Path { get; set; }

    /// <summary>
    /// Gets or sets a value for the <c>Secure</c> cookie attribute.
    /// <para>
    /// The Secure attribute limits the scope of the cookie to "secure"
    /// channels.
    /// </para>
    /// </summary>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6265#section-4.1.2.5"/>.</remarks>
    public bool Secure { get; set; }

    /// <summary>
    /// Gets or sets a value for the <c>SameSite</c> cookie attribute.
    /// <para>
    /// "SameSite" cookies offer a robust defense against CSRF attack when
    /// deployed in strict mode, and when supported by the client.
    /// </para>
    /// </summary>
    /// <remarks>See <see href="https://tools.ietf.org/html/draft-ietf-httpbis-rfc6265bis-05#section-8.8"/>.</remarks>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Unspecified;

    /// <summary>
    /// Gets or sets a value for the <c>HttpOnly</c> cookie attribute.
    /// <para>
    /// HttpOnly instructs the user agent to
    /// omit the cookie when providing access to cookies via "non-HTTP" APIs
    /// (such as a web browser API that exposes cookies to scripts).
    /// </para>
    /// </summary>
    /// <remarks>See <see href="https://tools.ietf.org/html/rfc6265#section-4.1.2.6"/>.</remarks>
    public bool HttpOnly { get; set; }

    /// <summary>
    /// Gets a collection of additional values to append to the cookie.
    /// </summary>
    public IList<StringSegment> Extensions
    {
        get => _extensions ??= new List<StringSegment>();
    }

    // name="value"; expires=Sun, 06 Nov 1994 08:49:37 GMT; max-age=86400; domain=domain1; path=path1; secure; samesite={strict|lax|none}; httponly
    /// <inheritdoc />
    public override string ToString()
    {
        var length = _name.Length + EqualsToken.Length + _value.Length;

        string? maxAge = null;
        string? sameSite = null;

        if (Expires.HasValue)
        {
            length += SeparatorToken.Length + ExpiresToken.Length + EqualsToken.Length + ExpiresDateLength;
        }

        if (MaxAge.HasValue)
        {
            maxAge = HeaderUtilities.FormatInt64((long)MaxAge.GetValueOrDefault().TotalSeconds);
            length += SeparatorToken.Length + MaxAgeToken.Length + EqualsToken.Length + maxAge.Length;
        }

        if (Domain != null)
        {
            length += SeparatorToken.Length + DomainToken.Length + EqualsToken.Length + Domain.Length;
        }

        if (Path != null)
        {
            length += SeparatorToken.Length + PathToken.Length + EqualsToken.Length + Path.Length;
        }

        if (Secure)
        {
            length += SeparatorToken.Length + SecureToken.Length;
        }

        // Allow for Unspecified (-1) to skip SameSite
        if (SameSite == SameSiteMode.None)
        {
            sameSite = SameSiteNoneToken;
            length += SeparatorToken.Length + SameSiteToken.Length + EqualsToken.Length + sameSite.Length;
        }
        else if (SameSite == SameSiteMode.Lax)
        {
            sameSite = SameSiteLaxToken;
            length += SeparatorToken.Length + SameSiteToken.Length + EqualsToken.Length + sameSite.Length;
        }
        else if (SameSite == SameSiteMode.Strict)
        {
            sameSite = SameSiteStrictToken;
            length += SeparatorToken.Length + SameSiteToken.Length + EqualsToken.Length + sameSite.Length;
        }

        if (HttpOnly)
        {
            length += SeparatorToken.Length + HttpOnlyToken.Length;
        }

        if (_extensions?.Count > 0)
        {
            foreach (var extension in _extensions)
            {
                length += SeparatorToken.Length + extension.Length;
            }
        }

        return string.Create(length, (this, maxAge, sameSite), (span, tuple) =>
        {
            var (headerValue, maxAgeValue, sameSite) = tuple;

            Append(ref span, headerValue._name);
            Append(ref span, EqualsToken);
            Append(ref span, headerValue._value);

            if (headerValue.Expires is DateTimeOffset expiresValue)
            {
                Append(ref span, SeparatorToken);
                Append(ref span, ExpiresToken);
                Append(ref span, EqualsToken);

                var formatted = expiresValue.TryFormat(span, out var charsWritten, ExpiresDateFormat, CultureInfo.InvariantCulture);
                span = span.Slice(charsWritten);

                Debug.Assert(formatted);
            }

            if (maxAgeValue != null)
            {
                AppendSegment(ref span, MaxAgeToken, maxAgeValue);
            }

            if (headerValue.Domain != null)
            {
                AppendSegment(ref span, DomainToken, headerValue.Domain);
            }

            if (headerValue.Path != null)
            {
                AppendSegment(ref span, PathToken, headerValue.Path);
            }

            if (headerValue.Secure)
            {
                AppendSegment(ref span, SecureToken, null);
            }

            if (sameSite != null)
            {
                AppendSegment(ref span, SameSiteToken, sameSite);
            }

            if (headerValue.HttpOnly)
            {
                AppendSegment(ref span, HttpOnlyToken, null);
            }

            if (_extensions?.Count > 0)
            {
                foreach (var extension in _extensions)
                {
                    AppendSegment(ref span, extension, null);
                }
            }
        });
    }

    private static void AppendSegment(ref Span<char> span, StringSegment name, StringSegment value)
    {
        Append(ref span, SeparatorToken);
        Append(ref span, name.AsSpan());
        if (value != null)
        {
            Append(ref span, EqualsToken);
            Append(ref span, value.AsSpan());
        }
    }

    private static void Append(ref Span<char> span, ReadOnlySpan<char> other)
    {
        other.CopyTo(span);
        span = span.Slice(other.Length);
    }

    /// <summary>
    /// Append string representation of this <see cref="SetCookieHeaderValue"/> to given
    /// <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="StringBuilder"/> to receive the string representation of this
    /// <see cref="SetCookieHeaderValue"/>.
    /// </param>
    public void AppendToStringBuilder(StringBuilder builder)
    {
        builder.Append(_name.AsSpan());
        builder.Append('=');
        builder.Append(_value.AsSpan());

        if (Expires.HasValue)
        {
            AppendSegment(builder, ExpiresToken, HeaderUtilities.FormatDate(Expires.GetValueOrDefault()));
        }

        if (MaxAge.HasValue)
        {
            AppendSegment(builder, MaxAgeToken, HeaderUtilities.FormatInt64((long)MaxAge.GetValueOrDefault().TotalSeconds));
        }

        if (Domain != null)
        {
            AppendSegment(builder, DomainToken, Domain);
        }

        if (Path != null)
        {
            AppendSegment(builder, PathToken, Path);
        }

        if (Secure)
        {
            AppendSegment(builder, SecureToken, null);
        }

        // Allow for Unspecified (-1) to skip SameSite
        if (SameSite == SameSiteMode.None)
        {
            AppendSegment(builder, SameSiteToken, SameSiteNoneToken);
        }
        else if (SameSite == SameSiteMode.Lax)
        {
            AppendSegment(builder, SameSiteToken, SameSiteLaxToken);
        }
        else if (SameSite == SameSiteMode.Strict)
        {
            AppendSegment(builder, SameSiteToken, SameSiteStrictToken);
        }

        if (HttpOnly)
        {
            AppendSegment(builder, HttpOnlyToken, null);
        }

        if (_extensions?.Count > 0)
        {
            foreach (var extension in _extensions)
            {
                AppendSegment(builder, extension, null);
            }
        }
    }

    private static void AppendSegment(StringBuilder builder, StringSegment name, StringSegment value)
    {
        builder.Append("; ");
        builder.Append(name.AsSpan());
        if (value != null)
        {
            builder.Append('=');
            builder.Append(value.AsSpan());
        }
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="SetCookieHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static SetCookieHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return SingleValueParser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="SetCookieHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="SetCookieHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out SetCookieHeaderValue? parsedValue)
    {
        var index = 0;
        return SingleValueParser.TryParseValue(input, ref index, out parsedValue!);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="SetCookieHeaderValue"/> values.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<SetCookieHeaderValue> ParseList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseValues(inputs);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="SetCookieHeaderValue"/> values using string parsing rules.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<SetCookieHeaderValue> ParseStrictList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseStrictValues(inputs);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="SetCookieHeaderValue"/>.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="SetCookieHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseList(IList<string>? inputs, [NotNullWhen(true)] out IList<SetCookieHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseValues(inputs, out parsedValues);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="SetCookieHeaderValue"/> using string parsing rules.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="StringWithQualityHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseStrictList(IList<string>? inputs, [NotNullWhen(true)] out IList<SetCookieHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseStrictValues(inputs, out parsedValues);
    }

    // name=value; expires=Sun, 06 Nov 1994 08:49:37 GMT; max-age=86400; domain=domain1; path=path1; secure; samesite={Strict|Lax|None}; httponly
    private static int GetSetCookieLength(StringSegment input, int startIndex, out SetCookieHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);
        var offset = startIndex;

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (offset >= input.Length))
        {
            return 0;
        }

        var result = new SetCookieHeaderValue();

        // The caller should have already consumed any leading whitespace, commas, etc..

        // Name=value;

        // Name
        var itemLength = HttpRuleParser.GetTokenLength(input, offset);
        if (itemLength == 0)
        {
            return 0;
        }
        result._name = input.Subsegment(offset, itemLength);
        offset += itemLength;

        // = (no spaces)
        if (!ReadEqualsSign(input, ref offset))
        {
            return 0;
        }

        // value or "quoted value"
        // The value may be empty
        result._value = CookieHeaderParserShared.GetCookieValue(input, ref offset);

        // *(';' SP cookie-av)
        while (offset < input.Length)
        {
            if (input[offset] == ',')
            {
                // Divider between headers
                break;
            }
            if (input[offset] != ';')
            {
                // Expecting a ';' between parameters
                return 0;
            }
            offset++;

            offset += HttpRuleParser.GetWhitespaceLength(input, offset);

            //  cookie-av = expires-av / max-age-av / domain-av / path-av / secure-av / samesite-av / httponly-av / extension-av
            itemLength = HttpRuleParser.GetTokenLength(input, offset);
            if (itemLength == 0)
            {
                // Trailing ';' or leading into garbage. Let the next parser fail.
                break;
            }
            var token = input.Subsegment(offset, itemLength);
            offset += itemLength;

            //  expires-av = "Expires=" sane-cookie-date
            if (StringSegment.Equals(token, ExpiresToken, StringComparison.OrdinalIgnoreCase))
            {
                // = (no spaces)
                if (!ReadEqualsSign(input, ref offset))
                {
                    return 0;
                }
                // We don't want to include comma, becouse date may contain it (eg. Sun, 06 Nov...)
                var dateString = ReadToSemicolonOrEnd(input, ref offset, includeComma: false);
                DateTimeOffset expirationDate;
                if (!HttpRuleParser.TryStringToDate(dateString, out expirationDate))
                {
                    // Invalid expiration date, abort
                    return 0;
                }
                result.Expires = expirationDate;
            }
            // max-age-av = "Max-Age=" digit *DIGIT ; valid positive and negative values following the RFC6265, Section 5.2.2
            else if (StringSegment.Equals(token, MaxAgeToken, StringComparison.OrdinalIgnoreCase))
            {
                // = (no spaces)
                if (!ReadEqualsSign(input, ref offset))
                {
                    return 0;
                }

                var isNegative = false;
                if (input[offset] == '-')
                {
                    isNegative = true;
                    offset++;
                }

                itemLength = HttpRuleParser.GetNumberLength(input, offset, allowDecimal: false);
                if (itemLength == 0)
                {
                    return 0;
                }

                var numberString = input.Subsegment(offset, itemLength);
                long maxAge;
                if (!HeaderUtilities.TryParseNonNegativeInt64(numberString, out maxAge))
                {
                    // Invalid expiration date, abort
                    return 0;
                }

                if (isNegative)
                {
                    maxAge = -maxAge;
                }

                result.MaxAge = TimeSpan.FromSeconds(maxAge);
                offset += itemLength;
            }
            // domain-av = "Domain=" domain-value
            // domain-value = <subdomain> ; defined in [RFC1034], Section 3.5, as enhanced by [RFC1123], Section 2.1
            else if (StringSegment.Equals(token, DomainToken, StringComparison.OrdinalIgnoreCase))
            {
                // = (no spaces)
                if (!ReadEqualsSign(input, ref offset))
                {
                    return 0;
                }
                // We don't do any detailed validation on the domain.
                result.Domain = ReadToSemicolonOrEnd(input, ref offset);
            }
            // path-av = "Path=" path-value
            // path-value = <any CHAR except CTLs or ";">
            else if (StringSegment.Equals(token, PathToken, StringComparison.OrdinalIgnoreCase))
            {
                // = (no spaces)
                if (!ReadEqualsSign(input, ref offset))
                {
                    return 0;
                }
                // We don't do any detailed validation on the path.
                result.Path = ReadToSemicolonOrEnd(input, ref offset);
            }
            // secure-av = "Secure"
            else if (StringSegment.Equals(token, SecureToken, StringComparison.OrdinalIgnoreCase))
            {
                result.Secure = true;
            }
            // samesite-av = "SameSite=" samesite-value
            // samesite-value = "Strict" / "Lax" / "None"
            else if (StringSegment.Equals(token, SameSiteToken, StringComparison.OrdinalIgnoreCase))
            {
                if (!ReadEqualsSign(input, ref offset))
                {
                    result.SameSite = SameSiteMode.Unspecified;
                }
                else
                {
                    var enforcementMode = ReadToSemicolonOrEnd(input, ref offset);

                    if (StringSegment.Equals(enforcementMode, SameSiteStrictToken, StringComparison.OrdinalIgnoreCase))
                    {
                        result.SameSite = SameSiteMode.Strict;
                    }
                    else if (StringSegment.Equals(enforcementMode, SameSiteLaxToken, StringComparison.OrdinalIgnoreCase))
                    {
                        result.SameSite = SameSiteMode.Lax;
                    }
                    else if (StringSegment.Equals(enforcementMode, SameSiteNoneToken, StringComparison.OrdinalIgnoreCase))
                    {
                        result.SameSite = SameSiteMode.None;
                    }
                    else
                    {
                        result.SameSite = SameSiteMode.Unspecified;
                    }
                }
            }
            // httponly-av = "HttpOnly"
            else if (StringSegment.Equals(token, HttpOnlyToken, StringComparison.OrdinalIgnoreCase))
            {
                result.HttpOnly = true;
            }
            // extension-av = <any CHAR except CTLs or ";">
            else
            {
                var tokenStart = offset - itemLength;
                ReadToSemicolonOrEnd(input, ref offset, includeComma: true);
                result.Extensions.Add(input.Subsegment(tokenStart, offset - tokenStart));
            }
        }

        parsedValue = result;
        return offset - startIndex;
    }

    private static bool ReadEqualsSign(StringSegment input, ref int offset)
    {
        // = (no spaces)
        if (offset >= input.Length || input[offset] != '=')
        {
            return false;
        }
        offset++;
        return true;
    }

    private static StringSegment ReadToSemicolonOrEnd(StringSegment input, ref int offset, bool includeComma = true)
    {
        var end = input.IndexOf(';', offset);
        if (end < 0)
        {
            // Also valid end of cookie
            if (includeComma)
            {
                end = input.IndexOf(',', offset);
            }
        }
        else if (includeComma)
        {
            var commaPosition = input.IndexOf(',', offset);
            if (commaPosition >= 0 && commaPosition < end)
            {
                end = commaPosition;
            }
        }

        if (end < 0)
        {
            // Remainder of the string
            end = input.Length;
        }

        var itemLength = end - offset;
        var result = input.Subsegment(offset, itemLength);
        offset += itemLength;
        return result;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as SetCookieHeaderValue;

        if (other == null)
        {
            return false;
        }

        return StringSegment.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase)
            && StringSegment.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase)
            && Expires.Equals(other.Expires)
            && MaxAge.Equals(other.MaxAge)
            && StringSegment.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase)
            && StringSegment.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && Secure == other.Secure
            && SameSite == other.SameSite
            && HttpOnly == other.HttpOnly
            && HeaderUtilities.AreEqualCollections(_extensions, other._extensions, StringSegmentComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_name)
            ^ StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_value)
            ^ (Expires.HasValue ? Expires.GetHashCode() : 0)
            ^ (MaxAge.HasValue ? MaxAge.GetHashCode() : 0)
            ^ (Domain != null ? StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(Domain) : 0)
            ^ (Path != null ? StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(Path) : 0)
            ^ Secure.GetHashCode()
            ^ SameSite.GetHashCode()
            ^ HttpOnly.GetHashCode();

        if (_extensions?.Count > 0)
        {
            foreach (var extension in _extensions)
            {
                hash ^= extension.GetHashCode();
            }
        }

        return hash;
    }
}
