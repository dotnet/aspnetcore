// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

internal abstract class HttpHeaderParser<T>
{
    private readonly bool _supportsMultipleValues;

    protected HttpHeaderParser(bool supportsMultipleValues)
    {
        _supportsMultipleValues = supportsMultipleValues;
    }

    public bool SupportsMultipleValues
    {
        get { return _supportsMultipleValues; }
    }

    // If a parser supports multiple values, a call to ParseValue/TryParseValue should return a value for 'index'
    // pointing to the next non-whitespace character after a delimiter. E.g. if called with a start index of 0
    // for string "value , second_value", then after the call completes, 'index' must point to 's', i.e. the first
    // non-whitespace after the separator ','.
    public abstract bool TryParseValue(StringSegment value, ref int index, out T? parsedValue);

    public T? ParseValue(StringSegment value, ref int index)
    {
        // Index may be value.Length (e.g. both 0). This may be allowed for some headers (e.g. Accept but not
        // allowed by others (e.g. Content-Length). The parser has to decide if this is valid or not.
        Contract.Requires((value == null) || ((index >= 0) && (index <= value.Length)));

        // If a parser returns 'null', it means there was no value, but that's valid (e.g. "Accept: "). The caller
        // can ignore the value.
        if (!TryParseValue(value, ref index, out var result))
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                "The header contains invalid values at index {0}: '{1}'", index, value.Value ?? "<null>"));
        }
        return result;
    }

    public virtual bool TryParseValues(IList<string>? values, [NotNullWhen(true)] out IList<T>? parsedValues)
    {
        return TryParseValues(values, strict: false, parsedValues: out parsedValues);
    }

    public virtual bool TryParseStrictValues(IList<string>? values, [NotNullWhen(true)] out IList<T>? parsedValues)
    {
        return TryParseValues(values, strict: true, parsedValues: out parsedValues);
    }

    protected virtual bool TryParseValues(IList<string>? values, bool strict, [NotNullWhen(true)] out IList<T>? parsedValues)
    {
        Contract.Assert(_supportsMultipleValues);
        // If a parser returns an empty list, it means there was no value, but that's valid (e.g. "Accept: "). The caller
        // can ignore the value.
        parsedValues = null;
        List<T>? results = null;
        if (values == null)
        {
            return false;
        }
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            var index = 0;

            while (!string.IsNullOrEmpty(value) && index < value.Length)
            {
                if (TryParseValue(value, ref index, out var output))
                {
                    // The entry may not contain an actual value, like " , "
                    if (output != null)
                    {
                        if (results == null)
                        {
                            results = new List<T>();    // Allocate it only when used
                        }
                        results.Add(output);
                    }
                }
                else if (strict)
                {
                    return false;
                }
                else
                {
                    // Skip the invalid values and keep trying.
                    index++;
                }
            }
        }
        if (results != null)
        {
            parsedValues = results;
            return true;
        }
        return false;
    }

    public virtual IList<T> ParseValues(IList<string>? values)
    {
        return ParseValues(values, strict: false);
    }

    public virtual IList<T> ParseStrictValues(IList<string>? values)
    {
        return ParseValues(values, strict: true);
    }

    protected virtual IList<T> ParseValues(IList<string>? values, bool strict)
    {
        Contract.Assert(_supportsMultipleValues);
        // If a parser returns an empty list, it means there was no value, but that's valid (e.g. "Accept: "). The caller
        // can ignore the value.
        var parsedValues = new List<T>();
        if (values == null)
        {
            return parsedValues;
        }
        foreach (var value in values)
        {
            int index = 0;

            while (!string.IsNullOrEmpty(value) && index < value.Length)
            {
                if (TryParseValue(value, ref index, out var output))
                {
                    // The entry may not contain an actual value, like " , "
                    if (output != null)
                    {
                        parsedValues.Add(output);
                    }
                }
                else if (strict)
                {
                    throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                        "The header contains invalid values at index {0}: '{1}'", index, value));
                }
                else
                {
                    // Skip the invalid values and keep trying.
                    index++;
                }
            }
        }
        return parsedValues;
    }

    // If ValueType is a custom header value type (e.g. NameValueHeaderValue) it implements ToString() correctly.
    // However for existing types like int, byte[], DateTimeOffset we can't override ToString(). Therefore the
    // parser provides a ToString() virtual method that can be overridden by derived types to correctly serialize
    // values (e.g. byte[] to Base64 encoded string).
    // The default implementation is to just call ToString() on the value itself which is the right thing to do
    // for most headers (custom types, string, etc.).
    public virtual string ToString(object value)
    {
        return value.ToString()!;
    }
}
