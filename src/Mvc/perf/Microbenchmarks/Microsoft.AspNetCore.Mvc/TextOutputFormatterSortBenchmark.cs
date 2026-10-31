// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

/// <summary>
/// Compares the previous and current implementations of the internal
/// <c>TextOutputFormatter.Sort</c> used to order weighted Accept-Charset values.
///
/// The two implementations are inlined here (the production method is private) so the benchmark can
/// measure the sort in isolation, before/after, for different numbers of header values and input
/// orderings:
/// <list type="bullet">
/// <item><c>PreferredFirst</c> - values already listed highest-quality first, which is what real
/// clients send. This is the common case and the one the change is optimized for.</item>
/// <item><c>Reversed</c> - values listed lowest-quality first, included to show each algorithm's
/// pathological ordering.</item>
/// </list>
/// </summary>
[MemoryDiagnoser]
public class TextOutputFormatterSortBenchmark
{
    private const int SortStackAllocThreshold = 32;

    [Params(2, 8, 16, 32, 64)]
    public int HeaderCount { get; set; }

    [Params(true, false)]
    public bool PreferredFirst { get; set; }

    private IList<StringWithQualityHeaderValue> _values = default!;

    [GlobalSetup]
    public void Setup()
    {
        var values = new StringWithQualityHeaderValue[HeaderCount];
        for (var i = 0; i < HeaderCount; i++)
        {
            // Distinct qualities in (0, 1) so ordering is well defined. When PreferredFirst is true the
            // highest quality comes first (descending), otherwise the lowest quality comes first.
            var rank = PreferredFirst ? HeaderCount - i : i + 1;
            var quality = rank / (double)(HeaderCount + 1);
            values[i] = new StringWithQualityHeaderValue($"charset{i}", quality);
        }

        _values = values;
    }

    [Benchmark(Baseline = true)]
    public IList<StringWithQualityHeaderValue> Before() => SortBefore(_values);

    [Benchmark]
    public IList<StringWithQualityHeaderValue> After() => SortAfter(_values);

    // Original implementation: build an ascending list with BinarySearch + Insert, then Reverse.
    private static IList<StringWithQualityHeaderValue> SortBefore(IList<StringWithQualityHeaderValue> values)
    {
        var sortNeeded = false;

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality == HeaderQuality.NoMatch)
            {
                // Exclude this one
            }
            else if (value.Quality != null)
            {
                sortNeeded = true;
            }
        }

        if (!sortNeeded)
        {
            return values;
        }

        var sorted = new List<StringWithQualityHeaderValue>();
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality == HeaderQuality.NoMatch)
            {
                // Exclude this one
            }
            else
            {
                // Doing an insertion sort.
                var position = sorted.BinarySearch(value, StringWithQualityHeaderValueComparer.QualityComparer);
                if (position >= 0)
                {
                    sorted.Insert(position + 1, value);
                }
                else
                {
                    sorted.Insert(~position, value);
                }
            }
        }

        // We want a descending sort, but BinarySearch does ascending
        sorted.Reverse();
        return sorted;
    }

    // Current implementation: stack-allocated index buffer + insertion build for realistic headers,
    // List.Sort + Reverse fallback for the pathological >32 case.
    private static IList<StringWithQualityHeaderValue> SortAfter(IList<StringWithQualityHeaderValue> values)
    {
        var sortNeeded = false;

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality == HeaderQuality.NoMatch)
            {
                // Exclude this one
            }
            else if (value.Quality != null)
            {
                sortNeeded = true;
            }
        }

        if (!sortNeeded)
        {
            return values;
        }

        return values.Count <= SortStackAllocThreshold
            ? SortAfterSmall(values)
            : SortAfterLarge(values);
    }

    private static IList<StringWithQualityHeaderValue> SortAfterSmall(IList<StringWithQualityHeaderValue> values)
    {
        var buffer = new IndexBuffer();
        Span<int> indices = buffer;
        var count = 0;

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality == HeaderQuality.NoMatch)
            {
                continue;
            }

            var j = count;
            while (j > 0 &&
                StringWithQualityHeaderValueComparer.QualityComparer.Compare(values[indices[j - 1]], value) <= 0)
            {
                indices[j] = indices[j - 1];
                j--;
            }

            indices[j] = i;
            count++;
        }

        var sorted = new List<StringWithQualityHeaderValue>(count);
        for (var i = 0; i < count; i++)
        {
            sorted.Add(values[indices[i]]);
        }

        return sorted;
    }

    private static IList<StringWithQualityHeaderValue> SortAfterLarge(IList<StringWithQualityHeaderValue> values)
    {
        var sorted = new List<StringWithQualityHeaderValue>(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality != HeaderQuality.NoMatch)
            {
                sorted.Add(value);
            }
        }

        sorted.Sort(StringWithQualityHeaderValueComparer.QualityComparer);
        sorted.Reverse();
        return sorted;
    }

    [InlineArray(SortStackAllocThreshold)]
    private struct IndexBuffer
    {
#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
        private int _element0;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CA1823 // Avoid unused private fields
    }
}
