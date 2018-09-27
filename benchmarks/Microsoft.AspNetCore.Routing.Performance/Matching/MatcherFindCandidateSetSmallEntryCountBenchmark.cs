// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class MatcherFindCandidateSetSmallEntryCountBenchmark : EndpointRoutingBenchmarkBase
    {
        // SegmentCount should be max-segments + 1
        private const int SegmentCount = 6;

        private TrivialMatcher _baseline;
        private DfaMatcher _dfa;

        private EndpointSelectorContext _feature;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            _baseline = (TrivialMatcher)SetupMatcher(new TrivialMatcherBuilder());
            _dfa = (DfaMatcher)SetupMatcher(CreateDfaMatcherBuilder());

            _feature = new EndpointSelectorContext();
        }

        private void SetupEndpoints()
        {
            Endpoints = new RouteEndpoint[10];
            Endpoints[0] = CreateEndpoint("/another-really-cool-entry");
            Endpoints[1] = CreateEndpoint("/Some-Entry");
            Endpoints[2] = CreateEndpoint("/a/path/with/more/segments");
            Endpoints[3] = CreateEndpoint("/random/name");
            Endpoints[4] = CreateEndpoint("/random/name2");
            Endpoints[5] = CreateEndpoint("/random/name3");
            Endpoints[6] = CreateEndpoint("/random/name4");
            Endpoints[7] = CreateEndpoint("/plaintext1");
            Endpoints[8] = CreateEndpoint("/plaintext2");
            Endpoints[9] = CreateEndpoint("/plaintext");
        }

        private void SetupRequests()
        {
            Requests = new HttpContext[1];
            Requests[0] = new DefaultHttpContext();
            Requests[0].RequestServices = CreateServices();
            Requests[0].Request.Path = "/plaintext";
        }

        // For this case we're specifically targeting the last entry to hit 'worst case'
        // performance for the matchers that scale linearly.
        private Matcher SetupMatcher(MatcherBuilder builder)
        {
            builder.AddEndpoint(Endpoints[0]);
            builder.AddEndpoint(Endpoints[1]);
            builder.AddEndpoint(Endpoints[2]);
            builder.AddEndpoint(Endpoints[3]);
            builder.AddEndpoint(Endpoints[4]);
            builder.AddEndpoint(Endpoints[5]);
            builder.AddEndpoint(Endpoints[6]);
            builder.AddEndpoint(Endpoints[7]);
            builder.AddEndpoint(Endpoints[8]);
            builder.AddEndpoint(Endpoints[9]);
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
            Validate(Requests[0], Endpoints[9], endpoint);
        }

        [Benchmark]
        public void Dfa()
        {
            var httpContext = Requests[0];
            var path = httpContext.Request.Path.Value;

            Span<PathSegment> segments = stackalloc PathSegment[SegmentCount];
            var count = FastPathTokenizer.Tokenize(path, segments);

            var candidates = _dfa.FindCandidateSet(httpContext, path, segments.Slice(0, count));

            var endpoint = candidates[0].Endpoint;
            Validate(Requests[0], Endpoints[9], endpoint);
        }
    }
}
