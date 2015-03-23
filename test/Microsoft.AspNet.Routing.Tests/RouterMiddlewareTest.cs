// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Core;
using Microsoft.Framework.Logging.Testing;
using Xunit;

namespace Microsoft.AspNet.Routing
{
    public class RouterMiddlewareTest
    {
        [Fact]
        public async void Invoke_LogsCorrectValues_WhenNotHandled()
        {
            // Arrange
            var expectedMessage = "Request did not match any routes.";
            var isHandled = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouterMiddleware>,
                TestSink.EnableWithTypeName<RouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var httpContext = new DefaultHttpContext();
            httpContext.ApplicationServices = new ServiceProvider();
            httpContext.RequestServices = httpContext.ApplicationServices;

            RequestDelegate next = (c) =>
            {
                return Task.FromResult<object>(null);
            };

            var router = new TestRouter(isHandled);
            var middleware = new RouterMiddleware(next, loggerFactory, router);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Empty(sink.Scopes);
            Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, sink.Writes[0].State?.ToString());
        }

        [Fact]
        public async void Invoke_DoesNotLog_WhenHandled()
        {
            // Arrange
            var isHandled = true;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouterMiddleware>,
                TestSink.EnableWithTypeName<RouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var httpContext = new DefaultHttpContext();
            httpContext.ApplicationServices = new ServiceProvider();
            httpContext.RequestServices = httpContext.ApplicationServices;

            RequestDelegate next = (c) =>
            {
                return Task.FromResult<object>(null);
            };

            var router = new TestRouter(isHandled);
            var middleware = new RouterMiddleware(next, loggerFactory, router);

            // Act
            await middleware.Invoke(httpContext);

            // Assert
            Assert.Empty(sink.Scopes);
            Assert.Empty(sink.Writes);
        }

        private class TestRouter : IRouter
        {
            private bool _isHandled;

            public TestRouter(bool isHandled)
            {
                _isHandled = isHandled;
            }

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                return new VirtualPathData(this, "");
            }

            public Task RouteAsync(RouteContext context)
            {
                context.IsHandled = _isHandled;
                return Task.FromResult<object>(null);
            }
        }

        private class ServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
