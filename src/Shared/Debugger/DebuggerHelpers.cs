// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Shared;

internal static class DebuggerHelpers
{
    public static string GetDebugText(string key1, object? value1, bool includeNullValues = true, string? prefix = null)
    {
        return GetDebugText(new KeyValuePair<string, object?>[] { Create(key1, value1) }, includeNullValues, prefix);
    }

    public static string GetDebugText(string key1, object? value1, string key2, object? value2, bool includeNullValues = true, string? prefix = null)
    {
        return GetDebugText(new KeyValuePair<string, object?>[] { Create(key1, value1), Create(key2, value2) }, includeNullValues, prefix);
    }

    public static string GetDebugText(string key1, object? value1, string key2, object? value2, string key3, object? value3, bool includeNullValues = true, string? prefix = null)
    {
        return GetDebugText(new KeyValuePair<string, object?>[] { Create(key1, value1), Create(key2, value2), Create(key3, value3) }, includeNullValues, prefix);
    }

    public static string GetDebugText(ReadOnlySpan<KeyValuePair<string, object?>> values, bool includeNullValues = true, string? prefix = null)
    {
        if (values.Length == 0)
        {
            return prefix ?? string.Empty;
        }

        var sb = new StringBuilder();
        if (prefix != null)
        {
            sb.Append(prefix);
        }

        var first = true;
        for (var i = 0; i < values.Length; i++)
        {
            var kvp = values[i];

            if (HasValue(kvp.Value) || includeNullValues)
            {
                if (first)
                {
                    if (prefix != null)
                    {
                        sb.Append(' ');
                    }

                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(kvp.Key);
                sb.Append(": ");
                if (kvp.Value is null)
                {
                    sb.Append("(null)");
                }
                else if (kvp.Value is string s)
                {
                    sb.Append(s);
                }
                else if (kvp.Value is IEnumerable enumerable)
                {
                    var firstItem = true;
                    foreach (var item in enumerable)
                    {
                        if (firstItem)
                        {
                            firstItem = false;
                        }
                        else
                        {
                            sb.Append(',');
                        }
                        sb.Append(item);
                    }
                }
                else
                {
                    sb.Append(kvp.Value);
                }
            }
        }

        return sb.ToString();
    }

    private static bool HasValue(object? value)
    {
        if (value is null)
        {
            return false;
        }

        // Empty collections don't have a value.
        if (value is not string && value is IEnumerable enumerable && !enumerable.GetEnumerator().MoveNext())
        {
            return false;
        }

        return true;
    }

    private static KeyValuePair<string, object?> Create(string key, object? value) => new KeyValuePair<string, object?>(key, value);
}
