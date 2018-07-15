// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/Azure/azure-rest-api-specs
    public partial class MatcherSelectCandidatesAzureBenchmark : MatcherBenchmarkBase
    {
        private const int SampleCount = 100;

        private BarebonesMatcher _baseline;
        private DfaMatcher _dfa;

        private int[] _samples;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            // The perf is kinda slow for these benchmarks, so we do some sampling
            // of the request data.
            _samples = SampleRequests(EndpointCount, SampleCount);

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = (DfaMatcher)SetupMatcher(new DfaMatcherBuilder());
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = SampleCount)]
        public void Baseline()
        {
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = Requests[sample];

                var path = httpContext.Request.Path.Value;
                var segments = new ReadOnlySpan<PathSegment>(Array.Empty<PathSegment>());

                var candidates = _baseline.Matchers[sample].SelectCandidates(path, segments);

                var endpoint = candidates.Candidates[0].Endpoint;
                Validate(httpContext, Endpoints[sample], endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public void Dfa()
        {
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = Requests[sample];

                var path = httpContext.Request.Path.Value;
                Span<PathSegment> segments = stackalloc PathSegment[FastPathTokenizer.DefaultSegmentCount];
                var count = FastPathTokenizer.Tokenize(path, segments);

                var candidates = _dfa.SelectCandidates(path, segments.Slice(0, count));

                var endpoint = candidates.Candidates[0].Endpoint;
                Validate(httpContext, Endpoints[sample], endpoint);
            }
        }
    }
}