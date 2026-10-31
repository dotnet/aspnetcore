// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// Just like TechEmpower Plaintext
public partial class MatcherSingleEntryBenchmark : EndpointRoutingBenchmarkBase
{
    private BarebonesMatcher _baseline;
    private Matcher _dfa;
    private Matcher _route;
    private Matcher _tree;

    [GlobalSetup]
    public void Setup()
    {
        Endpoints = new RouteEndpoint[1];
        Endpoints[0] = CreateEndpoint("/plaintext");

        Requests = new HttpContext[1];
        Requests[0] = new DefaultHttpContext();
        Requests[0].RequestServices = CreateServices();
        Requests[0].Request.Path = "/plaintext";

        _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
        _dfa = SetupMatcher(CreateDfaMatcherBuilder());
        _route = SetupMatcher(new RouteMatcherBuilder());
        _tree = SetupMatcher(new TreeRouterMatcherBuilder());
    }

    private Matcher SetupMatcher(MatcherBuilder builder)
    {
        builder.AddEndpoint(Endpoints[0]);
        return builder.Build();
    }

    [Benchmark(Baseline = true)]
    public async Task Baseline()
    {
        var httpContext = Requests[0];

        await _baseline.MatchAsync(httpContext);
        Validate(httpContext, Endpoints[0], httpContext.GetEndpoint());
    }

    [Benchmark]
    public async Task Dfa()
    {
        var httpContext = Requests[0];

        await _dfa.MatchAsync(httpContext);
        Validate(httpContext, Endpoints[0], httpContext.GetEndpoint());
    }

    [Benchmark]
    public async Task LegacyTreeRouter()
    {
        var httpContext = Requests[0];

        await _tree.MatchAsync(httpContext);
        Validate(httpContext, Endpoints[0], httpContext.GetEndpoint());
    }

    [Benchmark]
    public async Task LegacyRouter()
    {
        var httpContext = Requests[0];

        await _route.MatchAsync(httpContext);
        Validate(httpContext, Endpoints[0], httpContext.GetEndpoint());
    }
}
