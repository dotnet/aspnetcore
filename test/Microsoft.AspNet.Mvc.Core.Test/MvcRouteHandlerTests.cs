// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class MvcRouteHandlerTests
    {
        [Fact]
        public async Task RouteAsync_Success_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var context = CreateRouteContext(loggerFactory: loggerFactory);

            var handler = new MvcRouteHandler();

            // Act
            await handler.RouteAsync(context);

            // Assert
            var scope = Assert.Single(sink.Scopes);
            Assert.Equal(typeof(MvcRouteHandler).FullName, scope.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", scope.Scope);

            var write = Assert.Single(sink.Writes);
            Assert.Equal(typeof(MvcRouteHandler).FullName, write.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", write.Scope);
            var values = Assert.IsType<MvcRouteHandlerRouteAsyncValues>(write.State);
            Assert.Equal("MvcRouteHandler.RouteAsync", values.Name);
            Assert.True(values.ActionSelected);
            Assert.True(values.ActionInvoked);
            Assert.True(values.Handled);
        }

        [Fact]
        public async Task RouteAsync_FailOnNoAction_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var mockActionSelector = new Mock<IActionSelector>();
            mockActionSelector.Setup(a => a.SelectAsync(It.IsAny<RouteContext>()))
                .Returns(Task.FromResult<ActionDescriptor>(null));

            var context = CreateRouteContext(
                actionSelector: mockActionSelector.Object,
                loggerFactory: loggerFactory);

            var handler = new MvcRouteHandler();

            // Act
            await handler.RouteAsync(context);

            // Assert
            var scope = Assert.Single(sink.Scopes);
            Assert.Equal(typeof(MvcRouteHandler).FullName, scope.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", scope.Scope);

            var write = Assert.Single(sink.Writes);
            Assert.Equal(typeof(MvcRouteHandler).FullName, write.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", write.Scope);
            var values = Assert.IsType<MvcRouteHandlerRouteAsyncValues>(write.State);
            Assert.Equal("MvcRouteHandler.RouteAsync", values.Name);
            Assert.False(values.ActionSelected);
            Assert.False(values.ActionInvoked);
            Assert.False(values.Handled);
        }

        [Fact]
        public async Task RouteAsync_FailOnNoInvoker_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink);

            var mockInvokerFactory = new Mock<IActionInvokerFactory>();
            mockInvokerFactory.Setup(f => f.CreateInvoker(It.IsAny<ActionContext>()))
                .Returns<IActionInvoker>(null);

            var context = CreateRouteContext(
                invokerFactory: mockInvokerFactory.Object,
                loggerFactory: loggerFactory);

            var handler = new MvcRouteHandler();

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await handler.RouteAsync(context));

            // Assert
            var scope = Assert.Single(sink.Scopes);
            Assert.Equal(typeof(MvcRouteHandler).FullName, scope.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(MvcRouteHandler).FullName, write.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", write.Scope);
            var values = Assert.IsType<MvcRouteHandlerRouteAsyncValues>(write.State);
            Assert.Equal("MvcRouteHandler.RouteAsync", values.Name);
            Assert.True(values.ActionSelected);
            Assert.False(values.ActionInvoked);
            Assert.False(values.Handled);
        }

        [Fact]
        public async Task RouteAsync_SetsMaxErrorCountOnModelStateDictionary()
        {
            // Arrange
            var expected = 199;
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            var options = new MvcOptions
            {
                MaxModelValidationErrors = expected
            };
            optionsAccessor.SetupGet(o => o.Options)
                           .Returns(options);

            var invoked = false;
            var mockInvokerFactory = new Mock<IActionInvokerFactory>();
            mockInvokerFactory.Setup(f => f.CreateInvoker(It.IsAny<ActionContext>()))
                              .Callback<ActionContext>(c =>
                              {
                                  Assert.Equal(expected, c.ModelState.MaxAllowedErrors);
                                  invoked = true;
                              })
                              .Returns(Mock.Of<IActionInvoker>());

            var context = CreateRouteContext(
                invokerFactory: mockInvokerFactory.Object,
                optionsAccessor: optionsAccessor.Object);

            var handler = new MvcRouteHandler();

            // Act
            await handler.RouteAsync(context);

            // Assert
            Assert.True(invoked);
        }

        [Fact]
        public async Task RouteAsync_CreatesNewRouteData()
        {
            // Arrange
            RouteData actionRouteData = null;
            var invoker = new Mock<IActionInvoker>();
            invoker
                .Setup(i => i.InvokeAsync())
                .Returns(Task.FromResult(true));

            var invokerFactory = new Mock<IActionInvokerFactory>();
            invokerFactory
                .Setup(f => f.CreateInvoker(It.IsAny<ActionContext>()))
                .Returns<ActionContext>((c) =>
                {
                    actionRouteData = c.RouteData;
                    return invoker.Object;
                });

            var initialRouter = Mock.Of<IRouter>();

            var context = CreateRouteContext(invokerFactory: invokerFactory.Object);
            var handler = new MvcRouteHandler();

            var originalRouteData = context.RouteData;
            originalRouteData.Routers.Add(initialRouter);
            originalRouteData.Values.Add("action", "Index");

            // Act
            await handler.RouteAsync(context);

            // Assert
            Assert.NotSame(originalRouteData, context.RouteData);
            Assert.NotSame(originalRouteData, actionRouteData);
            Assert.Same(actionRouteData, context.RouteData);

            // The new routedata is a copy
            Assert.Equal("Index", context.RouteData.Values["action"]);

            Assert.Equal(initialRouter, Assert.Single(context.RouteData.Routers));
        }

        [Fact]
        public async Task RouteAsync_ResetsRouteDataOnException()
        {
            // Arrange
            RouteData actionRouteData = null;
            var invoker = new Mock<IActionInvoker>();
            invoker
                .Setup(i => i.InvokeAsync())
                .Throws(new Exception());

            var invokerFactory = new Mock<IActionInvokerFactory>();
            invokerFactory
                .Setup(f => f.CreateInvoker(It.IsAny<ActionContext>()))
                .Returns<ActionContext>((c) =>
                {
                    actionRouteData = c.RouteData;
                    c.RouteData.Values.Add("action", "Index");
                    return invoker.Object;
                });

            var context = CreateRouteContext(invokerFactory: invokerFactory.Object);
            var handler = new MvcRouteHandler();

            var initialRouter = Mock.Of<IRouter>();

            var originalRouteData = context.RouteData;
            originalRouteData.Routers.Add(initialRouter);

            // Act
            await Assert.ThrowsAsync<Exception>(() => handler.RouteAsync(context));

            // Assert
            Assert.Same(originalRouteData, context.RouteData);
            Assert.NotSame(originalRouteData, actionRouteData);
            Assert.NotSame(actionRouteData, context.RouteData);

            // The new routedata is a copy
            Assert.Null(context.RouteData.Values["action"]);
            Assert.Equal("Index", actionRouteData.Values["action"]);

            Assert.Equal(initialRouter, Assert.Single(actionRouteData.Routers));
        }

        private RouteContext CreateRouteContext(
            IActionSelector actionSelector = null,
            IActionInvokerFactory invokerFactory = null,
            ILoggerFactory loggerFactory = null,
            IOptions<MvcOptions> optionsAccessor = null)
        {
            var mockContextAccessor = new Mock<IScopedInstance<ActionContext>>();

            if (actionSelector == null)
            {
                var mockAction = new Mock<ActionDescriptor>();

                var mockActionSelector = new Mock<IActionSelector>();
                mockActionSelector.Setup(a => a.SelectAsync(It.IsAny<RouteContext>()))
                    .Returns(Task.FromResult(mockAction.Object));

                actionSelector = mockActionSelector.Object;
            }

            if (invokerFactory == null)
            {
                var mockInvoker = new Mock<IActionInvoker>();
                mockInvoker.Setup(i => i.InvokeAsync())
                    .Returns(Task.FromResult(true));

                var mockInvokerFactory = new Mock<IActionInvokerFactory>();
                mockInvokerFactory.Setup(f => f.CreateInvoker(It.IsAny<ActionContext>()))
                    .Returns(mockInvoker.Object);

                invokerFactory = mockInvokerFactory.Object;
            }

            if (loggerFactory == null)
            {
                loggerFactory = NullLoggerFactory.Instance;
            }

            if (optionsAccessor == null)
            {
                var options = new Mock<IOptions<MvcOptions>>();
                options.SetupGet(o => o.Options)
                                   .Returns(new MvcOptions());

                optionsAccessor = options.Object;
            }

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(h => h.RequestServices.GetService(typeof(IScopedInstance<ActionContext>)))
                .Returns(mockContextAccessor.Object);
            httpContext.Setup(h => h.RequestServices.GetService(typeof(IActionSelector)))
                .Returns(actionSelector);
            httpContext.Setup(h => h.RequestServices.GetService(typeof(IActionInvokerFactory)))
                .Returns(invokerFactory);
            httpContext.Setup(h => h.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);
            httpContext.Setup(h => h.RequestServices.GetService(typeof(MvcMarkerService)))
                 .Returns(new MvcMarkerService());
            httpContext.Setup(h => h.RequestServices.GetService(typeof(IOptions<MvcOptions>)))
                 .Returns(optionsAccessor);

            return new RouteContext(httpContext.Object);
        }
    }
}