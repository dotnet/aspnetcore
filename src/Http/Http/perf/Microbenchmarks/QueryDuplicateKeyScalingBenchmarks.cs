// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Characterizes allocation/timing of <c>QueryFeature</c> parsing as the number of
/// repeated (duplicate) query keys grows. Exercises the <c>KvpAccumulator</c> path that
/// promotes repeated keys into a <see cref="System.Collections.Generic.List{T}"/>, which is
/// the code path affected by the duplicate-key list promotion change.
/// </summary>
[MemoryDiagnoser]
public class QueryDuplicateKeyScalingBenchmarks
{
    private string _repeatedSingleKey = string.Empty;
    private string _uniqueKeys = string.Empty;
    private string _multipleDuplicatedKeys = string.Empty;
    private string _encodedDuplicates = string.Empty;
    private string _longValueDuplicates = string.Empty;

    /// <summary>
    /// Total number of occurrences of a single repeated key. 1 == no duplicate (control,
    /// no promotion); 2+ triggers a single promotion to a backing list, after which each
    /// further occurrence appends to that list (the list grows through its normal capacity
    /// doubling as more values are added).
    /// </summary>
    [Params(1, 2, 3, 4, 8, 16, 32)]
    public int Occurrences { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // One key repeated <Occurrences> times: ?k=v0&k=v1&...
        var sb = new StringBuilder("?");
        for (var i = 0; i < Occurrences; i++)
        {
            if (i > 0)
            {
                sb.Append('&');
            }
            sb.Append("key=value").Append(i);
        }
        _repeatedSingleKey = sb.ToString();

        // Unique-keys control: <Occurrences> DISTINCT keys, no duplicates, no promotion.
        sb.Clear();
        sb.Append('?');
        for (var i = 0; i < Occurrences; i++)
        {
            if (i > 0)
            {
                sb.Append('&');
            }
            sb.Append("key").Append(i).Append("=value").Append(i);
        }
        _uniqueKeys = sb.ToString();

        // Multiple duplicated keys: <Occurrences> distinct keys each duplicated 3x.
        sb.Clear();
        sb.Append('?');
        var first = true;
        for (var k = 0; k < Occurrences; k++)
        {
            for (var d = 0; d < 3; d++)
            {
                if (!first)
                {
                    sb.Append('&');
                }
                first = false;
                sb.Append('k').Append(k).Append("=v").Append(d);
            }
        }
        _multipleDuplicatedKeys = sb.ToString();

        // Percent-encoded duplicate values (decoding path + promotion).
        sb.Clear();
        sb.Append('?');
        for (var i = 0; i < Occurrences; i++)
        {
            if (i > 0)
            {
                sb.Append('&');
            }
            sb.Append("key=value%23").Append(i);
        }
        _encodedDuplicates = sb.ToString();

        // Long-value duplicates (larger strings, same promotion shape).
        var longVal = new string('x', 128);
        sb.Clear();
        sb.Append('?');
        for (var i = 0; i < Occurrences; i++)
        {
            if (i > 0)
            {
                sb.Append('&');
            }
            sb.Append("key=").Append(longVal).Append(i);
        }
        _longValueDuplicates = sb.ToString();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("RepeatedSingleKey")]
    public void RepeatedSingleKey()
    {
        _ = QueryFeature.ParseNullableQueryInternal(_repeatedSingleKey);
    }

    [Benchmark]
    [BenchmarkCategory("UniqueKeysControl")]
    public void UniqueKeysControl()
    {
        _ = QueryFeature.ParseNullableQueryInternal(_uniqueKeys);
    }

    [Benchmark]
    [BenchmarkCategory("MultipleDuplicatedKeys")]
    public void MultipleDuplicatedKeys()
    {
        _ = QueryFeature.ParseNullableQueryInternal(_multipleDuplicatedKeys);
    }

    [Benchmark]
    [BenchmarkCategory("EncodedDuplicates")]
    public void EncodedDuplicates()
    {
        _ = QueryFeature.ParseNullableQueryInternal(_encodedDuplicates);
    }

    [Benchmark]
    [BenchmarkCategory("LongValueDuplicates")]
    public void LongValueDuplicates()
    {
        _ = QueryFeature.ParseNullableQueryInternal(_longValueDuplicates);
    }
}
