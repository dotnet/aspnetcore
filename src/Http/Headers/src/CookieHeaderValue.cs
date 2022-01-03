// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

// http://tools.ietf.org/html/rfc6265
/// <summary>
/// Represents the HTTP request <c>Cookie</c> header.
/// </summary>
public class CookieHeaderValue
{
    private static readonly CookieHeaderParser SingleValueParser = new CookieHeaderParser(supportsMultipleValues: false);
    private static readonly CookieHeaderParser MultipleValueParser = new CookieHeaderParser(supportsMultipleValues: true);

    private StringSegment _name;
    private StringSegment _value;

    private CookieHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CookieHeaderValue"/>.
    /// </summary>
    /// <param name="name">The cookie name.</param>
    public CookieHeaderValue(StringSegment name)
        : this(name, StringSegment.Empty)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CookieHeaderValue"/>.
    /// </summary>
    /// <param name="name">The cookie name.</param>
    /// <param name="value">The cookie value.</param>
    public CookieHeaderValue(StringSegment name, StringSegment value)
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
            CheckNameFormat(value, nameof(value));
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
            CheckValueFormat(value, nameof(value));
            _value = value;
        }
    }

    /// <inheritdoc />
    // name="val ue";
    public override string ToString()
    {
        var header = new StringBuilder();

        header.Append(_name.AsSpan());
        header.Append('=');
        header.Append(_value.AsSpan());

        return header.ToString();
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="CookieHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static CookieHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return SingleValueParser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="CookieHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="CookieHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out CookieHeaderValue? parsedValue)
    {
        var index = 0;
        return SingleValueParser.TryParseValue(input, ref index, out parsedValue!);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="CookieHeaderValue"/> values.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<CookieHeaderValue> ParseList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseValues(inputs);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="CookieHeaderValue"/> values using string parsing rules.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<CookieHeaderValue> ParseStrictList(IList<string>? inputs)
    {
        return MultipleValueParser.ParseStrictValues(inputs);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="CookieHeaderValue"/>.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="CookieHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseList(IList<string>? inputs, [NotNullWhen(true)] out IList<CookieHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseValues(inputs, out parsedValues);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="CookieHeaderValue"/> using string parsing rules.
    /// </summary>
    /// <param name="inputs">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="StringWithQualityHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseStrictList(IList<string>? inputs, [NotNullWhen(true)] out IList<CookieHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseStrictValues(inputs, out parsedValues);
    }

    internal static void CheckNameFormat(StringSegment name, string parameterName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (HttpRuleParser.GetTokenLength(name, 0) != name.Length)
        {
            throw new ArgumentException("Invalid cookie name: " + name, parameterName);
        }
    }

    internal static void CheckValueFormat(StringSegment value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var offset = 0;
        var result = CookieHeaderParserShared.GetCookieValue(value, ref offset);
        if (result.Length != value.Length)
        {
            throw new ArgumentException("Invalid cookie value: " + value, parameterName);
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as CookieHeaderValue;

        if (other == null)
        {
            return false;
        }

        return StringSegment.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase)
            && StringSegment.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _name.GetHashCode() ^ _value.GetHashCode();
    }
}
