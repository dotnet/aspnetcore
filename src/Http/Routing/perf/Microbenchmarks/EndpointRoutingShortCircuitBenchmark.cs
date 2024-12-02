// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.ShortCircuit;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing;

public class EndpointRoutingShortCircuitBenchmark
{
    private EndpointRoutingMiddleware _normalEndpointMiddleware;
    private EndpointRoutingMiddleware _shortCircuitEndpointMiddleware;

    [GlobalSetup]
    public void Setup()
    {
        var routingMetrics = new RoutingMetrics(new TestMeterFactory());
        var normalEndpoint = new Endpoint(context => Task.CompletedTask, new EndpointMetadataCollection(), "normal");

        _normalEndpointMiddleware = new EndpointRoutingMiddleware(
            new BenchmarkMatcherFactory(normalEndpoint),
            NullLogger<EndpointRoutingMiddleware>.Instance,
            new BenchmarkEndpointRouteBuilder(),
            new BenchmarkEndpointDataSource(),
            new DiagnosticListener("benchmark"),
            Options.Create(new RouteOptions()),
            routingMetrics,
            context => Task.CompletedTask);

        var shortCircuitEndpoint = new Endpoint(context => Task.CompletedTask, new EndpointMetadataCollection(new ShortCircuitMetadata(200)), "shortcircuit");

        _shortCircuitEndpointMiddleware = new EndpointRoutingMiddleware(
            new BenchmarkMatcherFactory(shortCircuitEndpoint),
            NullLogger<EndpointRoutingMiddleware>.Instance,
            new BenchmarkEndpointRouteBuilder(),
            new BenchmarkEndpointDataSource(),
            new DiagnosticListener("benchmark"),
            Options.Create(new RouteOptions()),
            routingMetrics,
            context => Task.CompletedTask);
    }

    [Benchmark]
    public async Task NormalEndpoint()
    {
        var context = new DefaultHttpContext();
        await _normalEndpointMiddleware.Invoke(context);
    }

    [Benchmark]
    public async Task ShortCircuitEndpoint()
    {
        var context = new DefaultHttpContext();
        await _shortCircuitEndpointMiddleware.Invoke(context);
    }
}

internal class BenchmarkMatcherFactory : MatcherFactory
{
    private readonly Endpoint _endpoint;

    public BenchmarkMatcherFactory(Endpoint endpoint)
    {
        _endpoint = endpoint;
    }

    public override Matcher CreateMatcher(EndpointDataSource dataSource)
    {
        return new BenchmarkMatcher(_endpoint);
    }

    internal class BenchmarkMatcher : Matcher
    {
        private Endpoint _endpoint;

        public BenchmarkMatcher(Endpoint endpoint)
        {
            _endpoint = endpoint;
        }

        public override Task MatchAsync(HttpContext httpContext)
        {
            httpContext.SetEndpoint(_endpoint);
            return Task.CompletedTask;
        }
    }
}

internal class BenchmarkEndpointRouteBuilder : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider => throw new NotImplementedException();

    public ICollection<EndpointDataSource> DataSources => new List<EndpointDataSource>();

    public IApplicationBuilder CreateApplicationBuilder()
    {
        throw new NotImplementedException();
    }
}
internal class BenchmarkEndpointDataSource : EndpointDataSource
{
    public override IReadOnlyList<Endpoint> Endpoints => throw new NotImplementedException();

    public override IChangeToken GetChangeToken()
    {
        throw new NotImplementedException();
    }
}
