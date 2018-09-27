// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class EndpointRoutingMiddlewareTest
    {
        [Fact]
        public async Task Invoke_OnCall_SetsEndpointFeature()
        {
            // Arrange
            var httpContext = CreateHttpContext();

            var middleware = CreateMiddleware();

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            var endpointFeature = httpContext.Features.Get<IEndpointFeature>();
            Assert.NotNull(endpointFeature);
        }

        [Fact]
        public async Task Invoke_OnCall_WritesToConfiguredLogger()
        {
            // Arrange
            var expectedMessage = "Request matched endpoint 'Test endpoint'";

            var sink = new TestSink(
                TestSink.EnableWithTypeName<EndpointRoutingMiddleware>,
                TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var httpContext = CreateHttpContext();

            var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);
            var middleware = CreateMiddleware(logger);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Empty(sink.Scopes);
            var write = Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, write.State?.ToString());
        }

        [Fact]
        public async Task Invoke_BackCompatGetRouteValue_ValueUsedFromEndpointFeature()
        {
            // Arrange
            var httpContext = CreateHttpContext();

            var middleware = CreateMiddleware();

            // Act
            await middleware.Invoke(httpContext);
            var routeData = httpContext.GetRouteData();
            var routeValue = httpContext.GetRouteValue("controller");
            var routeValuesFeature = httpContext.Features.Get<IRouteValuesFeature>();

            // Assert
            Assert.NotNull(routeData);
            Assert.Equal("Home", (string)routeValue);

            // changing route data value is reflected in endpoint feature values
            routeData.Values["testKey"] = "testValue";
            Assert.Equal("testValue", routeValuesFeature.RouteValues["testKey"]);
        }

        [Fact]
        public async Task Invoke_BackCompatGetDataTokens_ValueUsedFromEndpointMetadata()
        {
            // Arrange
            var httpContext = CreateHttpContext();

            var middleware = CreateMiddleware();

            // Act
            await middleware.Invoke(httpContext);
            var routeData = httpContext.GetRouteData();
            var routeValue = httpContext.GetRouteValue("controller");
            var routeValuesFeature = httpContext.Features.Get<IRouteValuesFeature>();

            // Assert
            Assert.NotNull(routeData);
            Assert.Equal("Home", (string)routeValue);

            // changing route data value is reflected in endpoint feature values
            routeData.Values["testKey"] = "testValue";
            Assert.Equal("testValue", routeValuesFeature.RouteValues["testKey"]);
        }

        private HttpContext CreateHttpContext()
        {
            var context = new EndpointSelectorContext();

            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IEndpointFeature>(context);
            httpContext.Features.Set<IRouteValuesFeature>(context);

            httpContext.RequestServices = new TestServiceProvider();

            return httpContext;
        }

        private EndpointRoutingMiddleware CreateMiddleware(Logger<EndpointRoutingMiddleware> logger = null)
        {
            RequestDelegate next = (c) => Task.FromResult<object>(null);

            logger = logger ?? new Logger<EndpointRoutingMiddleware>(NullLoggerFactory.Instance);

            var options = Options.Create(new EndpointOptions());
            var matcherFactory = new TestMatcherFactory(true);
            var middleware = new EndpointRoutingMiddleware(
                matcherFactory,
                new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>()),
                logger,
                next);

            return middleware;
        }
    }
}
