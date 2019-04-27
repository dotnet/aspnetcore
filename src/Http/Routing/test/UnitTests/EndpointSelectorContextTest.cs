// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Endpoints;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointSelectorContextTest
    {
        [Fact]
        public void SettingEndpointSetsEndpointOnHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            var ep = new RouteEndpoint(
                    TestConstants.EmptyRequestDelegate,
                    RoutePatternFactory.Parse("/"),
                    0,
                    new EndpointMetadataCollection(),
                    "test");

            new EndpointSelectorContext(httpContext)
            {
                Endpoint = ep,
            };

            // Assert
            var endpoint = httpContext.GetEndpoint();
            Assert.NotNull(endpoint);
            Assert.Same(ep, endpoint);
        }

        [Fact]
        public void SettingRouteValuesSetRouteValuesHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            var routeValues = new RouteValueDictionary(new { A = "1" });

            new EndpointSelectorContext(httpContext)
            {
                RouteValues = routeValues
            };

            // Assert
            Assert.NotNull(httpContext.Request.RouteValues);
            Assert.Same(routeValues, httpContext.Request.RouteValues);
            Assert.Single(httpContext.Request.RouteValues);
            Assert.Equal("1", httpContext.Request.RouteValues["A"]);
        }
    }
}
