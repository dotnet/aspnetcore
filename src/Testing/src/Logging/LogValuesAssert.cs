// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Shared;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing;

public static class LogValuesAssert
{
    /// <summary>
    /// Asserts that the given key and value are present in the actual values.
    /// </summary>
    /// <param name="key">The key of the item to be found.</param>
    /// <param name="value">The value of the item to be found.</param>
    /// <param name="actualValues">The actual values.</param>
    public static void Contains(
        string key,
        object value,
        IEnumerable<KeyValuePair<string, object>> actualValues)
    {
        Contains(new[] { new KeyValuePair<string, object>(key, value) }, actualValues);
    }

    /// <summary>
    /// Asserts that all the expected values are present in the actual values by ignoring
    /// the order of values.
    /// </summary>
    /// <param name="expectedValues">Expected subset of values</param>
    /// <param name="actualValues">Actual set of values</param>
    public static void Contains(
        IEnumerable<KeyValuePair<string, object>> expectedValues,
        IEnumerable<KeyValuePair<string, object>> actualValues)
    {
        ArgumentNullThrowHelper.ThrowIfNull(expectedValues);
        ArgumentNullThrowHelper.ThrowIfNull(actualValues);

        var comparer = new LogValueComparer();

        foreach (var expectedPair in expectedValues)
        {
            if (!actualValues.Contains(expectedPair, comparer))
            {
                throw new EqualException(
                    expected: GetString(expectedValues),
                    actual: GetString(actualValues));
            }
        }
    }

    private static string GetString(IEnumerable<KeyValuePair<string, object>> logValues)
    {
        return string.Join(",", logValues.Select(kvp => $"[{kvp.Key} {kvp.Value}]"));
    }

    private sealed class LogValueComparer : IEqualityComparer<KeyValuePair<string, object>>
    {
        public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y)
        {
            return string.Equals(x.Key, y.Key) && object.Equals(x.Value, y.Value);
        }

        public int GetHashCode(KeyValuePair<string, object> obj)
        {
            // We are never going to put this KeyValuePair in a hash table,
            // so this is ok.
            throw new NotImplementedException();
        }
    }
}
