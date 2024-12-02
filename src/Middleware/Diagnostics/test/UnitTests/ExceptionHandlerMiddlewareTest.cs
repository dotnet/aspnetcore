// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Diagnostics;

public class ExceptionHandlerMiddlewareTest : LoggedTest
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
                    app.UseExceptionHandler();
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

    [Fact]
    public async Task Metrics_NoExceptionThrown()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var optionsAccessor = CreateOptionsAccessor();
        var meterFactory = new TestMeterFactory();
        var exceptionHandlers = new List<IExceptionHandler> { new TestExceptionHandler(true, "1") };
        var middleware = CreateMiddleware(_ => Task.CompletedTask, optionsAccessor, exceptionHandlers, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var diagnosticsRequestExceptionCollector = new MetricCollector<long>(meterFactory, DiagnosticsMetrics.MeterName, "aspnetcore.diagnostics.exceptions");

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        Assert.Equal(DiagnosticsMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        Assert.Empty(diagnosticsRequestExceptionCollector.GetMeasurementSnapshot());
    }

    [Fact]
    public async Task Metrics_ExceptionThrown_Handled_Reported()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var optionsAccessor = CreateOptionsAccessor();
        var meterFactory = new TestMeterFactory();
        var exceptionHandlers = new List<IExceptionHandler> { new TestExceptionHandler(true, "1") };
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor, exceptionHandlers, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var diagnosticsRequestExceptionCollector = new MetricCollector<long>(meterFactory, DiagnosticsMetrics.MeterName, "aspnetcore.diagnostics.exceptions");

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        Assert.Collection(diagnosticsRequestExceptionCollector.GetMeasurementSnapshot(),
            m => AssertRequestException(m, "System.InvalidOperationException", "handled", typeof(TestExceptionHandler).FullName));
    }

    [Fact]
    public async Task Metrics_ExceptionThrown_ResponseStarted_Skipped()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        var optionsAccessor = CreateOptionsAccessor();
        var meterFactory = new TestMeterFactory();
        var exceptionHandlers = new List<IExceptionHandler> { new TestExceptionHandler(true, "1") };
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor, exceptionHandlers, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var diagnosticsRequestExceptionCollector = new MetricCollector<long>(meterFactory, DiagnosticsMetrics.MeterName, "aspnetcore.diagnostics.exceptions");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(httpContext));

        // Assert
        Assert.Collection(diagnosticsRequestExceptionCollector.GetMeasurementSnapshot(),
            m => AssertRequestException(m, "System.InvalidOperationException", "skipped"));
    }

    private sealed class TestResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    [Fact]
    public async Task Metrics_ExceptionThrown_DefaultSettings_Handled_Reported()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var optionsAccessor = CreateOptionsAccessor();
        var meterFactory = new TestMeterFactory();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor, meterFactory: meterFactory);
        var meter = meterFactory.Meters.Single();

        using var diagnosticsRequestExceptionCollector = new MetricCollector<long>(meterFactory, DiagnosticsMetrics.MeterName, "aspnetcore.diagnostics.exceptions");

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        Assert.Collection(diagnosticsRequestExceptionCollector.GetMeasurementSnapshot(),
            m => AssertRequestException(m, "System.InvalidOperationException", "handled", null));
    }

    [Fact]
    public async Task Metrics_ExceptionThrown_Handled_UseOriginalRoute()
    {
        // Arrange
        var originalEndpointBuilder = new RouteEndpointBuilder(c => Task.CompletedTask, RoutePatternFactory.Parse("/path"), 0);
        var originalEndpoint = originalEndpointBuilder.Build();

        var meterFactory = new TestMeterFactory();
        using var requestDurationCollector = new MetricCollector<double>(meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");
        using var requestExceptionCollector = new MetricCollector<long>(meterFactory, DiagnosticsMetrics.MeterName, "aspnetcore.diagnostics.exceptions");

        using var host = new HostBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<IMeterFactory>(meterFactory);
                s.AddSingleton(LoggerFactory);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseExceptionHandler(new ExceptionHandlerOptions
                    {
                        ExceptionHandler = (c) => Task.CompletedTask
                    });
                    app.Run(context =>
                    {
                        context.SetEndpoint(originalEndpoint);
                        throw new Exception("Test exception");
                    });

                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        // Act
        var response = await server.CreateClient().GetAsync("/path");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        await requestDurationCollector.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

        // Assert
        Assert.Collection(
            requestDurationCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.True(m.Value > 0);
                Assert.Equal(500, (int)m.Tags["http.response.status_code"]);
                Assert.Equal("System.Exception", (string)m.Tags["error.type"]);
                Assert.Equal("/path", (string)m.Tags["http.route"]);
            });
        Assert.Collection(requestExceptionCollector.GetMeasurementSnapshot(),
            m => AssertRequestException(m, "System.Exception", "handled"));
    }

    [Fact]
    public async Task Metrics_ExceptionThrown_Handled_UseNewRoute()
    {
        // Arrange
        var originalEndpointBuilder = new RouteEndpointBuilder(c => Task.CompletedTask, RoutePatternFactory.Parse("/path"), 0);
        var originalEndpoint = originalEndpointBuilder.Build();

        var newEndpointBuilder = new RouteEndpointBuilder(c => Task.CompletedTask, RoutePatternFactory.Parse("/new"), 0);
        var newEndpoint = newEndpointBuilder.Build();

        var meterFactory = new TestMeterFactory();
        using var requestDurationCollector = new MetricCollector<double>(meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");
        using var requestExceptionCollector = new MetricCollector<long>(meterFactory, DiagnosticsMetrics.MeterName, "aspnetcore.diagnostics.exceptions");

        using var host = new HostBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<IMeterFactory>(meterFactory);
                s.AddSingleton(LoggerFactory);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseExceptionHandler(new ExceptionHandlerOptions
                    {
                        ExceptionHandler = (c) =>
                        {
                            c.SetEndpoint(newEndpoint);
                            return Task.CompletedTask;
                        }
                    });
                    app.Run(context =>
                    {
                        context.SetEndpoint(originalEndpoint);
                        throw new Exception("Test exception");
                    });

                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        // Act
        var response = await server.CreateClient().GetAsync("/path");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        await requestDurationCollector.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

        // Assert
        Assert.Collection(
            requestDurationCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.True(m.Value > 0);
                Assert.Equal(500, (int)m.Tags["http.response.status_code"]);
                Assert.Equal("System.Exception", (string)m.Tags["error.type"]);
                Assert.Equal("/new", (string)m.Tags["http.route"]);
            });
        Assert.Collection(requestExceptionCollector.GetMeasurementSnapshot(),
            m => AssertRequestException(m, "System.Exception", "handled"));
    }

    [Fact]
    public async Task Metrics_ExceptionThrown_Unhandled_Reported()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var optionsAccessor = CreateOptionsAccessor(exceptionHandler: c =>
        {
            c.Response.StatusCode = 404;
            return Task.CompletedTask;
        });
        var meterFactory = new TestMeterFactory();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(), optionsAccessor, meterFactory: meterFactory);
        var meter = meterFactory.Meters.Single();

        using var diagnosticsRequestExceptionCollector = new MetricCollector<long>(meterFactory, DiagnosticsMetrics.MeterName, "aspnetcore.diagnostics.exceptions");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(httpContext));

        // Assert
        Assert.Collection(diagnosticsRequestExceptionCollector.GetMeasurementSnapshot(),
            m => AssertRequestException(m, "System.InvalidOperationException", "unhandled"));
    }

    private static void AssertRequestException(CollectedMeasurement<long> measurement, string exceptionName, string result, string handler = null)
    {
        Assert.Equal(1, measurement.Value);
        Assert.Equal(exceptionName, (string)measurement.Tags["error.type"]);
        Assert.Equal(result, measurement.Tags["aspnetcore.diagnostics.exception.result"].ToString());
        if (handler == null)
        {
            Assert.False(measurement.Tags.ContainsKey("aspnetcore.diagnostics.handler.type"));
        }
        else
        {
            Assert.Equal(handler, (string)measurement.Tags["aspnetcore.diagnostics.handler.type"]);
        }
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
        IEnumerable<IExceptionHandler> exceptionHandlers = null,
        IMeterFactory meterFactory = null)
    {
        next ??= c => Task.CompletedTask;
        var listener = new DiagnosticListener("Microsoft.AspNetCore");

        var middleware = new ExceptionHandlerMiddlewareImpl(
            next,
            NullLoggerFactory.Instance,
            options,
            listener,
            exceptionHandlers ?? Enumerable.Empty<IExceptionHandler>(),
            meterFactory ?? new TestMeterFactory());

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
