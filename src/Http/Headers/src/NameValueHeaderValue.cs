// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

// According to the RFC, in places where a "parameter" is required, the value is mandatory
// (e.g. Media-Type, Accept). However, we don't introduce a dedicated type for it. So NameValueHeaderValue supports
// name-only values in addition to name/value pairs.
/// <summary>
/// Represents a name/value pair used in various headers as defined in RFC 2616.
/// </summary>
public class NameValueHeaderValue
{
    private static readonly HttpHeaderParser<NameValueHeaderValue> SingleValueParser
        = new GenericHeaderParser<NameValueHeaderValue>(false, GetNameValueLength);
    internal static readonly HttpHeaderParser<NameValueHeaderValue> MultipleValueParser
        = new GenericHeaderParser<NameValueHeaderValue>(true, GetNameValueLength);

    private StringSegment _name;
    private StringSegment _value;
    private bool _isReadOnly;

    private NameValueHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NameValueHeaderValue"/>.
    /// </summary>
    /// <param name="name">The header name.</param>
    public NameValueHeaderValue(StringSegment name)
        : this(name, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NameValueHeaderValue"/>.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    public NameValueHeaderValue(StringSegment name, StringSegment value)
    {
        CheckNameValueFormat(name, value);

        _name = name;
        _value = value;
    }

    /// <summary>
    /// Gets the header name.
    /// </summary>
    public StringSegment Name
    {
        get { return _name; }
    }

    /// <summary>
    /// Gets or sets the header value.
    /// </summary>
    public StringSegment Value
    {
        get { return _value; }
        set
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            CheckValueFormat(value);
            _value = value;
        }
    }

    /// <summary>
    /// Gets a value that determines if this header is read only.
    /// </summary>
    public bool IsReadOnly { get { return _isReadOnly; } }

    /// <summary>
    /// Provides a copy of this object without the cost of re-validating the values.
    /// </summary>
    /// <returns>A copy.</returns>
    public NameValueHeaderValue Copy()
    {
        return new NameValueHeaderValue()
        {
            _name = _name,
            _value = _value
        };
    }

    /// <summary>
    /// Provides a copy of this instance while making it immutable.
    /// </summary>
    /// <returns>The readonly <see cref="NameValueHeaderValue"/>.</returns>
    public NameValueHeaderValue CopyAsReadOnly()
    {
        if (IsReadOnly)
        {
            return this;
        }

        return new NameValueHeaderValue()
        {
            _name = _name,
            _value = _value,
            _isReadOnly = true
        };
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        Contract.Assert(_name != null);

        var nameHashCode = StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_name);

