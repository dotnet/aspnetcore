// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching;

public class FastPathTokenizerSmallBenchmark : FastPathTokenizerBenchmarkBase
{
    private const int MaxCount = 32;
    private static readonly string Input = "/hello/world/cool";

    // This is a naive reference implementation. We expect to do better.
    [Benchmark(Baseline = true)]
    public unsafe void Baseline()
    {
        var path = Input;
        var segments = stackalloc PathSegment[MaxCount];

        NaiveBaseline(path, segments, MaxCount);
    }

    [Benchmark]
    public void Implementation()
    {
        var path = Input;
        Span<PathSegment> segments = stackalloc PathSegment[MaxCount];

        FastPathTokenizer.Tokenize(path, segments);
    }
}
