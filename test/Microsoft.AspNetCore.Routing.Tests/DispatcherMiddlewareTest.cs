// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class DispatcherMiddlewareTest
    {
        [Fact]
        public async Task Invoke_OnCall_SetsEndpointFeature()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new TestServiceProvider();

            RequestDelegate next = (c) => Task.FromResult<object>(null);

            var logger = new Logger<DispatcherMiddleware>(NullLoggerFactory.Instance);
            var options = Options.Create(new DispatcherOptions());
            var matcherFactory = new TestMatcherFactory(false);
            var middleware = new DispatcherMiddleware(matcherFactory, options, logger, next);

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
                TestSink.EnableWithTypeName<DispatcherMiddleware>,
                TestSink.EnableWithTypeName<DispatcherMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new TestServiceProvider();

            RequestDelegate next = (c) => Task.FromResult<object>(null);

            var logger = new Logger<DispatcherMiddleware>(loggerFactory);
            var options = Options.Create(new DispatcherOptions());
            var matcherFactory = new TestMatcherFactory(true);
            var middleware = new DispatcherMiddleware(matcherFactory, options, logger, next);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Empty(sink.Scopes);
            var write = Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, write.State?.ToString());
        }
    }
}
