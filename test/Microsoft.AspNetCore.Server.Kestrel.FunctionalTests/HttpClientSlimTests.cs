// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class HttpClientSlimTests
    {
        [Fact]
        public async Task GetStringAsyncHttp()
        {
            using (var host = StartHost())
            {
                Assert.Equal("test", await HttpClientSlim.GetStringAsync(host.GetUri()));
            }
        }

        [Fact]
        public async Task GetStringAsyncHttps()
        {
            using (var host = StartHost(protocol: "https"))
            {
                Assert.Equal("test", await HttpClientSlim.GetStringAsync(host.GetUri(isHttps: true), validateCertificate: false));
            }
        }

        [Fact]
        public async Task GetStringAsyncThrowsForErrorResponse()
        {
            using (var host = StartHost(statusCode: StatusCodes.Status500InternalServerError))
            {
                await Assert.ThrowsAnyAsync<HttpRequestException>(() => HttpClientSlim.GetStringAsync(host.GetUri()));
            }
        }

        [Fact]
        public async Task PostAsyncHttp()
        {
            using (var host = StartHost(handler: (context) => context.Request.Body.CopyToAsync(context.Response.Body)))
            {
                Assert.Equal("test post", await HttpClientSlim.PostAsync(host.GetUri(), new StringContent("test post")));
            }
        }

        [Fact]
        public async Task PostAsyncHttps()
        {
            using (var host = StartHost(protocol: "https",
                handler: (context) => context.Request.Body.CopyToAsync(context.Response.Body)))
            {
                Assert.Equal("test post", await HttpClientSlim.PostAsync(host.GetUri(isHttps: true),
                    new StringContent("test post"), validateCertificate: false));
            }
        }

        [Fact]
        public async Task PostAsyncThrowsForErrorResponse()
        {
            using (var host = StartHost(statusCode: StatusCodes.Status500InternalServerError))
            {
                await Assert.ThrowsAnyAsync<HttpRequestException>(
                    () => HttpClientSlim.PostAsync(host.GetUri(), new StringContent("")));
            }
        }

        private IWebHost StartHost(string protocol = "http", int statusCode = StatusCodes.Status200OK, Func<HttpContext, Task> handler = null)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        if (protocol == "https")
                        {
                            listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                        }
                    });
                })
                .Configure((app) =>
                {
                    app.Run(context =>
                    {
                        context.Response.StatusCode = statusCode;
                        if (handler == null)
                        {
                            return context.Response.WriteAsync("test");
                        }
                        else
                        {
                            return handler(context);
                        }
                    });
                })
                .Build();

            host.Start();
            return host;
        }
    }
}
