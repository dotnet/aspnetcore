// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Diagnostics;

public class ExceptionHandlerMiddlewareTest
{
    [Fact]
    public async Task Invoke_ExceptionThrownResultsInClearedRouteValuesAndEndpoint()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.SetEndpoint(new Endpoint((_) => Task.CompletedTask, new EndpointMetadataCollection(), "Test"));
        httpContext.Request.RouteValues["John"] = "Doe";

        var optionsAccessor = CreateOptionsAccessor(
            exceptionHandler: context =>
            {
                Assert.Empty(context.Request.RouteValues);
                Assert.Null(context.GetEndpoint());
                return Task.CompletedTask;
            });
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor);

        // Act & Assert
        await middleware.Invoke(httpContext);
    }

    [Fact]
    public async Task Invoke_ExceptionHandlerCaptureRouteValuesAndEndpoint()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var endpoint = new Endpoint((_) => Task.CompletedTask, new EndpointMetadataCollection(), "Test");
        httpContext.SetEndpoint(endpoint);
        httpContext.Request.RouteValues["John"] = "Doe";

        var optionsAccessor = CreateOptionsAccessor(
            exceptionHandler: context =>
            {
                var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                Assert.Equal(endpoint, feature.Endpoint);
                Assert.Equal("Doe", feature.RouteValues["John"]);

                return Task.CompletedTask;
            });
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor);

        // Act & Assert
        await middleware.Invoke(httpContext);
    }

    [Fact]
    public async Task IExceptionHandlers_CallNextIfNotHandled()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var optionsAccessor = CreateOptionsAccessor();

        var exceptionHandlers = new List<IExceptionHandler>
        {
            new TestExceptionHandler(false, "1"),
            new TestExceptionHandler(false, "2"),
            new TestExceptionHandler(true, "3"),
        };

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor, exceptionHandlers);

        // Act & Assert
        await middleware.Invoke(httpContext);

        Assert.True(httpContext.Items.ContainsKey("1"));
        Assert.True(httpContext.Items.ContainsKey("2"));
        Assert.True(httpContext.Items.ContainsKey("3"));
    }

    [Fact]
    public async Task IExceptionHandlers_SkipIfOneHandle()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var optionsAccessor = CreateOptionsAccessor();

        var exceptionHandlers = new List<IExceptionHandler>
        {
            new TestExceptionHandler(false, "1"),
            new TestExceptionHandler(true, "2"),
            new TestExceptionHandler(true, "3"),
        };

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor, exceptionHandlers);

        // Act & Assert
        await middleware.Invoke(httpContext);

        Assert.True(httpContext.Items.ContainsKey("1"));
        Assert.True(httpContext.Items.ContainsKey("2"));
        Assert.False(httpContext.Items.ContainsKey("3"));
    }

    [Fact]
    public async Task IExceptionHandlers_CallOptionExceptionHandlerIfNobodyHandles()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var optionsAccessor = CreateOptionsAccessor(
            (context) =>
            {
                context.Items["ExceptionHandler"] = true;
                return Task.CompletedTask;
            });

        var exceptionHandlers = new List<IExceptionHandler>
        {
            new TestExceptionHandler(false, "1"),
            new TestExceptionHandler(false, "2"),
            new TestExceptionHandler(false, "3"),
        };

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor, exceptionHandlers);

        // Act & Assert
        await middleware.Invoke(httpContext);

        Assert.True(httpContext.Items.ContainsKey("1"));
        Assert.True(httpContext.Items.ContainsKey("2"));
        Assert.True(httpContext.Items.ContainsKey("3"));
        Assert.True(httpContext.Items.ContainsKey("ExceptionHandler"));
    }

    private class TestExceptionHandler : IExceptionHandler
    {
        private readonly bool _handle;
        private readonly string _name;

        public TestExceptionHandler(bool handle, string name)
        {
            _handle = handle;
            _name = name;
        }

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            httpContext.Items[_name] = true;
            return ValueTask.FromResult(_handle);
        }
    }

    private HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new TestServiceProvider()
        };

        return httpContext;
    }

    private IOptions<ExceptionHandlerOptions> CreateOptionsAccessor(
        RequestDelegate exceptionHandler = null,
        string exceptionHandlingPath = null)
    {
        exceptionHandler ??= c => Task.CompletedTask;
        var options = new ExceptionHandlerOptions()
        {
            ExceptionHandler = exceptionHandler,
            ExceptionHandlingPath = exceptionHandlingPath,
        };
        var optionsAccessor = Mock.Of<IOptions<ExceptionHandlerOptions>>(o => o.Value == options);
        return optionsAccessor;
    }

    private ExceptionHandlerMiddlewareImpl CreateMiddleware(
        RequestDelegate next,
        IOptions<ExceptionHandlerOptions> options,
        IEnumerable<IExceptionHandler> exceptionHandlers = null)
    {
        next ??= c => Task.CompletedTask;
        var listener = new DiagnosticListener("Microsoft.AspNetCore");

        var middleware = new ExceptionHandlerMiddlewareImpl(
            next,
            NullLoggerFactory.Instance,
            options,
            listener,
            exceptionHandlers ?? Enumerable.Empty<IExceptionHandler>());

        return middleware;
    }

    private class TestServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}
