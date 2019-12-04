// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // Generated from https://github.com/APIs-guru/openapi-directory
    // Use https://editor2.swagger.io/ to convert from yaml to json-
    public class MatcherGithubBenchmark : MatcherGithubBenchmarkBase
    {
        private BarebonesMatcher _baseline;
        private Matcher _dfa;

        private EndpointSelectorContext _feature;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = SetupMatcher(CreateDfaMatcherBuilder());

            _feature = new EndpointSelectorContext();
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
    }
}