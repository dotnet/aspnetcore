// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/Azure/azure-rest-api-specs
    public partial class AzureMatcherBenchmark : MatcherBenchmarkBase
    {
        private const int SampleCount = 100;

        private Matcher _route;
        private Matcher _tree;

        private int[] _samples;
        private EndpointFeature _feature;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            // The perf is kinda slow for these benchmarks, so we do some sampling
            // of the request data.
            _samples = SampleRequests(EndpointCount, SampleCount);

            _route = SetupMatcher(RouteMatcher.CreateBuilder());
            _tree = SetupMatcher(TreeRouterMatcher.CreateBuilder());

            _feature = new EndpointFeature();
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public async Task LegacyRoute()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];
                await _route.MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[sample], feature.Endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public async Task LegacyTreeRouter()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];
                await _tree.MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[sample], feature.Endpoint);
            }
        }
    }
}