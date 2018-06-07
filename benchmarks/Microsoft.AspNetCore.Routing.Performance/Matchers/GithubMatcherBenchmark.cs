// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/APIs-guru/openapi-directory
    // Use https://editor2.swagger.io/ to convert from yaml to json-
    public partial class GithubMatcherBenchmark : MatcherBenchmarkBase
    {
        private Matcher _route;
        private Matcher _tree;

        private EndpointFeature _feature;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            _route = SetupMatcher(RouteMatcher.CreateBuilder());
            _tree = SetupMatcher(TreeRouterMatcher.CreateBuilder());

            _feature = new EndpointFeature();
        }

        [Benchmark(OperationsPerInvoke = EndpointCount)]
        public async Task LegacyRoute()
        {
            var feature = _feature;
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = _requests[i];
                await _route.MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[i], feature.Endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = EndpointCount)]
        public async Task LegacyTreeRouter()
        {
            var feature = _feature;
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = _requests[i];
                await _tree.MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[i], feature.Endpoint);
            }
        }
    }
}