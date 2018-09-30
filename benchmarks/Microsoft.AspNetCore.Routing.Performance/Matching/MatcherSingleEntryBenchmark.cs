// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // Just like TechEmpower Plaintext
    public partial class MatcherSingleEntryBenchmark : EndpointRoutingBenchmarkBase
    {
        private const int SampleCount = 100;

        private BarebonesMatcher _baseline;
        private Matcher _dfa;
        private Matcher _route;
        private Matcher _tree;

        private EndpointSelectorContext _feature;

        [GlobalSetup]
        public void Setup()
        {
            Endpoints = new RouteEndpoint[1];
            Endpoints[0] = CreateEndpoint("/plaintext");

            Requests = new HttpContext[1];
            Requests[0] = new DefaultHttpContext();
            Requests[0].RequestServices = CreateServices();
            Requests[0].Request.Path = "/plaintext";

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = SetupMatcher(CreateDfaMatcherBuilder());
            _route = SetupMatcher(new RouteMatcherBuilder());
            _tree = SetupMatcher(new TreeRouterMatcherBuilder());

            _feature = new EndpointSelectorContext();
        }

        private Matcher SetupMatcher(MatcherBuilder builder)
        {
            builder.AddEndpoint(Endpoints[0]);
            return builder.Build();
        }

        [Benchmark(Baseline = true)]
        public async Task Baseline()
        {
            var feature = _feature;
            var httpContext = Requests[0];

            await _baseline.MatchAsync(httpContext, feature);
            Validate(httpContext, Endpoints[0], feature.Endpoint);
        }

        [Benchmark]
        public async Task Dfa()
        {
            var feature = _feature;
            var httpContext = Requests[0];

            await _dfa.MatchAsync(httpContext, feature);
            Validate(httpContext, Endpoints[0], feature.Endpoint);
        }

        [Benchmark]
        public async Task LegacyTreeRouter()
        {
            var feature = _feature;

            var httpContext = Requests[0];

            // This is required to make the legacy router implementation work with global routing.
            httpContext.Features.Set<IEndpointFeature>(feature);

            await _tree.MatchAsync(httpContext, feature);
            Validate(httpContext, Endpoints[0], feature.Endpoint);
        }

        [Benchmark]
        public async Task LegacyRouter()
        {
            var feature = _feature;
            var httpContext = Requests[0];

            // This is required to make the legacy router implementation work with global routing.
            httpContext.Features.Set<IEndpointFeature>(feature);

            await _route.MatchAsync(httpContext, feature);
            Validate(httpContext, Endpoints[0], feature.Endpoint);
        }
    }
}