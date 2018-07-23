// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class GlobalRoutingMiddlewareTest
    {
        [Fact]
        public async Task Invoke_OnCall_SetsEndpointFeature()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new TestServiceProvider();

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
            var expectedMessage = "Request matched endpoint 'Test endpoint'.";

            var sink = new TestSink(
                TestSink.EnableWithTypeName<GlobalRoutingMiddleware>,
                TestSink.EnableWithTypeName<GlobalRoutingMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new TestServiceProvider();

            var logger = new Logger<GlobalRoutingMiddleware>(loggerFactory);
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
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new TestServiceProvider();

            var middleware = CreateMiddleware();

            // Act
            await middleware.Invoke(httpContext);
            var routeData = httpContext.GetRouteData();
            var routeValue = httpContext.GetRouteValue("controller");
            var endpointFeature = httpContext.Features.Get<IEndpointFeature>();

            // Assert
            Assert.NotNull(routeData);
            Assert.Equal("Home", (string)routeValue);

            // changing route data value is reflected in endpoint feature values
            routeData.Values["testKey"] = "testValue";
            Assert.Equal("testValue", endpointFeature.Values["testKey"]);
        }

        private GlobalRoutingMiddleware CreateMiddleware(Logger<GlobalRoutingMiddleware> logger = null)
        {
            RequestDelegate next = (c) => Task.FromResult<object>(null);

            logger = logger ?? new Logger<GlobalRoutingMiddleware>(NullLoggerFactory.Instance);

            var options = Options.Create(new EndpointOptions());
            var matcherFactory = new TestMatcherFactory(true);
            var middleware = new GlobalRoutingMiddleware(
                matcherFactory,
                new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>()),
                logger,
                next);

            return middleware;
        }
    }
}
