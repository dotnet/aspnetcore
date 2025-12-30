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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Diagnostics;

public class DeveloperExceptionPageMiddlewareTest : LoggedTest
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
    public async Task ExceptionHandlerFeatureIsAvailableInCustomizeProblemDetailsWhenUsingExceptionPage()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddProblemDetails(configure =>
                {
                    configure.CustomizeProblemDetails = (context) =>
                    {
                        var feature = context.HttpContext.Features.Get<IExceptionHandlerFeature>();
                        context.ProblemDetails.Extensions.Add("OriginalExceptionMessage", feature?.Error.Message);
                        context.ProblemDetails.Extensions.Add("EndpointDisplayName", feature?.Endpoint?.DisplayName);
                        context.ProblemDetails.Extensions.Add("RouteValue", feature?.RouteValues?["id"]);
                        context.ProblemDetails.Extensions.Add("Path", feature?.Path);
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
                    app.UseRouting();
                    app.UseEndpoints(endpoint =>
                    {
                        endpoint.MapGet("/test/{id}", (int id) =>
                        {
                            throw new Exception("Test exception");
                        });
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test/1");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await server.CreateClient().SendAsync(request);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var originalExceptionMessage = ((JsonElement)body.Extensions["OriginalExceptionMessage"]).GetString();
        var endpointDisplayName = ((JsonElement)body.Extensions["EndpointDisplayName"]).GetString();
        var routeValue = ((JsonElement)body.Extensions["RouteValue"]).GetString();
        var path = ((JsonElement)body.Extensions["Path"]).GetString();
        Assert.Equal("Test exception", originalExceptionMessage);
        Assert.Contains("/test/{id}", endpointDisplayName);
        Assert.Equal("1", routeValue);
        Assert.Equal("/test/1", path);
    }

    [Fact]
    public async Task ExceptionHandlerPathFeatureIsAvailableInCustomizeProblemDetailsWhenUsingExceptionPage()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddProblemDetails(configure =>
                {
                    configure.CustomizeProblemDetails = (context) =>
                    {
                        var feature = context.HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                        context.ProblemDetails.Extensions.Add("OriginalExceptionMessage", feature?.Error.Message);
                        context.ProblemDetails.Extensions.Add("EndpointDisplayName", feature?.Endpoint?.DisplayName);
                        context.ProblemDetails.Extensions.Add("RouteValue", feature?.RouteValues?["id"]);
                        context.ProblemDetails.Extensions.Add("Path", feature?.Path);
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
                    app.UseRouting();
                    app.UseEndpoints(endpoint =>
                    {
                        endpoint.MapGet("/test/{id}", (int id) =>
                        {
                            throw new Exception("Test exception");
                        });
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test/1");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await server.CreateClient().SendAsync(request);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var originalExceptionMessage = ((JsonElement)body.Extensions["OriginalExceptionMessage"]).GetString();
        var endpointDisplayName = ((JsonElement)body.Extensions["EndpointDisplayName"]).GetString();
        var routeValue = ((JsonElement)body.Extensions["RouteValue"]).GetString();
        var path = ((JsonElement)body.Extensions["Path"]).GetString();
        Assert.Equal("Test exception", originalExceptionMessage);
        Assert.Contains("/test/{id}", endpointDisplayName);
        Assert.Equal("1", routeValue);
        Assert.Equal("/test/1", path);
    }

    [Fact]
    public async Task UnhandledErrorsWriteToDiagnosticWhenUsingExceptionPage()
    {
        // Arrange
        DiagnosticListener diagnosticListener = null;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new Exception("Test exception");
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var listener = new TestDiagnosticListener();
        diagnosticListener.SubscribeWithAdapter(listener);

        // Act
        await server.CreateClient().GetAsync("/path");

        // Assert
        Assert.NotNull(listener.DiagnosticUnhandledException?.HttpContext);
        Assert.NotNull(listener.DiagnosticUnhandledException?.Exception);
        Assert.Null(listener.DiagnosticHandledException?.HttpContext);
        Assert.Null(listener.DiagnosticHandledException?.Exception);
    }

    [Fact]
    public async Task ErrorPageWithAcceptHeaderForHtmlReturnsHtml()
    {
        // Arrange
        using var host = new HostBuilder()
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

        // Act
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        var response = await client.GetAsync("/path");

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
        Assert.Contains("<html", responseText);
        Assert.Contains("Test exception", responseText);
    }

    [Fact]
    public async Task ErrorPageWithoutAcceptHeaderForHtmlReturnsPlainText()
    {
        // Arrange
        using var host = new HostBuilder()
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

        // Act
        var response = await server.CreateClient().GetAsync("/path");

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        Assert.Contains("Test exception", responseText);
        Assert.DoesNotContain("<html", responseText);
    }

    [Fact]
    public async Task ErrorPageShowsEndpointMetadata()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.Run(httpContext =>
                    {
                        var endpoint = new Endpoint(null, new EndpointMetadataCollection("my metadata"), null);
                        httpContext.SetEndpoint(endpoint);
                        throw new Exception("Test exception");
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        // Act
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        var response = await client.GetAsync("/path");

        // Assert
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Contains("my metadata", responseText);
    }

    [Fact]
    public async Task StatusCodeFromBadHttpRequestExceptionIsPreserved()
    {
        const int statusCode = 418;

        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new BadHttpRequestException("Not found!", statusCode);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        // Act
        var response = await server.CreateClient().GetAsync("/path");

        // Assert
        Assert.Equal(statusCode, (int)response.StatusCode);

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Contains("Not found!", responseText);
    }

    [Fact]
    public async Task ExceptionPageFiltersAreApplied()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionMessageFilter>();
                })
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

        // Act
        var response = await server.CreateClient().GetAsync("/path");

        // Assert
        Assert.Equal("Test exception", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExceptionFilterCallingNextWorks()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IDeveloperPageExceptionFilter, PassThroughExceptionFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, AlwaysBadFormatExceptionFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionMessageFilter>();
                })
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

        // Act
        var response = await server.CreateClient().GetAsync("/path");

        // Assert
        Assert.Equal("Bad format exception!", await response.Content.ReadAsStringAsync());

        await host.StartAsync();
    }

    [Fact]
    public async Task ExceptionPageFiltersAreAppliedInOrder()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IDeveloperPageExceptionFilter, AlwaysThrowSameMessageFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionMessageFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionToStringFilter>();
                })
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

        // Act
        var response = await server.CreateClient().GetAsync("/path");

        // Assert
        Assert.Equal("An error occurred", await response.Content.ReadAsStringAsync());
    }

    public static TheoryData<List<CompilationFailure>> CompilationExceptionData
    {
        get
        {
            var variations = new TheoryData<List<CompilationFailure>>();
            var failures = new List<CompilationFailure>();
            var diagnosticMessages = new List<DiagnosticMessage>();
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", diagnosticMessages)
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(null, "source file content", "compiled content", diagnosticMessages)
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", null, "compiled content", diagnosticMessages)
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", null, diagnosticMessages)
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(null, null, null, diagnosticMessages)
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", diagnosticMessages),
                    new CompilationFailure(@"c:\sourcefilepath.cs", null, "compiled content", diagnosticMessages)
                });
            variations.Add(null);
            variations.Add(new List<CompilationFailure>()
                {
                    null
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", diagnosticMessages),
                    null
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", null)
                });
            variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", new List<DiagnosticMessage>(){ null })
                });
            return variations;
        }
    }

    [Theory]
    [MemberData(nameof(CompilationExceptionData))]
    public async Task NullInfoInCompilationException_ShouldNotThrowExceptionGeneratingExceptionPage(
        List<CompilationFailure> failures)
    {
        // Arrange
        DiagnosticListener diagnosticListener = null;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new CustomCompilationException(failures);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        var listener = new TestDiagnosticListener();
        diagnosticListener.SubscribeWithAdapter(listener);

        // Act
        await server.CreateClient().GetAsync("/path");

        // Assert
        Assert.NotNull(listener.DiagnosticUnhandledException?.HttpContext);
        Assert.NotNull(listener.DiagnosticUnhandledException?.Exception);
        Assert.Null(listener.DiagnosticHandledException?.HttpContext);
        Assert.Null(listener.DiagnosticHandledException?.Exception);
    }

    [Fact]
    public async Task UnhandledError_ExceptionNameTagAdded()
    {
        // Arrange
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
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
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
            });
        Assert.Collection(requestExceptionCollector.GetMeasurementSnapshot(),
            m => AssertRequestException(m, "System.Exception", "unhandled"));
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

    public class CustomCompilationException : Exception, ICompilationException
    {
        public CustomCompilationException(IEnumerable<CompilationFailure> compilationFailures)
        {
            CompilationFailures = compilationFailures;
        }

        public IEnumerable<CompilationFailure> CompilationFailures { get; }
    }

    public class ExceptionMessageFilter : IDeveloperPageExceptionFilter
    {
        public Task HandleExceptionAsync(ErrorContext context, Func<ErrorContext, Task> next)
        {
            return context.HttpContext.Response.WriteAsync(context.Exception.Message);
        }
    }

    public class ExceptionToStringFilter : IDeveloperPageExceptionFilter
    {
        public Task HandleExceptionAsync(ErrorContext context, Func<ErrorContext, Task> next)
        {
            return context.HttpContext.Response.WriteAsync(context.Exception.ToString());
        }
    }

    public class AlwaysThrowSameMessageFilter : IDeveloperPageExceptionFilter
    {
        public Task HandleExceptionAsync(ErrorContext context, Func<ErrorContext, Task> next)
        {
            return context.HttpContext.Response.WriteAsync("An error occurred");
        }
    }

    public class AlwaysBadFormatExceptionFilter : IDeveloperPageExceptionFilter
    {
        public Task HandleExceptionAsync(ErrorContext context, Func<ErrorContext, Task> next)
        {
            return next(new ErrorContext(context.HttpContext, new FormatException("Bad format exception!")));
        }
    }

    public class PassThroughExceptionFilter : IDeveloperPageExceptionFilter
    {
        public Task HandleExceptionAsync(ErrorContext context, Func<ErrorContext, Task> next)
        {
            return next(context);
        }
    }
}
