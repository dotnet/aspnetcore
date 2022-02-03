// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Routing;

public class RouterMiddlewareTest
{
    [Fact]
    public async Task RoutingFeatureSetInIRouter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        httpContext.Request.Path = "/foo/10";

        var routeHandlerExecuted = false;

        var handler = new RouteHandler(context =>
        {
            routeHandlerExecuted = true;

            var routingFeature = context.Features.Get<IRoutingFeature>();

            Assert.NotNull(routingFeature);
            Assert.NotNull(context.Features.Get<IRouteValuesFeature>());

            Assert.Single(routingFeature.RouteData.Values);
            Assert.Single(context.Request.RouteValues);
            Assert.True(routingFeature.RouteData.Values.ContainsKey("id"));
            Assert.True(context.Request.RouteValues.ContainsKey("id"));
            Assert.Equal("10", routingFeature.RouteData.Values["id"]);
            Assert.Equal("10", context.Request.RouteValues["id"]);
            Assert.Equal("10", context.GetRouteValue("id"));
            Assert.Same(routingFeature.RouteData, context.GetRouteData());

            return Task.CompletedTask;
        });

        var route = new Route(handler, "/foo/{id}", Mock.Of<IInlineConstraintResolver>());

        var middleware = new RouterMiddleware(context => Task.CompletedTask, NullLoggerFactory.Instance, route);

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        Assert.True(routeHandlerExecuted);

    }

    [Fact]
    public async Task Invoke_LogsCorrectValues_WhenNotHandled()
    {
        // Arrange
        var expectedMessage = "Request did not match any routes";
        var isHandled = false;

        var sink = new TestSink(
            TestSink.EnableWithTypeName<RouterMiddleware>,
            TestSink.EnableWithTypeName<RouterMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceProvider();

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
        var write = Assert.Single(sink.Writes);
        Assert.Equal(expectedMessage, write.State?.ToString());
    }

    [Fact]
    public async Task Invoke_DoesNotLog_WhenHandled()
    {
        // Arrange
        var isHandled = true;

        var sink = new TestSink(
            TestSink.EnableWithTypeName<RouterMiddleware>,
            TestSink.EnableWithTypeName<RouterMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceProvider();

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
        private readonly bool _isHandled;

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
            context.Handler = _isHandled ? (RequestDelegate)((c) => Task.CompletedTask) : null;
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
