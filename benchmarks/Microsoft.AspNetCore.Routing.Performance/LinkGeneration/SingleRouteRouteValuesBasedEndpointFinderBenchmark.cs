// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.LinkGeneration
{
    public class SingleRouteRouteValuesBasedEndpointFinderBenchmark : EndpointRoutingBenchmarkBase
    {
        private IEndpointFinder<RouteValuesAddress> _finder;
        private TestEndpointFinder _baseFinder;
        private (HttpContext HttpContext, RouteValueDictionary AmbientValues) _requestContext;

        [GlobalSetup]
        public void Setup()
        {
            var template = "Products/Details";
            var defaults = new { controller = "Products", action = "Details" };
            var requiredValues = new { controller = "Products", action = "Details" };

            SetupEndpoints(CreateEndpoint(template, defaults, requiredValues: requiredValues, routeName: "ProductDetails"));
            var services = CreateServices();
            _finder = services.GetRequiredService<IEndpointFinder<RouteValuesAddress>>();
            _baseFinder = new TestEndpointFinder(Endpoints[0]);

            _requestContext = CreateCurrentRequestContext();
        }

        [Benchmark(Baseline = true)]
        public void Baseline()
        {
            var actual = _baseFinder.FindEndpoints(address: 0);
        }

        [Benchmark]
        public void RouteValues()
        {
            var actual = _finder.FindEndpoints(new RouteValuesAddress
            {
                AmbientValues = _requestContext.AmbientValues,
                ExplicitValues = new RouteValueDictionary(new { controller = "Products", action = "Details" }),
                RouteName = null
            });
        }

        [Benchmark]
        public void RouteName()
        {
            var actual = _finder.FindEndpoints(new RouteValuesAddress
            {
                AmbientValues = _requestContext.AmbientValues,
                RouteName = "ProductDetails"
            });
        }

        private class TestEndpointFinder : IEndpointFinder<int>
        {
            private readonly Endpoint _endpoint;

            public TestEndpointFinder(Endpoint endpoint)
            {
                _endpoint = endpoint;
            }

            public IEnumerable<Endpoint> FindEndpoints(int address)
            {
                return new[] { _endpoint };
            }
        }
    }
}
