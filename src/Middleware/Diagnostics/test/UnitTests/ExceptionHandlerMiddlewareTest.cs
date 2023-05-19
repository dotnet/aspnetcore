// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Diagnostics;

public class ExceptionHandlerMiddlewareTest
{
    [Fact]
    public async Task ExceptionIsSetOnProblemDetailsContext()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddProblemDetails(configure =>
                {
                    configure.CustomizeProblemDetails = (context) =>
                    {
                        if (context.Exception is not null)
                        {
                            context.ProblemDetails.Extensions.Add("OriginalExceptionMessage", context.Exception.Message);
                        }
                    };
                });
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new Exception("Test exception");
                    });

                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var request = new HttpRequestMessage(HttpMethod.Get, "/path");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await server.CreateClient().SendAsync(request);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var originalExceptionMessage = ((JsonElement)body.Extensions["OriginalExceptionMessage"]).GetString();
        Assert.Equal("Test exception", originalExceptionMessage);
    }

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
