// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task GetStringAsyncHttps()
        {
            using (var host = StartHost(protocol: "https"))
            {
                Assert.Equal("test", await HttpClientSlim.GetStringAsync(host.GetUri(), validateCertificate: false));
            }
        }

        [Fact]
        public async Task GetStringAsyncThrowsForErrorResponse()
        {
            using (var host = StartHost(statusCode: 500))
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

        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task PostAsyncHttps()
        {
            using (var host = StartHost(protocol: "https",
                handler: (context) => context.Request.Body.CopyToAsync(context.Response.Body)))
            {
                Assert.Equal("test post", await HttpClientSlim.PostAsync(host.GetUri(),
                    new StringContent("test post"), validateCertificate: false));
            }
        }

        [Fact]
        public async Task PostAsyncThrowsForErrorResponse()
        {
            using (var host = StartHost(statusCode: 500))
            {
                await Assert.ThrowsAnyAsync<HttpRequestException>(
                    () => HttpClientSlim.PostAsync(host.GetUri(), new StringContent("")));
            }
        }

        private IWebHost StartHost(string protocol = "http", int statusCode = 200, Func<HttpContext, Task> handler = null)
        {
            var host = new WebHostBuilder()
                .UseUrls($"{protocol}://127.0.0.1:0")
                .UseKestrel(options =>
                {
                    options.UseHttps(@"TestResources/testCert.pfx", "testPassword");
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
