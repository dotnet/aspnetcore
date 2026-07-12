// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class StringArrayComparer : IEqualityComparer<string[]>, IAlternateEqualityComparer<ReadOnlySpan<string>, string[]>
{
    public static readonly StringArrayComparer Ordinal = new StringArrayComparer(StringComparer.Ordinal);

    public static readonly StringArrayComparer OrdinalIgnoreCase = new StringArrayComparer(StringComparer.OrdinalIgnoreCase);

    private readonly StringComparer _valueComparer;

    private StringArrayComparer(StringComparer valueComparer)
    {
        _valueComparer = valueComparer;
    }

    public bool Equals(string[]? x, string[]? y)
    {
        if (object.ReferenceEquals(x, y))
        {
            return true;
        }

        if (x == null && y == null)
        {
            return true;
        }

        if (x == null || y == null)
        {
            return false;
        }

        if (x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; i++)
        {
            if (string.IsNullOrEmpty(x[i]) && string.IsNullOrEmpty(y[i]))
            {
                continue;
            }

            if (!_valueComparer.Equals(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(string[] obj)
    {
        if (obj == null)
        {
            return 0;
        }

        var hash = new HashCode();
        for (var i = 0; i < obj.Length; i++)
        {
            // Route values define null and "" to be equivalent.
            hash.Add(obj[i] ?? string.Empty, _valueComparer);
        }

        return hash.ToHashCode();
    }

    // Alternate lookup support: allows probing the dictionary with a ReadOnlySpan<string> key
    // (typically backed by a pooled buffer sliced to the exact length) without allocating a string[].
    // The logic below mirrors the string[] overloads exactly, using the span length as the effective length.
    public bool Equals(ReadOnlySpan<string> alternate, string[] other)
    {
        if (other == null)
        {
            return false;
        }

        if (alternate.Length != other.Length)
        {
            return false;
        }

        for (var i = 0; i < alternate.Length; i++)
        {
            if (string.IsNullOrEmpty(alternate[i]) && string.IsNullOrEmpty(other[i]))
            {
                continue;
            }

            if (!_valueComparer.Equals(alternate[i], other[i]))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(ReadOnlySpan<string> alternate)
    {
        var hash = new HashCode();
        for (var i = 0; i < alternate.Length; i++)
        {
            // Route values define null and "" to be equivalent.
            hash.Add(alternate[i] ?? string.Empty, _valueComparer);
        }

        return hash.ToHashCode();
    }

    // Only used when inserting via the alternate lookup. Action selection only reads, so this is
    // never expected to be called, but it is required by the interface.
    public string[] Create(ReadOnlySpan<string> alternate) => alternate.ToArray();
}
