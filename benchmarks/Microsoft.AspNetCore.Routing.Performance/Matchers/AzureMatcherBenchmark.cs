// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/Azure/azure-rest-api-specs
    public partial class AzureMatcherBenchmark : MatcherBenchmarkBase
    {
        private const int SampleCount = 100;

        private BarebonesMatcher _baseline;
        private Matcher _dfa;
        private Matcher _instruction;
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

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = SetupMatcher(new DfaMatcherBuilder());
            _instruction = SetupMatcher(new InstructionMatcherBuilder());
            _route = SetupMatcher(new RouteMatcherBuilder());
            _tree = SetupMatcher(new TreeRouterMatcherBuilder());

            _feature = new EndpointFeature();
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = SampleCount)]
        public async Task Baseline()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];
                await _baseline._matchers[sample].MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[sample], feature.Endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public async Task Dfa()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                if (sample == 805)
                {
                    GC.KeepAlive(5);
                }
                var httpContext = _requests[sample];
                await _dfa.MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[sample], feature.Endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public async Task Instruction()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];
                await _instruction.MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[sample], feature.Endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public async Task LegacyRoute()
        {
            var feature = _feature;
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];

                // This is required to make the legacy router implementation work with dispatcher.
                httpContext.Features.Set<IEndpointFeature>(feature);

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

                // This is required to make the legacy router implementation work with dispatcher.
                httpContext.Features.Set<IEndpointFeature>(feature);

                await _tree.MatchAsync(httpContext, feature);
                Validate(httpContext, _endpoints[sample], feature.Endpoint);
            }
        }
    }
}