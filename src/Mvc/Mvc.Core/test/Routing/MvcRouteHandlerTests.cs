// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Routing;

public class MvcRouteHandlerTests
{
    [Fact]
    public async Task RouteAsync_FailOnNoAction_LogsCorrectValues()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var mockActionSelector = new Mock<IActionSelector>();
        mockActionSelector
            .Setup(a => a.SelectCandidates(It.IsAny<RouteContext>()))
            .Returns(new ActionDescriptor[0]);

        var context = CreateRouteContext();
        context.RouteData.Values.Add("controller", "Home");
        context.RouteData.Values.Add("action", "Index");

        var handler = CreateMvcRouteHandler(
            actionSelector: mockActionSelector.Object,
            loggerFactory: loggerFactory);

        var expectedMessage = "No actions matched the current request. Route values: controller=Home, action=Index";

        // Act
        await handler.RouteAsync(context);

        // Assert
        Assert.Empty(sink.Scopes);
        var write = Assert.Single(sink.Writes);
        Assert.Equal(expectedMessage, write.State?.ToString());
    }

    private MvcRouteHandler CreateMvcRouteHandler(
        ActionDescriptor actionDescriptor = null,
        IActionSelector actionSelector = null,
        IActionInvokerFactory invokerFactory = null,
        ILoggerFactory loggerFactory = null,
        object diagnosticListener = null)
    {
        if (actionDescriptor == null)
        {
            var mockAction = new Mock<ActionDescriptor>();
            actionDescriptor = mockAction.Object;
        }

        if (actionSelector == null)
        {
            var mockActionSelector = new Mock<IActionSelector>();
            mockActionSelector
                .Setup(a => a.SelectCandidates(It.IsAny<RouteContext>()))
                .Returns(new ActionDescriptor[] { actionDescriptor });

            mockActionSelector
                .Setup(a => a.SelectBestCandidate(It.IsAny<RouteContext>(), It.IsAny<IReadOnlyList<ActionDescriptor>>()))
                .Returns(actionDescriptor);
            actionSelector = mockActionSelector.Object;
        }

        if (loggerFactory == null)
        {
            loggerFactory = NullLoggerFactory.Instance;
        }

        var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
        if (diagnosticListener != null)
        {
            diagnosticSource.SubscribeWithAdapter(diagnosticListener);
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

        return new MvcRouteHandler(
            invokerFactory,
            actionSelector,
            diagnosticSource,
            loggerFactory);
    }

    private RouteContext CreateRouteContext()
    {
        var routingFeature = new RoutingFeature();

        var httpContext = new Mock<HttpContext>();
        httpContext
            .Setup(h => h.Features[typeof(IRoutingFeature)])
            .Returns(routingFeature);

        var routeContext = new RouteContext(httpContext.Object);
        routingFeature.RouteData = routeContext.RouteData;
        return routeContext;
    }

    private class RoutingFeature : IRoutingFeature
    {
        public RouteData RouteData { get; set; }
    }
}
