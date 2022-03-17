// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public struct KeyValueAccumulator
{
    private Dictionary<string, StringValues> _accumulator;
    private Dictionary<string, List<string>> _expandingAccumulator;

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public void Append(string key, string value)
    {
        if (_accumulator == null)
        {
            _accumulator = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        }

        StringValues values;
        if (_accumulator.TryGetValue(key, out values))
        {
            if (values.Count == 0)
            {
                // Marker entry for this key to indicate entry already in expanding list dictionary
                _expandingAccumulator[key].Add(value);
            }
            else if (values.Count == 1)
            {
                // Second value for this key
                _accumulator[key] = new string[] { values[0]!, value };
            }
            else
            {
                // Third value for this key
                // Add zero count entry and move to data to expanding list dictionary
                _accumulator[key] = default(StringValues);

                if (_expandingAccumulator == null)
                {
                    _expandingAccumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }

                // Already 3 entries so use starting allocated as 8; then use List's expansion mechanism for more
                var list = new List<string>(8);
                var array = values.ToArray();

                list.Add(array[0]!);
                list.Add(array[1]!);
                list.Add(value);

                _expandingAccumulator[key] = list;
            }
        }
        else
        {
            // First value for this key
            _accumulator[key] = new StringValues(value);
        }

        ValueCount++;
    }

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool HasValues => ValueCount > 0;

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public int KeyCount => _accumulator?.Count ?? 0;

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public int ValueCount { get; private set; }

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public Dictionary<string, StringValues> GetResults()
    {
        if (_expandingAccumulator != null)
        {
            // Coalesce count 3+ multi-value entries into _accumulator dictionary
            foreach (var entry in _expandingAccumulator)
            {
                _accumulator[entry.Key] = new StringValues(entry.Value.ToArray());
            }
        }

        return _accumulator ?? new Dictionary<string, StringValues>(0, StringComparer.OrdinalIgnoreCase);
    }
}
