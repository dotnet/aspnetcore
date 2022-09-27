// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// Generated from https://github.com/APIs-guru/openapi-directory
// Use https://editor2.swagger.io/ to convert from yaml to json-
public class MatcherGithubBenchmark : MatcherGithubBenchmarkBase
{
    private BarebonesMatcher _baseline;
    private Matcher _dfa;

    [GlobalSetup]
    public void Setup()
    {
        SetupEndpoints();

        SetupRequests();

        _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
        _dfa = SetupMatcher(CreateDfaMatcherBuilder());
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = EndpointCount)]
    public async Task Baseline()
    {
        for (var i = 0; i < EndpointCount; i++)
        {
            var httpContext = Requests[i];
            await _baseline.Matchers[i].MatchAsync(httpContext);
            Validate(httpContext, Endpoints[i], httpContext.GetEndpoint());
        }
    }

    [Benchmark(OperationsPerInvoke = EndpointCount)]
    public async Task Dfa()
    {
        for (var i = 0; i < EndpointCount; i++)
        {
            var httpContext = Requests[i];
            await _dfa.MatchAsync(httpContext);
            Validate(httpContext, Endpoints[i], httpContext.GetEndpoint());
        }
    }
}
