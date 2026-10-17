// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

[MemoryDiagnoser]
public class PrefixContainerBenchmark
{
    private LegacyPrefixContainer _existing = default!;
    private PrefixContainer _sortedRange = default!;

    [Params(10, 1_000, 100_000)]
    public int KeyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var keys = PrefixContainerBenchmarkData.CreateKeys(KeyCount);
        _existing = new LegacyPrefixContainer(keys);
        _sortedRange = new PrefixContainer(keys);
    }

    [Benchmark(Baseline = true)]
    public IDictionary<string, string> Existing() => _existing.GetKeysFromPrefix("target");

    [Benchmark]
    public IDictionary<string, string> SortedRange() => _sortedRange.GetKeysFromPrefix("target");
}

[MemoryDiagnoser]
public class PrefixContainerConstructionBenchmark
{
    private string[] _keys = default!;

    [Params(10, 1_000, 100_000)]
    public int KeyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keys = PrefixContainerBenchmarkData.CreateKeys(KeyCount);
    }

    [Benchmark(Baseline = true)]
    public object Existing() => new LegacyPrefixContainer(_keys);

    [Benchmark]
    public PrefixContainer SortedRange() => new(_keys);
}

internal static class PrefixContainerBenchmarkData
{
    public static string[] CreateKeys(int keyCount)
    {
        var keys = new string[keyCount];
        for (var i = 0; i < keys.Length - 1; i++)
        {
            keys[i] = $"key{i}.value";
        }
        keys[^1] = "target.child";

        return keys;
    }
}

// Keeps the implementation from before this change available only to the microbenchmarks,
// so BenchmarkDotNet can compare both algorithms in the same process and configuration.
internal sealed class LegacyPrefixContainer
{
    private readonly ICollection<string> _originalValues;
    private readonly string[] _sortedValues;

    public LegacyPrefixContainer(ICollection<string> values)
    {
        _originalValues = values;

        if (_originalValues.Count == 0)
        {
            _sortedValues = Array.Empty<string>();
        }
        else
        {
            _sortedValues = new string[_originalValues.Count];
            _originalValues.CopyTo(_sortedValues, 0);
            Array.Sort(_sortedValues, StringComparer.OrdinalIgnoreCase);
        }
    }

    public IDictionary<string, string> GetKeysFromPrefix(string prefix)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _originalValues)
        {
            if (entry is not null)
            {
                if (entry.Length == prefix.Length)
                {
                    continue;
                }

                if (prefix.Length == 0)
                {
                    GetKeyFromEmptyPrefix(entry, result);
                }
                else if (entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    GetKeyFromNonEmptyPrefix(prefix, entry, result);
                }
            }
        }

        return result;
    }

    private static void GetKeyFromEmptyPrefix(string entry, IDictionary<string, string> results)
    {
        string key;
        string fullName;
        var delimiterPosition = entry.AsSpan().IndexOfAny('[', '.');

        if (delimiterPosition == 0 && entry[0] == '[')
        {
            var bracketPosition = entry.IndexOf(']', 1);
            if (bracketPosition == -1)
            {
                return;
            }

            key = entry.Substring(1, bracketPosition - 1);
            fullName = entry.Substring(0, bracketPosition + 1);
        }
        else
        {
            key = delimiterPosition == -1 ? entry : entry.Substring(0, delimiterPosition);
            fullName = key;
        }

        if (!results.ContainsKey(key))
        {
            results.Add(key, fullName);
        }
    }

    private static void GetKeyFromNonEmptyPrefix(
        string prefix,
        string entry,
        IDictionary<string, string> results)
    {
        string key;
        string fullName;
        var keyPosition = prefix.Length + 1;

        switch (entry[prefix.Length])
        {
            case '.':
                var delimiterPosition = entry.AsSpan(keyPosition).IndexOfAny('[', '.');
                if (delimiterPosition < 0)
                {
                    key = entry.Substring(keyPosition);
                    fullName = entry;
                }
                else
                {
                    key = entry.Substring(keyPosition, delimiterPosition);
                    fullName = entry.Substring(0, delimiterPosition + keyPosition);
                }
                break;

            case '[':
                var bracketPosition = entry.IndexOf(']', keyPosition);
                if (bracketPosition == -1)
                {
                    return;
                }

                key = entry.Substring(keyPosition, bracketPosition - keyPosition);
                fullName = entry.Substring(0, bracketPosition + 1);
                break;

            default:
                return;
        }

        if (!results.ContainsKey(key))
        {
            results.Add(key, fullName);
        }
    }
}
