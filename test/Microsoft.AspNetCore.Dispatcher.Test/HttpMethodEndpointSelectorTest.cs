// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class HttpMethodEndpointSelectorTest
    {
        [Theory]
        [InlineData("get")]
        [InlineData("Get")]
        [InlineData("GET")]
        public async Task RequestMethod_MatchesEndpointMethod_IgnoresCase(string httpMethod)
        {
            // Arrange
            var endpoints = new List<Endpoint>()
            {
                new RoutePatternEndpoint("{controller=Home}/{action=Index}/{id?}", new { controller = "Products", action = "Get", }, "GET", Products_Get, "Products:Get()"),
                new RoutePatternEndpoint("{controller=Home}/{action=Index}/{id?}", new { controller = "Products", action = "Create", }, "POST", Products_Post, "Products:Post()"),
            };

            var (context, selector) = CreateContextAndSelector(httpMethod, endpoints);

            // Act
            await selector.SelectAsync(context);
            var templateEndpoints = context.Endpoints.Cast<RoutePatternEndpoint>();

            // Assert
            Assert.Collection(
                templateEndpoints,
                endpoint => Assert.Equal(httpMethod.ToUpperInvariant(), endpoint.HttpMethod));
        }

        [Fact]
        public async Task RequestMethod_DoesNotMatch_AnyEndpointMethod()
        {
            // Arrange
            var endpoints = new List<Endpoint>()
            {
                new RoutePatternEndpoint("{controller=Home}/{action=Index}/{id?}", new { controller = "Products", action = "Get", }, "GET", Products_Get, "Products:Get()"),
                new RoutePatternEndpoint("{controller=Home}/{action=Index}/{id?}", new { controller = "Products", action = "Create", }, "POST", Products_Post, "Products:Post()"),
            };

            var (context, selector) = CreateContextAndSelector("PUT", endpoints);

            // Act
            await selector.SelectAsync(context);

            // Assert
            Assert.Empty(context.Endpoints);
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData(null)]
        public async Task RequestMethod_NotSpecifiedOrNotFound_ReturnsFallbackEndpointMethod(string httpMethod)
        {
            // Arrange
            var endpoints = new List<Endpoint>()
            {
                new RoutePatternEndpoint("{controller=Home}/{action=Index}/{id?}", new { controller = "Products", action = "Get", }, "GET", Products_Get, "Products:Get()"),
                new RoutePatternEndpoint("{controller=Home}/{action=Index}/{id?}", new { controller = "Products", action = "Create", }, "POST", Products_Post, "Products:Post()"),
                new RoutePatternEndpoint("{controller=Home}/{action=Index}/{id?}", new { controller = "Products", action = "Get", }, Products_Get),
            };

            var (context, selector) = CreateContextAndSelector(httpMethod, endpoints);

            // Act
            await selector.SelectAsync(context);
            var templateEndpoints = context.Endpoints.Cast<RoutePatternEndpoint>();

            // Assert
            Assert.Collection(
                templateEndpoints,
                endpoint => Assert.Null(endpoint.HttpMethod));
        }

        private (EndpointSelectorContext, EndpointSelector) CreateContextAndSelector(string httpMethod, List<Endpoint> endpoints)
        {
            // Add services to the collection
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod;
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var selector = new HttpMethodEndpointSelector();
            var selectors = new List<EndpointSelector>()
            {
                selector
            };

            var selectorContext = new EndpointSelectorContext(httpContext, new DispatcherValueCollection(), endpoints, selectors);
            return (selectorContext, selector);
        }

        private Task Products_Get(HttpContext httpContext) => httpContext.Response.WriteAsync("Hello, Products_Get");

        private Task Products_Post(HttpContext httpContext) => httpContext.Response.WriteAsync("Hello, Products_Post");
    }
}
