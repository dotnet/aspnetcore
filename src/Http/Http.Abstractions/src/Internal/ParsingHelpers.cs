// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

internal static class ParsingHelpers
{
    public static StringValues GetHeader(IHeaderDictionary headers, string key)
    {
        StringValues value;
        return headers.TryGetValue(key, out value) ? value : StringValues.Empty;
    }

    public static string[] GetHeaderSplit(IHeaderDictionary headers, string key)
    {
        var values = GetHeaderUnmodified(headers, key);

        ValueListBuilder<string> strings = new([null!, null!, null!, null!]);

        foreach (var segment in new HeaderSegmentCollection(values))
        {
            if (!StringSegment.IsNullOrEmpty(segment.Data))
            {
                var value = DeQuote(segment.Data.Value);
                if (!string.IsNullOrEmpty(value))
                {
                    strings.Add(value);
                }
            }
        }

        string[] result = strings.AsSpan().ToArray();
        strings.Dispose();

        return result;
    }

    public static StringValues GetHeaderUnmodified(IHeaderDictionary headers, string key)
    {
        ArgumentNullException.ThrowIfNull(headers);

        StringValues values;
        return headers.TryGetValue(key, out values) ? values : StringValues.Empty;
    }

    public static void SetHeaderJoined(IHeaderDictionary headers, string key, StringValues value)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (StringValues.IsNullOrEmpty(value))
        {
            headers.Remove(key);
        }
        else
        {
            headers[key] = JoinQuoted(prefix: null, value);
        }
    }

    // Joins the values with ',' (null values are treated as empty), quoting items that contain commas and are not already quoted.
    // When prefix is not null, the result is prefix + "," + joined values.
    private static string JoinQuoted(string? prefix, StringValues values)
    {
        var count = values.Count;

        if (prefix is null && count == 1)
        {
            var single = values[0];
            if (!NeedsQuotes(single))
            {
                return single ?? string.Empty;
            }
        }

        var length = prefix is null ? count - 1 : prefix.Length + count;
        for (var i = 0; i < count; i++)
        {
            var value = values[i];
            if (value is not null)
            {
                length += NeedsQuotes(value) ? value.Length + 2 : value.Length;
            }
        }

        return string.Create(length, (prefix, values), static (span, state) =>
        {
            var position = 0;
            if (state.prefix is not null)
            {
                state.prefix.AsSpan().CopyTo(span);
                position = state.prefix.Length;
                span[position++] = ',';
            }

            var values = state.values;
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    span[position++] = ',';
                }

                var value = values[i];
                if (value is null)
                {
                    continue;
                }

                if (NeedsQuotes(value))
                {
                    span[position++] = '"';
                    value.AsSpan().CopyTo(span.Slice(position));
                    position += value.Length;
                    span[position++] = '"';
                }
                else
                {
                    value.AsSpan().CopyTo(span.Slice(position));
                    position += value.Length;
                }
            }

            Debug.Assert(position == span.Length);
        });
    }

    // Items that contain commas and are not already quoted need to be quoted.
    private static bool NeedsQuotes([NotNullWhen(true)] string? value)
    {
        return !string.IsNullOrEmpty(value) &&
            value.Contains(',') &&
            (value[0] != '"' || value[value.Length - 1] != '"');
    }

    private static string? DeQuote(string? value)
    {
        if (!string.IsNullOrEmpty(value) &&
            (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"'))
        {
            value = value.Substring(1, value.Length - 2);
        }

        return value;
    }

    public static void SetHeaderUnmodified(IHeaderDictionary headers, string key, StringValues? values)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (!values.HasValue || StringValues.IsNullOrEmpty(values.GetValueOrDefault()))
        {
            headers.Remove(key);
        }
        else
        {
            headers[key] = values.GetValueOrDefault();
        }
    }

    public static void AppendHeaderJoined(IHeaderDictionary headers, string key, params string[] values)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(key);

        if (values == null || values.Length == 0)
        {
            return;
        }

        string? existing = GetHeader(headers, key);
        if (existing == null)
        {
            SetHeaderJoined(headers, key, values);
        }
        else
        {
            headers[key] = JoinQuoted(existing, values);
        }
    }

    public static void AppendHeaderUnmodified(IHeaderDictionary headers, string key, StringValues values)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(key);

        if (values.Count == 0)
        {
            return;
        }

        var existing = GetHeaderUnmodified(headers, key);
        SetHeaderUnmodified(headers, key, StringValues.Concat(existing, values));
    }
}
