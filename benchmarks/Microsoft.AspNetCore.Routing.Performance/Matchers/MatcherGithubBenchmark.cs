// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/APIs-guru/openapi-directory
    // Use https://editor2.swagger.io/ to convert from yaml to json-
    public partial class MatcherGithubBenchmark : MatcherBenchmarkBase
    {
        private BarebonesMatcher _baseline;
        private Matcher _dfa;
        private Matcher _tree;

        private EndpointFeature _feature;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = SetupMatcher(new DfaMatcherBuilder());
            _tree = SetupMatcher(new TreeRouterMatcherBuilder());

            _feature = new EndpointFeature();
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = EndpointCount)]
        public async Task Baseline()
        {
            var feature = _feature;
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = Requests[i];
                await _baseline.Matchers[i].MatchAsync(httpContext, feature);
                Validate(httpContext, Endpoints[i], feature.Endpoint);
            }
        }

        [Benchmark( OperationsPerInvoke = EndpointCount)]
        public async Task Dfa()
        {
            var feature = _feature;
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = Requests[i];
                await _dfa.MatchAsync(httpContext, feature);
                Validate(httpContext, Endpoints[i], feature.Endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = EndpointCount)]
        public async Task LegacyTreeRouter()
        {
            var feature = _feature;
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = Requests[i];

                // This is required to make the legacy router implementation work with dispatcher.
                httpContext.Features.Set<IEndpointFeature>(feature);

                await _tree.MatchAsync(httpContext, feature);
                Validate(httpContext, Endpoints[i], feature.Endpoint);
            }
        }
    }
}