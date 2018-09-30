// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // Generated from https://github.com/Azure/azure-rest-api-specs
    public class MatcherAzureBenchmark : MatcherAzureBenchmarkBase
    {
        private const int SampleCount = 100;

        private BarebonesMatcher _baseline;
        private Matcher _dfa;

        private int[] _samples;
        private EndpointSelectorContext _feature;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            // The perf is kinda slow for these benchmarks, so we do some sampling
            // of the request data.
            _samples = SampleRequests(EndpointCount, SampleCount);

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = SetupMatcher(CreateDfaMatcherBuilder());

            _feature = new EndpointSelectorContext();
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = SampleCount)]
        public async Task Baseline()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = Requests[sample];
                await _baseline.Matchers[sample].MatchAsync(httpContext, feature);
                Validate(httpContext, Endpoints[sample], feature.Endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public async Task Dfa()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = Requests[sample];
                await _dfa.MatchAsync(httpContext, feature);
                Validate(httpContext, Endpoints[sample], feature.Endpoint);
            }
        }
    }
}