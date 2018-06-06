// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class SingleEntryMatcherBenchmark : MatcherBenchmarkBase
    {
        private Matcher _minimal;
        private Matcher _dfa;
        private Matcher _instruction;
        private Matcher _route;
        private Matcher _tree;

        [GlobalSetup]
        public void Setup()
        {
            _endpoints = new MatcherEndpoint[1];
            _endpoints[0] = CreateEndpoint("/plaintext");

            _requests = new HttpContext[1];
            _requests[0] = new DefaultHttpContext();
            _requests[0].RequestServices = CreateServices();
            _requests[0].Request.Path = "/plaintext";

            _minimal = SetupMatcher(MinimalMatcher.CreateBuilder());
            _dfa = SetupMatcher(DfaMatcher.CreateBuilder());
            _instruction = SetupMatcher(InstructionMatcher.CreateBuilder());
            _route = SetupMatcher(RouteMatcher.CreateBuilder());
            _tree = SetupMatcher(TreeRouterMatcher.CreateBuilder());
        }

        private Matcher SetupMatcher(MatcherBuilder builder)
        {
            builder.AddEntry("/plaintext", _endpoints[0]);
            return builder.Build();
        }

        [Benchmark(Baseline = true)]
        public async Task Minimal()
        {
            var feature = new EndpointFeature();
            await _minimal.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[0], feature.Endpoint);
        }

        [Benchmark]
        public async Task Dfa()
        {
            var feature = new EndpointFeature();
            await _dfa.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[0], feature.Endpoint);
        }

        [Benchmark]
        public async Task Instruction()
        {
            var feature = new EndpointFeature();
            await _instruction.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[0], feature.Endpoint);
        }

        [Benchmark]
        public async Task LegacyRoute()
        {
            var feature = new EndpointFeature();
            await _route.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[0], feature.Endpoint);
        }

        [Benchmark]
        public async Task LegacyTreeRouter()
        {
            var feature = new EndpointFeature();
            await _tree.MatchAsync(_requests[0], feature);
            Validate(_requests[0], _endpoints[0], feature.Endpoint);
        }
    }
}
