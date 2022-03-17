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

    private ExceptionHandlerMiddleware CreateMiddleware(
        RequestDelegate next,
        IOptions<ExceptionHandlerOptions> options)
    {
        next ??= c => Task.CompletedTask;
        var listener = new DiagnosticListener("Microsoft.AspNetCore");

        var middleware = new ExceptionHandlerMiddleware(
            next,
            NullLoggerFactory.Instance,
            options,
            listener);

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
