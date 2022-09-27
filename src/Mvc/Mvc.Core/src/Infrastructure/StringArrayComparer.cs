// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class StringArrayComparer : IEqualityComparer<string[]>
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
}
