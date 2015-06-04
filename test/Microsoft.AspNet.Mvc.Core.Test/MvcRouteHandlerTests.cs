// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.TestCommon.Notification;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Testing;
using Microsoft.Framework.Notification;
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
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var displayName = "A.B.C";
            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.SetupGet(ad => ad.DisplayName)
                            .Returns(displayName);
            var context = CreateRouteContext(actionDescriptor: actionDescriptor.Object, loggerFactory: loggerFactory);
            var handler = new MvcRouteHandler();
            var expectedMessage = $"Executing action {displayName}";

            // Act
            await handler.RouteAsync(context);

            // Assert
            Assert.Single(sink.Scopes);
            Assert.StartsWith("ActionId: ", sink.Scopes[0].Scope?.ToString());
            Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, sink.Writes[0].State?.ToString());
        }

        [Fact]
        public async Task RouteAsync_FailOnNoAction_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var mockActionSelector = new Mock<IActionSelector>();
            mockActionSelector.Setup(a => a.SelectAsync(It.IsAny<RouteContext>()))
                .Returns(Task.FromResult<ActionDescriptor>(null));

            var context = CreateRouteContext(
                actionSelector: mockActionSelector.Object,
                loggerFactory: loggerFactory);

            var handler = new MvcRouteHandler();
            var expectedMessage = "No actions matched the current request.";

            // Act
            await handler.RouteAsync(context);

            // Assert
            Assert.Empty(sink.Scopes);
            Assert.Single(sink.Writes);
            Assert.Equal(expectedMessage, sink.Writes[0].State?.ToString());
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

        [Fact]
        public async Task RouteAsync_Notifies_ActionSelected()
        {
            // Arrange
            var listener = new TestNotificationListener();

            var context = CreateRouteContext(notificationListener: listener);
            context.RouteData.Values.Add("tag", "value");

            var handler = new MvcRouteHandler();

            // Act
            await handler.RouteAsync(context);

            // Assert
            Assert.NotNull(listener?.ActionSelected.ActionDescriptor);
            Assert.NotNull(listener?.ActionSelected.HttpContext);

            var routeValues = listener?.ActionSelected?.RouteData?.Values;
            Assert.NotNull(routeValues);

            Assert.Equal(1, routeValues.Count);
            Assert.Contains(routeValues, kvp => kvp.Key == "tag" && string.Equals(kvp.Value, "value"));
        }

        private RouteContext CreateRouteContext(
            ActionDescriptor actionDescriptor = null,
            IActionSelector actionSelector = null,
            IActionInvokerFactory invokerFactory = null,
            ILoggerFactory loggerFactory = null,
            IOptions<MvcOptions> optionsAccessor = null,
            object notificationListener = null)
        {
            var mockContextAccessor = new Mock<IScopedInstance<ActionContext>>();

            if (actionDescriptor == null)
            {
                var mockAction = new Mock<ActionDescriptor>();
                actionDescriptor = mockAction.Object;
            }

            if (actionSelector == null)
            {
                var mockActionSelector = new Mock<IActionSelector>();
                mockActionSelector.Setup(a => a.SelectAsync(It.IsAny<RouteContext>()))
                    .Returns(Task.FromResult(actionDescriptor));

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
                optionsAccessor = new MockMvcOptionsAccessor();
            }

            var notifier = new Notifier(new NotifierMethodAdapter());
            if (notificationListener != null)
            {
                notifier.EnlistTarget(notificationListener);
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
            httpContext.Setup(h => h.RequestServices.GetService(typeof(INotifier)))
                .Returns(notifier);

            return new RouteContext(httpContext.Object);
        }
    }
}