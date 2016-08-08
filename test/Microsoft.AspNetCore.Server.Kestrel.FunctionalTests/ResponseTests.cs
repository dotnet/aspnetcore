// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ResponseTests
    {
        [Fact]
        public async Task LargeDownload()
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var bytes = new byte[1024];
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bytes[i] = (byte)i;
                        }

                        context.Response.ContentLength = bytes.Length * 1024;

                        for (int i = 0; i < 1024; i++)
                        {
                            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                        }
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://localhost:{host.GetPort()}/");
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStreamAsync();

                    // Read the full response body
                    var total = 0;
                    var bytes = new byte[1024];
                    var count = await responseBody.ReadAsync(bytes, 0, bytes.Length);
                    while (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            Assert.Equal(total % 256, bytes[i]);
                            total++;
                        }
                        count = await responseBody.ReadAsync(bytes, 0, bytes.Length);
                    }
                }
            }
        }

        [Theory, MemberData(nameof(NullHeaderData))]
        public async Task IgnoreNullHeaderValues(string headerName, StringValues headerValue, string expectedValue)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.Headers.Add(headerName, headerValue);
                        
                        await context.Response.WriteAsync("");
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://localhost:{host.GetPort()}/");
                    response.EnsureSuccessStatusCode();

                    var headers = response.Headers;

                    if (expectedValue == null)
                    {
                        Assert.False(headers.Contains(headerName));
                    }
                    else
                    {
                        Assert.True(headers.Contains(headerName));
                        Assert.Equal(headers.GetValues(headerName).Single(), expectedValue);
                    }
                }
            }
        }

        [Fact]
        public async Task OnCompleteCalledEvenWhenOnStartingNotCalled()
        {
            var onStartingCalled = false;
            var onCompletedCalled = false;

            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        context.Response.OnStarting(() => Task.Run(() => onStartingCalled = true));
                        context.Response.OnCompleted(() => Task.Run(() => onCompletedCalled = true));

                        // Prevent OnStarting call (see Frame<T>.RequestProcessingAsync()).
                        throw new Exception();
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://localhost:{host.GetPort()}/");

                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                    Assert.False(onStartingCalled);
                    Assert.True(onCompletedCalled);
                }
            }
        }

        [Fact]
        public async Task OnStartingThrowsWhenSetAfterResponseHasAlreadyStarted()
        {
            InvalidOperationException ex = null;

            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("hello, world");
                        await context.Response.Body.FlushAsync();
                        ex = Assert.Throws<InvalidOperationException>(() => context.Response.OnStarting(_ => TaskUtilities.CompletedTask, null));
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://localhost:{host.GetPort()}/");

                    // Despite the error, the response had already started
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.NotNull(ex);
                }
            }
        }

        public static TheoryData<string, StringValues, string> NullHeaderData
        {
            get
            {
                var dataset = new TheoryData<string, StringValues, string>();

                // Unknown headers
                dataset.Add("NullString", (string)null, null);
                dataset.Add("EmptyString", "", "");
                dataset.Add("NullStringArray", new string[] { null }, null);
                dataset.Add("EmptyStringArray", new string[] { "" }, "");
                dataset.Add("MixedStringArray", new string[] { null, "" }, "");
                // Known headers
                dataset.Add("Location", (string)null, null);
                dataset.Add("Location", "", "");
                dataset.Add("Location", new string[] { null }, null);
                dataset.Add("Location", new string[] { "" }, "");
                dataset.Add("Location", new string[] { null, "" }, "");

                return dataset;
            }
        }
    }
}
