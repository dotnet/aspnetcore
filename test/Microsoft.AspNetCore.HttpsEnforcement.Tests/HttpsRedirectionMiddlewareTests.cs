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
        public async Task SetOptions_SetStatusCodeHttpsPort(int statusCode, int? httpsPort, string expected)
        {

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HttpsRedirectionOptions>(options =>
                    {
                        options.RedirectStatusCode = statusCode;
                        options.HttpsPort = httpsPort;
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
        public async Task SetOptionsThroughHelperMethod_SetStatusCodeAndHttpsPort(int statusCode, int? httpsPort, string expectedUrl)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpsRedirection(options =>
                    {
                        options.RedirectStatusCode = statusCode;
                        options.HttpsPort = httpsPort;
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

        [Theory]
        [InlineData(null, null, "https://localhost/")]
        [InlineData(null, "5000", "https://localhost:5000/")]
        [InlineData(null, "443", "https://localhost/")]
        [InlineData(443, "5000", "https://localhost/")]
        [InlineData(4000, "5000", "https://localhost:4000/")]
        [InlineData(5000, null, "https://localhost:5000/")]
        public async Task SetHttpsPortEnvironmentVariable_ReturnsCorrectStatusCodeOnResponse(int? optionsHttpsPort, string configHttpsPort, string expectedUrl)
        {
            var builder = new WebHostBuilder()
               .ConfigureServices(services =>
               {
                   services.AddHttpsRedirection(options =>
                   {
                       options.HttpsPort = optionsHttpsPort;
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
            builder.UseSetting("HTTPS_PORT", configHttpsPort);
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(expectedUrl, response.Headers.Location.ToString());
        }
    }
}
