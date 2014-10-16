// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(MvcRouteHandler).FullName, scope.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(MvcRouteHandler).FullName, write.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", write.Scope);
            var values = Assert.IsType<MvcRouteHandlerRouteAsyncValues>(write.State);
            Assert.Equal("MvcRouteHandler.RouteAsync", values.Name);
            Assert.Equal(true, values.ActionSelected);
            Assert.Equal(true, values.ActionInvoked);
            Assert.Equal(true, values.Handled);
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
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(MvcRouteHandler).FullName, scope.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(MvcRouteHandler).FullName, write.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", write.Scope);
            var values = Assert.IsType<MvcRouteHandlerRouteAsyncValues>(write.State);
            Assert.Equal("MvcRouteHandler.RouteAsync", values.Name);
            Assert.Equal(false, values.ActionSelected);
            Assert.Equal(false, values.ActionInvoked);
            Assert.Equal(false, values.Handled);
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
            Assert.Equal(1, sink.Scopes.Count);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(MvcRouteHandler).FullName, scope.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", scope.Scope);

            Assert.Equal(1, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(typeof(MvcRouteHandler).FullName, write.LoggerName);
            Assert.Equal("MvcRouteHandler.RouteAsync", write.Scope);
            var values = Assert.IsType<MvcRouteHandlerRouteAsyncValues>(write.State);
            Assert.Equal("MvcRouteHandler.RouteAsync", values.Name);
            Assert.Equal(true, values.ActionSelected);
            Assert.Equal(false, values.ActionInvoked);
            Assert.Equal(false, values.Handled);
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

        private RouteContext CreateRouteContext(
            IActionSelector actionSelector = null,
            IActionInvokerFactory invokerFactory = null,
            ILoggerFactory loggerFactory = null,
            IOptions<MvcOptions> optionsAccessor = null)
        {
            var mockContextAccessor = new Mock<IContextAccessor<ActionContext>>();
            mockContextAccessor.Setup(c => c.SetContextSource(
                It.IsAny<Func<ActionContext>>(),
                It.IsAny<Func<ActionContext, ActionContext>>()))
                .Returns(NullDisposable.Instance);


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
                var mockOptionsAccessor = new Mock<IOptions<MvcOptions>>();
                mockOptionsAccessor.SetupGet(o => o.Options)
                                   .Returns(new MvcOptions());

                optionsAccessor = mockOptionsAccessor.Object;
            }

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(h => h.RequestServices.GetService(typeof(IContextAccessor<ActionContext>)))
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