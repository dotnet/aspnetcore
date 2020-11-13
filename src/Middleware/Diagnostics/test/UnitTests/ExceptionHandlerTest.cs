// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics
{
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
                                await next();
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
                                await next();
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
                                await next();
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
                "Alternatively, set one of the aforementioned properties in 'Startup.ConfigureServices' as follows: 'services.AddExceptionHandler(options => { ... });'.",
                exception.Message);
        }

        [Fact]
        public async Task ExceptionHandlerNotFound_RethrowsOriginalError()
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
                    })
                    .Configure(app =>
                    {
                        app.Use(async (httpContext, next) =>
                        {
                            Exception exception = null;
                            try
                            {
                                await next();
                            }
                            catch (InvalidOperationException ex)
                            {
                                exception = ex;

                                // This mimics what the server would do when an exception occurs
                                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            }

                            // The original exception is thrown
                            Assert.NotNull(exception);
                            Assert.Equal("Something bad happened.", exception.Message);

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
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }

            Assert.Contains(sink.Writes, w =>
                w.LogLevel == LogLevel.Warning
                && w.EventId == 4
                && w.Message == "No exception handler was found, rethrowing original exception.");
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
    }
}
