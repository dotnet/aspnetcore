// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents a collection of <see cref="HealthReportEntry"/> ordered by keys.
/// </summary>
public sealed class HealthReportEntryDictionary : IReadOnlyDictionary<string, HealthReportEntry>
{
    // We make the HealthReportEntryDictionary implements IReadOnlyDictionary, so to have a Dictionary interface to be used by the HealthReport cctor accepting duplicate keys
    private readonly Dictionary<string, List<HealthReportEntry>> _entries;

    /// <summary>
    /// Returns the first <see cref="HealthReportEntry"/> corresponding to the given key
    /// </summary>
    /// <param name="key"></param>
    /// <returns>The first <see cref="HealthReportEntry"/> corresponding to the given key</returns>
    public HealthReportEntry this[string key] => _entries[key].FirstOrDefault();

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _entries.Keys;

    /// <inheritdoc/>
    public IEnumerable<HealthReportEntry> Values => _entries.SelectMany(e => e.Value).AsEnumerable();

    /// <inheritdoc/>
    public int Count => _entries.Count;

    /// <summary>
    /// Create a <see cref="HealthReportEntryDictionary"/> starting from a <see cref="ICollection{KeyValuePair}"/> of <see cref="HealthReportEntry"/> 
    /// </summary>
    /// <param name="entries">An <see cref="ICollection{KeyValuePair}"/> of <see cref="HealthReportEntry"/></param>
    /// <param name="stringComparer">An <see cref="IEqualityComparer"/> to compare keys.</param>
    public HealthReportEntryDictionary(ICollection<KeyValuePair<string, HealthReportEntry>> entries, StringComparer stringComparer = default)
    {
        stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
        _entries = entries.GroupBy(e => e.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Select(s => s.Value).ToList(), stringComparer);
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        return _entries.ContainsKey(key); ;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, HealthReportEntry>> GetEnumerator()
    {
        return _entries.SelectMany(kvp => kvp.Value.Select(v => new KeyValuePair<string, HealthReportEntry>(kvp.Key, v))).GetEnumerator();
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, out HealthReportEntry value)
    {
        foreach (var entry in _entries)
        {
            if (entry.Key.Equals(key, StringComparison.Ordinal))
            {
                value = entry.Value.First();
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
