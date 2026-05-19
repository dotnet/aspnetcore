// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.LinkGeneration;

public class SingleRouteRouteValuesAddressSchemeBenchmark : EndpointRoutingBenchmarkBase
{
    private IEndpointAddressScheme<RouteValuesAddress> _implementation;
    private TestAddressScheme _baseline;
    private (HttpContext HttpContext, RouteValueDictionary AmbientValues) _requestContext;

    [GlobalSetup]
    public void Setup()
    {
        var template = "Products/Details";
        var defaults = new { controller = "Products", action = "Details" };
        var requiredValues = new { controller = "Products", action = "Details" };

        SetupEndpoints(CreateEndpoint(template, defaults, requiredValues: requiredValues, routeName: "ProductDetails"));
        var services = CreateServices();
        _implementation = services.GetRequiredService<IEndpointAddressScheme<RouteValuesAddress>>();
        _baseline = new TestAddressScheme(Endpoints[0]);

        _requestContext = CreateCurrentRequestContext();
    }

    [Benchmark(Baseline = true)]
    public void Baseline()
    {
        var actual = _baseline.FindEndpoints(address: 0);
    }

    [Benchmark]
    public void RouteValues()
    {
        var actual = _implementation.FindEndpoints(new RouteValuesAddress
        {
            AmbientValues = _requestContext.AmbientValues,
            ExplicitValues = new RouteValueDictionary(new { controller = "Products", action = "Details" }),
            RouteName = null
        });
    }

    [Benchmark]
    public void RouteName()
    {
        var actual = _implementation.FindEndpoints(new RouteValuesAddress
        {
            AmbientValues = _requestContext.AmbientValues,
            RouteName = "ProductDetails"
        });
    }

    private sealed class TestAddressScheme : IEndpointAddressScheme<int>
    {
        private readonly Endpoint _endpoint;

        public TestAddressScheme(Endpoint endpoint)
        {
            _endpoint = endpoint;
        }

        public IEnumerable<Endpoint> FindEndpoints(int address)
        {
            return new[] { _endpoint };
        }
    }
}