        if (!StringSegment.IsNullOrEmpty(_value))
        {
            // If we have a quoted-string, then just use the hash code. If we have a token, convert to lowercase
            // and retrieve the hash code.
            if (_value[0] == '"')
            {
                return nameHashCode ^ _value.GetHashCode();
            }

            return nameHashCode ^ StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_value);
        }

        return nameHashCode;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        var other = obj as NameValueHeaderValue;

        if (other == null)
        {
            return false;
        }

        if (!_name.Equals(other._name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // RFC2616: 14.20: unquoted tokens should use case-INsensitive comparison; quoted-strings should use
        // case-sensitive comparison. The RFC doesn't mention how to compare quoted-strings outside the "Expect"
        // header. We treat all quoted-strings the same: case-sensitive comparison.

        if (StringSegment.IsNullOrEmpty(_value))
        {
            return StringSegment.IsNullOrEmpty(other._value);
        }

        if (_value[0] == '"')
        {
            // We have a quoted string, so we need to do case-sensitive comparison.
            return (_value.Equals(other._value, StringComparison.Ordinal));
        }
        else
        {
            return (_value.Equals(other._value, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// If the value is a quoted-string as defined by <see href="https://tools.ietf.org/html/rfc7230#section-3.2.6">the RFC specification</see>,
    /// removes quotes and unescapes backslashes and quotes.
    /// </summary>
    /// <returns>An unescaped version of <see cref="Value"/>.</returns>
    public StringSegment GetUnescapedValue()
    {
        if (!HeaderUtilities.IsQuoted(_value))
        {
            return _value;
        }
        return HeaderUtilities.UnescapeAsQuotedString(_value);
    }

    /// <summary>
    /// Sets <see cref="Value"/> after it has been quoted as defined by <see href="https://tools.ietf.org/html/rfc7230#section-3.2.6">the RFC specification</see>.
    /// </summary>
    /// <param name="value"></param>
    public void SetAndEscapeValue(StringSegment value)
    {
        HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
        if (StringSegment.IsNullOrEmpty(value) || (GetValueLength(value, 0) == value.Length))
        {
            _value = value;
        }
        else
        {
            Value = HeaderUtilities.EscapeAsQuotedString(value);
        }
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="NameValueHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static NameValueHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return SingleValueParser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="NameValueHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="NameValueHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out NameValueHeaderValue? parsedValue)
    {
        var index = 0;
        return SingleValueParser.TryParseValue(input, ref index, out parsedValue!);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="NameValueHeaderValue"/> values.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<NameValueHeaderValue> ParseList(IList<string>? input)
    {
        return MultipleValueParser.ParseValues(input);
    }

    /// <summary>
    /// Parses a sequence of inputs as a sequence of <see cref="NameValueHeaderValue"/> values using string parsing rules.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static IList<NameValueHeaderValue> ParseStrictList(IList<string>? input)
    {
        return MultipleValueParser.ParseStrictValues(input);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="NameValueHeaderValue"/>.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="NameValueHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseList(IList<string>? input, [NotNullWhen(true)] out IList<NameValueHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseValues(input, out parsedValues);
    }

    /// <summary>
    /// Attempts to parse the sequence of values as a sequence of <see cref="NameValueHeaderValue"/> using string parsing rules.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <param name="parsedValues">The parsed values.</param>
    /// <returns><see langword="true"/> if all inputs are valid <see cref="StringWithQualityHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParseStrictList(IList<string>? input, [NotNullWhen(true)] out IList<NameValueHeaderValue>? parsedValues)
    {
        return MultipleValueParser.TryParseStrictValues(input, out parsedValues);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (!StringSegment.IsNullOrEmpty(_value))
        {
            return _name + "=" + _value;
        }
        return _name.ToString();
    }

    internal static void ToString(
        IList<NameValueHeaderValue>? values,
        char separator,
        bool leadingSeparator,
        StringBuilder destination)
    {
        Contract.Assert(destination != null);

        if ((values == null) || (values.Count == 0))
        {
            return;
        }

        for (var i = 0; i < values.Count; i++)
        {
            if (leadingSeparator || (destination.Length > 0))
            {
                destination.Append(separator);
                destination.Append(' ');
            }
            destination.Append(values[i].Name.AsSpan());
            if (!StringSegment.IsNullOrEmpty(values[i].Value))
            {
                destination.Append('=');
                destination.Append(values[i].Value.AsSpan());
            }
        }
    }

    internal static string? ToString(IList<NameValueHeaderValue>? values, char separator, bool leadingSeparator)
    {
        if ((values == null) || (values.Count == 0))
        {
            return null;
        }

        var sb = new StringBuilder();

        ToString(values, separator, leadingSeparator, sb);

        return sb.ToString();
    }

    internal static int GetHashCode(IList<NameValueHeaderValue>? values)
    {
        if ((values == null) || (values.Count == 0))
        {
            return 0;
        }

        var result = 0;
        for (var i = 0; i < values.Count; i++)
        {
            result = result ^ values[i].GetHashCode();
        }
        return result;
    }

    private static int GetNameValueLength(StringSegment input, int startIndex, out NameValueHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
        {
            return 0;
        }

        // Parse the name, i.e. <name> in name/value string "<name>=<value>". Caller must remove
        // leading whitespaces.
        var nameLength = HttpRuleParser.GetTokenLength(input, startIndex);

        if (nameLength == 0)
        {
            return 0;
        }

        var name = input.Subsegment(startIndex, nameLength);
        var current = startIndex + nameLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        // Parse the separator between name and value
        if ((current == input.Length) || (input[current] != '='))
        {
            // We only have a name and that's OK. Return.
            parsedValue = new NameValueHeaderValue();
            parsedValue._name = name;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
            return current - startIndex;
        }

        current++; // skip delimiter.
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);

        // Parse the value, i.e. <value> in name/value string "<name>=<value>"
        int valueLength = GetValueLength(input, current);

        // Value after the '=' may be empty
        // Use parameterless ctor to avoid double-parsing of name and value, i.e. skip public ctor validation.
        parsedValue = new NameValueHeaderValue();
        parsedValue._name = name;
        parsedValue._value = input.Subsegment(current, valueLength);
        current = current + valueLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
        return current - startIndex;
    }

    // Returns the length of a name/value list, separated by 'delimiter'. E.g. "a=b, c=d, e=f" adds 3
    // name/value pairs to 'nameValueCollection' if 'delimiter' equals ','.
    internal static int GetNameValueListLength(
        StringSegment input,
        int startIndex,
        char delimiter,
        IList<NameValueHeaderValue> nameValueCollection)
    {
        Contract.Requires(startIndex >= 0);

        if ((StringSegment.IsNullOrEmpty(input)) || (startIndex >= input.Length))
        {
            return 0;
        }

        var current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
        while (true)
        {
            var nameValueLength = GetNameValueLength(input, current, out var parameter);

            if (nameValueLength == 0)
            {
                // There may be a trailing ';'
                return current - startIndex;
            }

            nameValueCollection!.Add(parameter!);
            current = current + nameValueLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if ((current == input.Length) || (input[current] != delimiter))
            {
                // We're done and we have at least one valid name/value pair.
                return current - startIndex;
            }

            // input[current] is 'delimiter'. Skip the delimiter and whitespaces and try to parse again.
            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);
        }
    }

    /// <summary>
    /// Finds a <see cref="NameValueHeaderValue"/> with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="values">The collection to search.</param>
    /// <param name="name">The name to find.</param>
    /// <returns>The <see cref="NameValueHeaderValue" /> if found, otherwise <see langword="null" />.</returns>
    public static NameValueHeaderValue? Find(IList<NameValueHeaderValue>? values, StringSegment name)
    {
        Contract.Requires(name.Length > 0);

        if ((values == null) || (values.Count == 0))
        {
            return null;
        }

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }
        return null;
    }

    internal static int GetValueLength(StringSegment input, int startIndex)
    {
        if (startIndex >= input.Length)
        {
            return 0;
        }

        var valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

        if (valueLength == 0)
        {
            // A value can either be a token or a quoted string. Check if it is a quoted string.
            if (HttpRuleParser.GetQuotedStringLength(input, startIndex, out valueLength) != HttpParseResult.Parsed)
            {
                // We have an invalid value. Reset the name and return.
                return 0;
            }
        }
        return valueLength;
    }

    private static void CheckNameValueFormat(StringSegment name, StringSegment value)
    {
        HeaderUtilities.CheckValidToken(name, nameof(name));
        CheckValueFormat(value);
    }

    private static void CheckValueFormat(StringSegment value)
    {
        // Either value is null/empty or a valid token/quoted string
        if (!(StringSegment.IsNullOrEmpty(value) || (GetValueLength(value, 0) == value.Length)))
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "The header value is invalid: '{0}'", value));
        }
    }
}
