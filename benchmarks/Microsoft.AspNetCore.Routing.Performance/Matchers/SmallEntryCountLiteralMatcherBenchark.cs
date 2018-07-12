// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class SmallEntryCountLiteralMatcherBenchark : MatcherBenchmarkBase
    {
        private Matcher _baseline;
        private Matcher _dfa;
        private Matcher _route;
        private Matcher _tree;

        private EndpointFeature _feature;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            _baseline = SetupMatcher(new TrivialMatcherBuilder());
            _dfa = SetupMatcher(new DfaMatcherBuilder());
            _route = SetupMatcher(new RouteMatcherBuilder());
            _tree = SetupMatcher(new TreeRouterMatcherBuilder());

            _feature = new EndpointFeature();
        }

        private void SetupEndpoints()
        {
            _endpoints = new MatcherEndpoint[10];
            _endpoints[0] = CreateEndpoint("/another-really-cool-entry");
            _endpoints[1] = CreateEndpoint("/Some-Entry");
            _endpoints[2] = CreateEndpoint("/a/path/with/more/segments");
            _endpoints[3] = CreateEndpoint("/random/name");
            _endpoints[4] = CreateEndpoint("/random/name2");
            _endpoints[5] = CreateEndpoint("/random/name3");
            _endpoints[6] = CreateEndpoint("/random/name4");
            _endpoints[7] = CreateEndpoint("/plaintext1");
            _endpoints[8] = CreateEndpoint("/plaintext2");
            _endpoints[9] = CreateEndpoint("/plaintext");
        }

        private void SetupRequests()
        {
            _requests = new HttpContext[1];
            _requests[0] = new DefaultHttpContext();
            _requests[0].RequestServices = CreateServices();
            _requests[0].Request.Path = "/plaintext";
        }

        // For this case we're specifically targeting the last entry to hit 'worst case'
        // performance for the matchers that scale linearly.
        private Matcher SetupMatcher(MatcherBuilder builder)
        {
            builder.AddEndpoint(_endpoints[0]);
            builder.AddEndpoint(_endpoints[1]);
            builder.AddEndpoint(_endpoints[2]);
            builder.AddEndpoint(_endpoints[3]);
            builder.AddEndpoint(_endpoints[4]);
            builder.AddEndpoint(_endpoints[5]);
            builder.AddEndpoint(_endpoints[6]);
            builder.AddEndpoint(_endpoints[7]);
            builder.AddEndpoint(_endpoints[8]);
            builder.AddEndpoint(_endpoints[9]);
            return builder.Build();
        }

        [Benchmark(Baseline = true)]
        public async Task Baseline()
        {
            var feature = _feature;
            await _baseline.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[9], feature.Endpoint);
        }

        [Benchmark]
        public async Task Dfa()
        {
            var feature = _feature;
            await _dfa.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[9], feature.Endpoint);
        }

        [Benchmark]
        public async Task LegacyRoute()
        {
            var feature = _feature;

            // This is required to make the legacy router implementation work with dispatcher.
            _requests[0].Features.Set<IEndpointFeature>(feature);

            await _route.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[9], feature.Endpoint);
        }

        [Benchmark]
        public async Task LegacyTreeRouter()
        {
            var feature = _feature;

            // This is required to make the legacy router implementation work with dispatcher.
            _requests[0].Features.Set<IEndpointFeature>(feature);

            await _tree.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[9], feature.Endpoint);
        }
    }
}
