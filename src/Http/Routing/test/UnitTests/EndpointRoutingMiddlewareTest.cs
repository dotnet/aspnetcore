// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;
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
        public async Task Invoke_SkipsRouting_IfEndpointSet()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(), "myapp"));

            var middleware = CreateMiddleware();

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            var endpoint = httpContext.GetEndpoint();
            Assert.NotNull(endpoint);
            Assert.Equal("myapp", endpoint.DisplayName);
        }

        [Fact]
        public async Task Invoke_OnCall_WritesToConfiguredLogger()
        {
            // Arrange
            var expectedMessage = "Request matched endpoint 'Test endpoint'";
            bool eventFired = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<EndpointRoutingMiddleware>,
                TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var listener = new DiagnosticListener("TestListener");

            using var subscription = listener.Subscribe(new DelegateObserver(pair =>
            {
                eventFired = true;

                Assert.Equal("Microsoft.AspNetCore.Routing.EndpointMatched", pair.Key);
                Assert.IsAssignableFrom<HttpContext>(pair.Value);
            }));

            var httpContext = CreateHttpContext();

            var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);
            var middleware = CreateMiddleware(logger, listener: listener);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Empty(sink.Scopes);
            var write = Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, write.State?.ToString());
            Assert.True(eventFired);
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

        [Fact]
        public async Task Invoke_InitializationFailure_AllowsReinitialization()
        {
            // Arrange
            var httpContext = CreateHttpContext();

            var matcherFactory = new Mock<MatcherFactory>();
            matcherFactory
                .Setup(f => f.CreateMatcher(It.IsAny<EndpointDataSource>()))
                .Throws(new InvalidTimeZoneException())
                .Verifiable();

            var middleware = CreateMiddleware(matcherFactory: matcherFactory.Object);

            // Act
            await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await middleware.Invoke(httpContext));
            await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await middleware.Invoke(httpContext));

            // Assert
            matcherFactory
                .Verify(f => f.CreateMatcher(It.IsAny<EndpointDataSource>()), Times.Exactly(2));
        }

        private HttpContext CreateHttpContext()
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new TestServiceProvider()
            };

            return httpContext;
        }

        private EndpointRoutingMiddleware CreateMiddleware(
            Logger<EndpointRoutingMiddleware> logger = null,
            MatcherFactory matcherFactory = null,
            DiagnosticListener listener = null,
            RequestDelegate next = null)
        {
            next ??= c => Task.CompletedTask;
            logger ??= new Logger<EndpointRoutingMiddleware>(NullLoggerFactory.Instance);
            matcherFactory ??= new TestMatcherFactory(true);
            listener ??= new DiagnosticListener("Microsoft.AspNetCore");

            var middleware = new EndpointRoutingMiddleware(
                matcherFactory,
                logger,
                new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>()),
                listener,
                next);

            return middleware;
        }

        private class DelegateObserver : IObserver<KeyValuePair<string, object>>
        {
            private readonly Action<KeyValuePair<string, object>> _onNext;

            public DelegateObserver(Action<KeyValuePair<string, object>> onNext)
            {
                _onNext = onNext;
            }
            public void OnCompleted()
            {

            }

            public void OnError(Exception error)
            {

            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                _onNext(value);
            }
        }
    }
}
