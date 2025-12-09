// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using System.Net.Http;

namespace Microsoft.AspNetCore.Diagnostics;

public class ExceptionHandlerTest
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task OnlyHandles_UnhandledExceptions(HttpStatusCode expectedStatusCode)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseExceptionHandler("/handle-errors");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            await httpContext.Response.WriteAsync("Handled error in a custom way.");
                        });
                    });

                    app.Run((RequestDelegate)(async (context) =>
                    {
                        context.Response.StatusCode = (int)expectedStatusCode;
                        context.Response.ContentType = "text/plain; charset=utf-8";
                        await context.Response.WriteAsync("An error occurred while adding a product");
                    }));
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal("An error occurred while adding a product", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task DoesNotHandle_UnhandledExceptions_WhenResponseAlreadyStarted()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        Exception exception = null;
                        try
                        {
                            await next(httpContext);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exception = ex;
                        }

                        Assert.NotNull(exception);
                        Assert.Equal("Something bad happened", exception.Message);
                    });

                    app.UseExceptionHandler("/handle-errors");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            await httpContext.Response.WriteAsync("Handled error in a custom way.");
                        });
                    });

                    app.Run(async (httpContext) =>
                    {
                        await httpContext.Response.WriteAsync("Hello");
                        throw new InvalidOperationException("Something bad happened");
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task ClearsResponseBuffer_BeforeRequestIsReexecuted()
    {
        var expectedResponseBody = "New response body";
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    // add response buffering
                    app.Use(async (httpContext, next) =>
                    {
                        var response = httpContext.Response;
                        var originalResponseBody = response.Body;
                        var bufferingStream = new MemoryStream();
                        response.Body = bufferingStream;

                        try
                        {
                            await next(httpContext);
                        }
                        finally
                        {
                            response.Body = originalResponseBody;
                        }

                        bufferingStream.Seek(0, SeekOrigin.Begin);
                        await bufferingStream.CopyToAsync(response.Body);
                    });

                    app.UseExceptionHandler("/handle-errors");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            Assert.True(httpContext.Response.Body.CanSeek);
                            Assert.Equal(0, httpContext.Response.Body.Position);

                            await httpContext.Response.WriteAsync(expectedResponseBody);
                        });
                    });

                    app.Run(async (context) =>
                    {
                        // Write some content into the response before throwing exception
                        await context.Response.WriteAsync(new string('a', 100));

                        throw new InvalidOperationException("Invalid input provided.");
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(expectedResponseBody, await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.True(response.Headers.CacheControl.NoCache);
            Assert.True(response.Headers.CacheControl.NoStore);
            Assert.True(response.Headers.TryGetValues("Pragma", out values));
            Assert.Single(values);
            Assert.Equal("no-cache", values.First());
            Assert.True(response.Content.Headers.TryGetValues("Expires", out values));
            Assert.Single(values);
            Assert.Equal("-1", values.First());
            Assert.False(response.Headers.TryGetValues("ETag", out values));
        }
    }

    [Fact]
    public async Task ClearsCacheHeaders_SetByReexecutionPathHandlers()
    {
        var expiresTime = DateTime.UtcNow.AddDays(5).ToString("R");
        var expectedResponseBody = "Handled error in a custom way.";
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseExceptionHandler("/handle-errors");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            httpContext.Response.Headers.Add("Cache-Control", new[] { "max-age=600" });
                            httpContext.Response.Headers.Add("Pragma", new[] { "max-age=600" });
                            httpContext.Response.Headers.Add(
                                "Expires", new[] { expiresTime });
                            httpContext.Response.Headers.Add("ETag", new[] { "12345" });

                            await httpContext.Response.WriteAsync(expectedResponseBody);
                        });
                    });

                    app.Run((context) =>
                    {
                        throw new InvalidOperationException("Invalid input provided.");
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(expectedResponseBody, await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.True(response.Headers.CacheControl.NoCache);
            Assert.True(response.Headers.CacheControl.NoStore);
            Assert.True(response.Headers.TryGetValues("Pragma", out values));
            Assert.Single(values);
            Assert.Equal("no-cache", values.First());
            Assert.True(response.Content.Headers.TryGetValues("Expires", out values));
            Assert.Single(values);
            Assert.Equal("-1", values.First());
            Assert.False(response.Headers.TryGetValues("ETag", out values));
        }
    }

    [Fact]
    public async Task DoesNotModifyCacheHeaders_WhenNoExceptionIsThrown()
    {
        var expiresTime = DateTime.UtcNow.AddDays(10).ToString("R");
        var expectedResponseBody = "Hello world!";
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseExceptionHandler("/handle-errors");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            await httpContext.Response.WriteAsync("Handled error in a custom way.");
                        });
                    });

                    app.Run(async (httpContext) =>
                    {
                        httpContext.Response.Headers.Add("Cache-Control", new[] { "max-age=3600" });
                        httpContext.Response.Headers.Add("Pragma", new[] { "max-age=3600" });
                        httpContext.Response.Headers.Add("Expires", new[] { expiresTime });
                        httpContext.Response.Headers.Add("ETag", new[] { "abcdef" });

                        await httpContext.Response.WriteAsync(expectedResponseBody);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.Equal(expectedResponseBody, await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.True(response.Headers.TryGetValues("Cache-Control", out values));
            Assert.Single(values);
            Assert.Equal("max-age=3600", values.First());
            Assert.True(response.Headers.TryGetValues("Pragma", out values));
            Assert.Single(values);
            Assert.Equal("max-age=3600", values.First());
            Assert.True(response.Content.Headers.TryGetValues("Expires", out values));
            Assert.Single(values);
            Assert.Equal(expiresTime, values.First());
            Assert.True(response.Headers.TryGetValues("ETag", out values));
            Assert.Single(values);
            Assert.Equal("abcdef", values.First());
        }
    }

    [Fact]
    public async Task ExceptionHandlerSucceeded_IfExceptionHandlerResponseHasStarted()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        Exception exception = null;
                        try
                        {
                            await next(httpContext);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exception = ex;
                        }

                        Assert.Null(exception);
                    });

                    app.UseExceptionHandler("/handle-errors");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                            await httpContext.Response.WriteAsync("Custom 404");
                        });
                    });

                    app.Run(httpContext =>
                    {
                        httpContext.Response.Headers.Add("Cache-Control", new[] { "max-age=3600" });
                        httpContext.Response.Headers.Add("Pragma", new[] { "max-age=3600" });
                        httpContext.Response.Headers.Add("Expires", new[] { DateTime.UtcNow.AddDays(10).ToString("R") });
                        httpContext.Response.Headers.Add("ETag", new[] { "abcdef" });

                        throw new InvalidOperationException("Something bad happened");
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Custom 404", await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.True(response.Headers.CacheControl.NoCache);
            Assert.True(response.Headers.CacheControl.NoStore);
            Assert.True(response.Headers.TryGetValues("Pragma", out values));
            Assert.Single(values);
            Assert.Equal("no-cache", values.First());
            Assert.False(response.Headers.TryGetValues("Expires", out _));
            Assert.False(response.Headers.TryGetValues("ETag", out _));
        }
    }

    [Fact]
    public async Task DoesNotClearCacheHeaders_WhenResponseHasAlreadyStarted()
    {
        var expiresTime = DateTime.UtcNow.AddDays(10).ToString("R");
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        Exception exception = null;
                        try
                        {
                            await next(httpContext);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exception = ex;
                        }

                        Assert.NotNull(exception);
                        Assert.Equal("Something bad happened", exception.Message);
                    });

                    app.UseExceptionHandler("/handle-errors");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            await httpContext.Response.WriteAsync("Handled error in a custom way.");
                        });
                    });

                    app.Run(async (httpContext) =>
                    {
                        httpContext.Response.Headers.Add("Cache-Control", new[] { "max-age=3600" });
                        httpContext.Response.Headers.Add("Pragma", new[] { "max-age=3600" });
                        httpContext.Response.Headers.Add("Expires", new[] { expiresTime });
                        httpContext.Response.Headers.Add("ETag", new[] { "abcdef" });

                        await httpContext.Response.WriteAsync("Hello");

                        throw new InvalidOperationException("Something bad happened");
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello", await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.True(response.Headers.TryGetValues("Cache-Control", out values));
            Assert.Single(values);
            Assert.Equal("max-age=3600", values.First());
            Assert.True(response.Headers.TryGetValues("Pragma", out values));
            Assert.Single(values);
            Assert.Equal("max-age=3600", values.First());
            Assert.True(response.Content.Headers.TryGetValues("Expires", out values));
            Assert.Single(values);
            Assert.Equal(expiresTime, values.First());
            Assert.True(response.Headers.TryGetValues("ETag", out values));
            Assert.Single(values);
            Assert.Equal("abcdef", values.First());
        }
    }

    [Fact]
    public async Task HandledErrorsWriteToDiagnosticWhenUsingExceptionHandler()
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

                    app.UseExceptionHandler("/handle-errors");
                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            await httpContext.Response.WriteAsync("Handled error in a custom way.");
                        });
                    });
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
        await server.CreateClient().GetAsync(string.Empty);

        // This ensures that all diagnostics are completely written to the diagnostic listener
        Thread.Sleep(1000);

        // Assert
        Assert.NotNull(listener.EndRequest?.HttpContext);
        Assert.Null(listener.HostingUnhandledException?.HttpContext);
        Assert.Null(listener.HostingUnhandledException?.Exception);
        Assert.Null(listener.DiagnosticUnhandledException?.HttpContext);
        Assert.Null(listener.DiagnosticUnhandledException?.Exception);
        Assert.NotNull(listener.DiagnosticHandledException?.HttpContext);
        Assert.NotNull(listener.DiagnosticHandledException?.Exception);
    }

    [Fact]
    public void UsingExceptionHandler_ThrowsAnException_WhenExceptionHandlingPathNotSet()
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
                    app.UseExceptionHandler();
                });
            }).Build();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => host.Start());

        // Assert
        Assert.Equal("An error occurred when configuring the exception handler middleware. " +
            "Either the 'ExceptionHandlingPath' or the 'ExceptionHandler' property must be set in 'UseExceptionHandler()'. " +
            "Alternatively, set one of the aforementioned properties in 'Startup.ConfigureServices' as follows: 'services.AddExceptionHandler(options => { ... });' " +
            "or configure to generate a 'ProblemDetails' response in 'service.AddProblemDetails()'.",
            exception.Message);
    }

    [Fact]
    public async Task ExceptionHandlerNotFound_ThrowsIOEWithOriginalError()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.Use(async (httpContext, next) =>
                    {
                        Exception exception = null;
                        try
                        {
                            await next(httpContext);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exception = ex;

                            // This mimics what the server would do when an exception occurs
                            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        }

                        // Invalid operation exception
                        Assert.NotNull(exception);
                        Assert.Equal("The exception handler configured on ExceptionHandlerOptions produced a 404 status response. " +
            "This InvalidOperationException containing the original exception was thrown since this is often due to a misconfigured ExceptionHandlingPath. " +
            "If the exception handler is expected to return 404 status responses then set AllowStatusCode404Response to true.", exception.Message);

                        // The original exception is inner exception
                        Assert.NotNull(exception.InnerException);
                        Assert.IsType<ApplicationException>(exception.InnerException);
                        Assert.Equal("Something bad happened.", exception.InnerException.Message);

                    });

                    app.UseExceptionHandler("/non-existent-hander");

                    app.Map("/handle-errors", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(async (httpContext) =>
                        {
                            await httpContext.Response.WriteAsync("Handled error in a custom way.");
                        });
                    });

                    app.Map("/throw", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(httpContext =>
                        {
                            throw new ApplicationException("Something bad happened.");
                        });
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("throw");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task ExceptionHandler_CanReturn404Responses_WhenAllowed()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<ExceptionHandlerMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                    services.Configure<ExceptionHandlerOptions>(options =>
                    {
                        options.AllowStatusCode404Response = true;
                        options.ExceptionHandler = httpContext =>
                        {
                            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                            return Task.CompletedTask;
                        };
                    });
                })
                .Configure(app =>
                {
                    app.UseExceptionHandler();

                    app.Map("/throw", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(httpContext =>
                        {
                            throw new InvalidOperationException("Something bad happened.");
                        });
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("throw");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        Assert.DoesNotContain(sink.Writes, w =>
            w.LogLevel == LogLevel.Warning
            && w.EventId == 4
            && w.Message == "No exception handler was found, rethrowing original exception.");
    }

    [Fact]
    public async Task ExceptionHandler_SelectsStatusCode()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services.AddProblemDetails())
                    .Configure(app =>
                    {
                        app.UseExceptionHandler(new ExceptionHandlerOptions
                        {
                            StatusCodeSelector = ex => ex is ApplicationException
                                ? StatusCodes.Status409Conflict
                                : StatusCodes.Status500InternalServerError,
                        });

                        app.Map("/throw", innerAppBuilder =>
                        {
                            innerAppBuilder.Run(_ => throw new ApplicationException("Something bad happened."));
                        });
                    });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("throw");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact]
    public async Task StatusCodeSelector_CanSelect404()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services.AddProblemDetails())
                    .Configure(app =>
                    {
                        app.UseExceptionHandler(new ExceptionHandlerOptions
                        {
                            // 404 is not allowed,
                            // but as the exception is explicitly mapped to 404 by the StatusCodeSelector,
                            // it should be set anyway.
                            AllowStatusCode404Response = false,
                            StatusCodeSelector = ex => ex is ApplicationException
                                ? StatusCodes.Status404NotFound
                                : StatusCodes.Status500InternalServerError,
                        });

                        app.Map("/throw", innerAppBuilder =>
                        {
                            innerAppBuilder.Run(_ => throw new ApplicationException("Something bad happened."));
                        });
                    });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("throw");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task ExceptionHandlerWithOwnBuilder()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<ExceptionHandlerMiddleware>);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseExceptionHandler(builder =>
                    {
                        builder.Run(c =>
                        {
                            c.Response.StatusCode = 200;
                            return c.Response.WriteAsync("separate pipeline");
                        });
                    });

                    app.Map("/throw", (innerAppBuilder) =>
                    {
                        innerAppBuilder.Run(httpContext =>
                        {
                            throw new InvalidOperationException("Something bad happened.");
                        });
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("throw");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("separate pipeline", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task ExceptionHandlerWithPathWorksAfterUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.Use(async (httpContext, next) =>
        {
            Exception exception = null;
            try
            {
                await next(httpContext);
            }
            catch (InvalidOperationException ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        });

        app.UseRouting();

        app.UseExceptionHandler("/handle-errors");

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/handle-errors", c =>
            {
                c.Response.StatusCode = 200;
                return c.Response.WriteAsync("Handled");
            });
        });

        app.Run((httpContext) =>
        {
            throw new InvalidOperationException("Something bad happened");
        });

        await app.StartAsync();

        using (var server = app.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.Equal("Handled", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task ExceptionHandlerWithOptionsWorksAfterUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.Use(async (httpContext, next) =>
        {
            Exception exception = null;
            try
            {
                await next(httpContext);
            }
            catch (InvalidOperationException ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        });

        app.UseRouting();

        app.UseExceptionHandler(new ExceptionHandlerOptions()
        {
            ExceptionHandlingPath = "/handle-errors"
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/handle-errors", c =>
            {
                c.Response.StatusCode = 200;
                return c.Response.WriteAsync("Handled");
            });
        });

        app.Run((httpContext) =>
        {
            throw new InvalidOperationException("Something bad happened");
        });

        await app.StartAsync();

        using (var server = app.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.Equal("Handled", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task ExceptionHandlerWithAddWorksAfterUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddExceptionHandler(o => o.ExceptionHandlingPath = "/handle-errors");
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.Use(async (httpContext, next) =>
        {
            Exception exception = null;
            try
            {
                await next(httpContext);
            }
            catch (InvalidOperationException ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        });

        app.UseRouting();

        app.UseExceptionHandler();

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/handle-errors", c =>
            {
                c.Response.StatusCode = 200;
                return c.Response.WriteAsync("Handled");
            });
        });

        app.Run((httpContext) =>
        {
            throw new InvalidOperationException("Something bad happened");
        });

        await app.StartAsync();

        using (var server = app.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            response.EnsureSuccessStatusCode();
            Assert.Equal("Handled", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task ExceptionHandlerWithExceptionHandlerNotReplacedWithGlobalRouteBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.Use(async (httpContext, next) =>
        {
            Exception exception = null;
            try
            {
                await next(httpContext);
            }
            catch (InvalidOperationException ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        });

        app.UseRouting();

        app.UseExceptionHandler(new ExceptionHandlerOptions()
        {
            ExceptionHandler = httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return httpContext.Response.WriteAsync("Custom handler");
            }
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/handle-errors", c =>
            {
                c.Response.StatusCode = 200;
                return c.Response.WriteAsync("Handled");
            });
        });

        app.Run((httpContext) =>
        {
            throw new InvalidOperationException("Something bad happened");
        });

        await app.StartAsync();

        using (var server = app.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Custom handler", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task UnhandledError_ExceptionNameTagAdded()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        using var instrumentCollector = new MetricCollector<double>(meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        using var host = new HostBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<IMeterFactory>(meterFactory);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseExceptionHandler(new ExceptionHandlerOptions()
                    {
                        ExceptionHandler = httpContext =>
                        {
                            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                            return httpContext.Response.WriteAsync("Custom handler");
                        }
                    });
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
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await instrumentCollector.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

        // Assert
        Assert.Collection(
            instrumentCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.True(m.Value > 0);
                Assert.Equal(404, (int)m.Tags["http.response.status_code"]);
                Assert.Equal("System.Exception", (string)m.Tags["error.type"]);
            });
    }

    [Fact]
    public async Task UnhandledError_MultipleHandlers_ExceptionNameTagAddedOnce()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        using var instrumentCollector = new MetricCollector<double>(meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        using var host = new HostBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<IMeterFactory>(meterFactory);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    // Second error and handler
                    app.UseExceptionHandler(new ExceptionHandlerOptions()
                    {
                        ExceptionHandler = httpContext =>
                        {
                            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            return Task.CompletedTask;
                        }
                    });
                    app.Use(async (context, next) =>
                    {
                        await next();
                        throw new InvalidOperationException("Test exception2");
                    });

                    // First error and handler
                    app.UseExceptionHandler(new ExceptionHandlerOptions()
                    {
                        ExceptionHandler = httpContext =>
                        {
                            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                            return Task.CompletedTask;
                        }
                    });
                    app.Run(context =>
                    {
                        throw new Exception("Test exception1");
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        // Act
        var response = await server.CreateClient().GetAsync("/path");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        await instrumentCollector.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

        // Assert
        Assert.Collection(
            instrumentCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.True(m.Value > 0);
                Assert.Equal(500, (int)m.Tags["http.response.status_code"]);
                Assert.Equal("System.Exception", (string)m.Tags["error.type"]);
            });
    }

    [Fact]
    public async Task UnhandledError_ErrorAfterHandler_ExceptionNameTagAddedOnce()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        using var instrumentCollector = new MetricCollector<double>(meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        using var host = new HostBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<IMeterFactory>(meterFactory);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    // Second error
                    app.Use(async (context, next) =>
                    {
                        await next();

                        throw new InvalidOperationException("Test exception2");
                    });

                    // First error and handler
                    app.UseExceptionHandler(new ExceptionHandlerOptions()
                    {
                        ExceptionHandler = httpContext =>
                        {
                            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                            return httpContext.Response.WriteAsync("Custom handler");
                        }
                    });
                    app.Run(context =>
                    {
                        throw new Exception("Test exception1");
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        // Act
        await Assert.ThrowsAsync<HttpRequestException>(async () => await server.CreateClient().GetAsync("/path"));

        await instrumentCollector.WaitForMeasurementsAsync(minCount: 1).DefaultTimeout();

        // Assert
        Assert.Collection(
            instrumentCollector.GetMeasurementSnapshot(),
            m =>
            {
                Assert.True(m.Value > 0);
                Assert.Equal(404, (int)m.Tags["http.response.status_code"]);
                Assert.Equal("System.Exception", (string)m.Tags["error.type"]);
            });
    }
}
