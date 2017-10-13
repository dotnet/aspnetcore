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
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpsPolicy.Tests
{
    public class HttpsRedirectionMiddlewareTests
    {
        [Fact]
        public async Task SetOptions_DefaultsSetCorrectly()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                })
                .Configure(app =>
                {
                    app.UseHttpsRedirection();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal("https://localhost/", response.Headers.Location.ToString());
        }

        [Theory]
        [InlineData(301, null, "https://localhost/")]
        [InlineData(302, null, "https://localhost/")]
        [InlineData(307, null, "https://localhost/")]
        [InlineData(308, null, "https://localhost/")]
        [InlineData(301, 5050, "https://localhost:5050/")]
        [InlineData(301, 443, "https://localhost/")]
        public async Task SetOptions_SetStatusCodeTlsPort(int statusCode, int? tlsPort, string expected)
        {

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HttpsRedirectionOptions>(options =>
                    {
                        options.RedirectStatusCode = statusCode;
                        options.TlsPort = tlsPort;
                    });
                })
                .Configure(app =>
                {
                    app.UseHttpsRedirection();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(statusCode, (int)response.StatusCode);
            Assert.Equal(expected, response.Headers.Location.ToString());
        }

        [Theory]
        [InlineData(301, null, "https://localhost/")]
        [InlineData(302, null, "https://localhost/")]
        [InlineData(307, null, "https://localhost/")]
        [InlineData(308, null, "https://localhost/")]
        [InlineData(301, 5050, "https://localhost:5050/")]
        [InlineData(301, 443, "https://localhost/")]
        public async Task SetOptionsThroughHelperMethod_SetStatusCodeTlsPort(int statusCode, int? tlsPort, string expectedUrl)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpsRedirection(options =>
                    {
                        options.RedirectStatusCode = statusCode;
                        options.TlsPort = tlsPort;
                    });
                })
                .Configure(app =>
                {
                    app.UseHttpsRedirection();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(statusCode, (int)response.StatusCode);
            Assert.Equal(expectedUrl, response.Headers.Location.ToString());
        }
    }
}
