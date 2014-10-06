// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.Framework.Logging;
#if ASPNET50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Routing
{
    public class RouterMiddlewareTest
    {
#if ASPNET50
        [Fact]
        public async void Invoke_LogsCorrectValuesWhenNotHandled()
        {
            // Arrange
            var isHandled = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouterMiddleware>, 
                TestSink.EnableWithTypeName<RouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
            mockContext.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);

            RequestDelegate next = (c) =>
            {
                return Task.FromResult<object>(null);
            };

            var router = new TestRouter(isHandled);
            var middleware = new RouterMiddleware(next, router);

            // Act
            await middleware.Invoke(mockContext.Object);

            // Assert
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouterMiddleware).FullName, scope.LoggerName);
            Assert.Equal("RouterMiddleware.Invoke", scope.Scope);

            Assert.Single(sink.Writes);

            var write = sink.Writes[0];
            Assert.Equal(typeof(RouterMiddleware).FullName, write.LoggerName);
            Assert.Equal("RouterMiddleware.Invoke", write.Scope);
            var values = Assert.IsType<RouterMiddlewareInvokeValues>(write.State);
            Assert.Equal("RouterMiddleware.Invoke", values.Name);
            Assert.Equal(false, values.Handled);
        }

        [Fact]
        public async void Invoke_DoesNotLogWhenDisabledAndNotHandled()
        {
            // Arrange
            var isHandled = false;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouterMiddleware>,
                TestSink.EnableWithTypeName<RouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: false);

            var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
            mockContext.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);

            RequestDelegate next = (c) =>
            {
                return Task.FromResult<object>(null);
            };

            var router = new TestRouter(isHandled);
            var middleware = new RouterMiddleware(next, router);

            // Act
            await middleware.Invoke(mockContext.Object);

            // Assert
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouterMiddleware).FullName, scope.LoggerName);
            Assert.Equal("RouterMiddleware.Invoke", scope.Scope);

            Assert.Empty(sink.Writes);
        }

        [Fact]
        public async void Invoke_LogsCorrectValuesWhenHandled()
        {
            // Arrange
            var isHandled = true;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouterMiddleware>,
                TestSink.EnableWithTypeName<RouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
            mockContext.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);

            RequestDelegate next = (c) =>
            {
                return Task.FromResult<object>(null);
            };

            var router = new TestRouter(isHandled);
            var middleware = new RouterMiddleware(next, router);

            // Act
            await middleware.Invoke(mockContext.Object);

            // Assert
            // exists a BeginScope, verify contents
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouterMiddleware).FullName, scope.LoggerName);
            Assert.Equal("RouterMiddleware.Invoke", scope.Scope);

            Assert.Single(sink.Writes);

            var write = sink.Writes[0];
            Assert.Equal(typeof(RouterMiddleware).FullName, write.LoggerName);
            Assert.Equal("RouterMiddleware.Invoke", write.Scope);
            Assert.Equal(typeof(RouterMiddlewareInvokeValues), write.State.GetType());
            var values = (RouterMiddlewareInvokeValues)write.State;
            Assert.Equal("RouterMiddleware.Invoke", values.Name);
            Assert.Equal(true, values.Handled);
        }

        [Fact]
        public async void Invoke_DoesNotLogWhenDisabledAndHandled()
        {
            // Arrange
            var isHandled = true;

            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouterMiddleware>,
                TestSink.EnableWithTypeName<RouterMiddleware>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: false);

            var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
            mockContext.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);

            RequestDelegate next = (c) =>
            {
                return Task.FromResult<object>(null);
            };

            var router = new TestRouter(isHandled);
            var middleware = new RouterMiddleware(next, router);

            // Act
            await middleware.Invoke(mockContext.Object);

            // Assert
            // exists a BeginScope, verify contents
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouterMiddleware).FullName, scope.LoggerName);
            Assert.Equal("RouterMiddleware.Invoke", scope.Scope);

            Assert.Empty(sink.Writes);
        }
#endif

        private class TestRouter : IRouter
        {
            private bool _isHandled;
            
            public TestRouter(bool isHandled)
            {
                _isHandled = isHandled;
            }

            public string GetVirtualPath(VirtualPathContext context)
            {
                return "";
            }

            public Task RouteAsync(RouteContext context)
            {
                context.IsHandled = _isHandled;
                return Task.FromResult<object>(null);
            }
        }
    }
}
