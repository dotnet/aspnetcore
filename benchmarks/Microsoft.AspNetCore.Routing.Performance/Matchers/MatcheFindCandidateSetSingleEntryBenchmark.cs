// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class MatcheFindCandidateSetSingleEntryBenchmark : MatcherBenchmarkBase
    {
        private TrivialMatcher _baseline;
        private DfaMatcher _dfa;

        [GlobalSetup]
        public void Setup()
        {
            Endpoints = new MatcherEndpoint[1];
            Endpoints[0] = CreateEndpoint("/plaintext");

            Requests = new HttpContext[1];
            Requests[0] = new DefaultHttpContext();
            Requests[0].RequestServices = CreateServices();
            Requests[0].Request.Path = "/plaintext";
            
            _baseline = (TrivialMatcher)SetupMatcher(new TrivialMatcherBuilder());
            _dfa = (DfaMatcher)SetupMatcher(CreateDfaMatcherBuilder());
        }

        private Matcher SetupMatcher(MatcherBuilder builder)
        {
            builder.AddEndpoint(Endpoints[0]);
            return builder.Build();
        }

        [Benchmark(Baseline = true)]
        public void Baseline()
        {
            var httpContext = Requests[0];
            var path = httpContext.Request.Path.Value;
            var segments = new ReadOnlySpan<PathSegment>(Array.Empty<PathSegment>());

            var candidates = _baseline.FindCandidateSet(path, segments);

            var endpoint = candidates[0].Endpoint;
            Validate(Requests[0], Endpoints[0], endpoint);
        }

        [Benchmark]
        public void Dfa()
        {
            var httpContext = Requests[0];
            var path = httpContext.Request.Path.Value;
            Span<PathSegment> segments = stackalloc PathSegment[FastPathTokenizer.DefaultSegmentCount];
            var count = FastPathTokenizer.Tokenize(path, segments);

            var candidates = _dfa.FindCandidateSet(httpContext, path, segments.Slice(0, count));

            var endpoint = candidates[0].Endpoint;
            Validate(Requests[0], Endpoints[0], endpoint);
        }
    }
}
