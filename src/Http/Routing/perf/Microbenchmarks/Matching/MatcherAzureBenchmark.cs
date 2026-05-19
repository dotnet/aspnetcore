// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// Generated from https://github.com/Azure/azure-rest-api-specs
public class MatcherAzureBenchmark : MatcherAzureBenchmarkBase
{
    private const int SampleCount = 100;

    private BarebonesMatcher _baseline;
    private Matcher _dfa;

    private int[] _samples;

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
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = SampleCount)]
    public async Task Baseline()
    {
        for (var i = 0; i < SampleCount; i++)
        {
            var sample = _samples[i];
            var httpContext = Requests[sample];
            await _baseline.Matchers[sample].MatchAsync(httpContext);
            Validate(httpContext, Endpoints[sample], httpContext.GetEndpoint());
        }
    }

    [Benchmark(OperationsPerInvoke = SampleCount)]
    public async Task Dfa()
    {
        for (var i = 0; i < SampleCount; i++)
        {
            var sample = _samples[i];
            var httpContext = Requests[sample];
            await _dfa.MatchAsync(httpContext);
            Validate(httpContext, Endpoints[sample], httpContext.GetEndpoint());
        }
    }
}
