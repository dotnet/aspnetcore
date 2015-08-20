// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Diagnostics
{
    public class ErrorHandlerTest
    {
        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task OnlyHandles_UnhandledExceptions(HttpStatusCode expectedStatusCode)
        {
            using (var server = TestServer.Create(app =>
            {
                app.UseErrorHandler("/handle-errors");

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
            }))
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
            using (var server = TestServer.Create(app =>
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

                app.UseErrorHandler("/handle-errors");

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
            }))
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
            using (var server = TestServer.Create(app =>
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

                        response.Body = originalResponseBody;
                        bufferingStream.Seek(0, SeekOrigin.Begin);
                        await bufferingStream.CopyToAsync(response.Body);
                    }
                    finally
                    {
                        response.Body = originalResponseBody;
                    }
                });

                app.UseErrorHandler("/handle-errors");

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
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.Equal(expectedResponseBody, await response.Content.ReadAsStringAsync());
                IEnumerable<string> values;
                Assert.True(response.Headers.TryGetValues("Cache-Control", out values));
                Assert.Single(values);
                Assert.Equal("no-cache", values.First());
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
            using (var server = TestServer.Create(app =>
            {
                app.UseErrorHandler("/handle-errors");

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
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.Equal(expectedResponseBody, await response.Content.ReadAsStringAsync());
                IEnumerable<string> values;
                Assert.True(response.Headers.TryGetValues("Cache-Control", out values));
                Assert.Single(values);
                Assert.Equal("no-cache", values.First());
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
            using (var server = TestServer.Create(app =>
            {
                app.UseErrorHandler("/handle-errors");

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
            }))
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
            using (var server = TestServer.Create(app =>
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

                app.UseErrorHandler("/handle-errors");

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
            }))
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
    }
}
