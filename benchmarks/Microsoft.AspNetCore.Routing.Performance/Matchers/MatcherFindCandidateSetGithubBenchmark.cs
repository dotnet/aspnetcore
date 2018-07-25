// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/APIs-guru/openapi-directory
    // Use https://editor2.swagger.io/ to convert from yaml to json-
    public partial class MatcherFindCandidateSetGithubBenchmark : MatcherBenchmarkBase
    {
        private BarebonesMatcher _baseline;
        private DfaMatcher _dfa;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = (DfaMatcher)SetupMatcher(CreateDfaMatcherBuilder());
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = EndpointCount)]
        public void Baseline()
        {
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = Requests[i];

                var path = httpContext.Request.Path.Value;
                var segments = new ReadOnlySpan<PathSegment>(Array.Empty<PathSegment>());

                var candidates = _baseline.Matchers[i].FindCandidateSet(path, segments);

                var endpoint = candidates[0].Endpoint;
                Validate(httpContext, Endpoints[i], endpoint);
            }
        }

        [Benchmark( OperationsPerInvoke = EndpointCount)]
        public void Dfa()
        {
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = Requests[i];

                var path = httpContext.Request.Path.Value;
                Span<PathSegment> segments = stackalloc PathSegment[FastPathTokenizer.DefaultSegmentCount];
                var count = FastPathTokenizer.Tokenize(path, segments);

                var candidates = _dfa.FindCandidateSet(httpContext, path, segments.Slice(0, count));

                var endpoint = candidates[0].Endpoint;
                Validate(httpContext, Endpoints[i], endpoint);
            }
        }
    }
}