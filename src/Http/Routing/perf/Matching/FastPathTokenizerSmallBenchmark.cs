// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching
{
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
}
