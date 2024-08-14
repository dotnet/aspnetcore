// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

internal static class ComparerHelpers
{
    internal static bool DictionaryEquals<TKey, TValue>(IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y, IEqualityComparer<TValue> comparer)
        where TKey : notnull
        where TValue : notnull
    {
        if (x is Dictionary<TKey, TValue> xDictionary && y is Dictionary<TKey, TValue> yDictionary)
        {
            return DictionaryEquals(xDictionary, yDictionary, comparer);
        }

        if (x.Keys.Count != y.Keys.Count)
        {
            return false;
        }

        foreach (var key in x.Keys)
        {
            if (!y.TryGetValue(key, out var value) || !comparer.Equals(x[key], value))
            {
                return false;
            }
        }

        return true;
    }

    // Private method to avoid interface dispatch.
    private static bool DictionaryEquals<TKey, TValue>(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y, IEqualityComparer<TValue> comparer)
        where TKey : notnull
        where TValue : notnull
    {
        if (x.Keys.Count != y.Keys.Count)
        {
            return false;
        }

        foreach (var key in x.Keys)
        {
            if (!y.TryGetValue(key, out var value) || !comparer.Equals(x[key], value))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ListEquals<T>(IList<T> x, IList<T> y, IEqualityComparer<T> comparer)
    {
        if (x is List<T> xList && y is List<T> yList)
        {
            return ListEquals(xList, yList, comparer);
        }

        if (x.Count != y.Count)
        {
            return false;
        }

        for (var i = 0; i < x.Count; i++)
        {
            if (!comparer.Equals(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }

    // Private method to avoid interface dispatch.
    private static bool ListEquals<T>(List<T> x, List<T> y, IEqualityComparer<T> comparer)
    {
        if (x.Count != y.Count)
        {
            return false;
        }

        for (var i = 0; i < x.Count; i++)
        {
            if (!comparer.Equals(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }
}
