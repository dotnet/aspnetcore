// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Diagnostics;

public class DeveloperExceptionPageMiddlewareTest
{
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

    public static TheoryData CompilationExceptionData
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
