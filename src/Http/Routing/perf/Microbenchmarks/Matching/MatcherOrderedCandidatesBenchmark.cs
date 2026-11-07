// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// Several endpoints with different orders (and therefore different scores) that all end up in the same DFA state.
public class MatcherOrderedCandidatesBenchmark : EndpointRoutingBenchmarkBase
{
    private Matcher _dfa;
    private HttpContext _httpContext;
    private Endpoint _expected;

    // "/products/17" matches the first (best scored) candidate, "/products/abc" matches the third.
    [Params("/products/17", "/products/abc")]
    public string Path { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Endpoints = new[]
        {
            CreateEndpoint("/products/{id:int}", order: 0),
            CreateEndpoint("/products/{id:guid}", order: 1),
            CreateEndpoint("/products/{id:alpha}", order: 2),
            CreateEndpoint("/products/{id:minlength(1)}", order: 3),
            CreateEndpoint("/products/{id}", order: 4),
        };

        var builder = CreateDfaMatcherBuilder();
        foreach (var endpoint in Endpoints)
        {
            builder.AddEndpoint(endpoint);
        }
        _dfa = builder.Build();

        _httpContext = new DefaultHttpContext();
        _httpContext.RequestServices = CreateServices();
        _httpContext.Request.Path = Path;

        _expected = Path == "/products/17" ? Endpoints[0] : Endpoints[2];
    }

    [Benchmark]
    public async Task Dfa()
    {
        var httpContext = _httpContext;

        await _dfa.MatchAsync(httpContext);
        Validate(httpContext, _expected, httpContext.GetEndpoint());
    }
}
