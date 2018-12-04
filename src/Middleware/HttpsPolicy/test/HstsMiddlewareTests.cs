// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    public class HstsMiddlewareTests
    {
        [Fact]
        public async Task SetOptionsWithDefault_SetsMaxAgeToCorrectValue()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                })
                .Configure(app =>
                {
                    app.UseHsts();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("https://example.com:5050");

            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("max-age=2592000", response.Headers.GetValues(HeaderNames.StrictTransportSecurity).FirstOrDefault());
        }

        [Theory]
        [InlineData(0, false, false, "max-age=0")]
        [InlineData(-1, false, false, "max-age=-1")]
        [InlineData(0, true, false, "max-age=0; includeSubDomains")]
        [InlineData(50000, false, true, "max-age=50000; preload")]
        [InlineData(0, true, true, "max-age=0; includeSubDomains; preload")]
        [InlineData(50000, true, true, "max-age=50000; includeSubDomains; preload")]
        public async Task SetOptionsThroughConfigure_SetsHeaderCorrectly(int maxAge, bool includeSubDomains, bool preload, string expected)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<HstsOptions>(options => {
                        options.Preload = preload;
                        options.IncludeSubDomains = includeSubDomains;
                        options.MaxAge = TimeSpan.FromSeconds(maxAge);
                    });
                })
                .Configure(app =>
                {
                    app.UseHsts();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("https://example.com:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, response.Headers.GetValues(HeaderNames.StrictTransportSecurity).FirstOrDefault());
        }

        [Theory]
        [InlineData(0, false, false, "max-age=0")]
        [InlineData(-1, false, false, "max-age=-1")]
        [InlineData(0, true, false, "max-age=0; includeSubDomains")]
        [InlineData(50000, false, true, "max-age=50000; preload")]
        [InlineData(0, true, true, "max-age=0; includeSubDomains; preload")]
        [InlineData(50000, true, true, "max-age=50000; includeSubDomains; preload")]
        public async Task SetOptionsThroughHelper_SetsHeaderCorrectly(int maxAge, bool includeSubDomains, bool preload, string expected)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHsts(options => {
                        options.Preload = preload;
                        options.IncludeSubDomains = includeSubDomains;
                        options.MaxAge = TimeSpan.FromSeconds(maxAge);
                    });
                })
                .Configure(app =>
                {
                    app.UseHsts();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("https://example.com:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, response.Headers.GetValues(HeaderNames.StrictTransportSecurity).FirstOrDefault());
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("Localhost")]
        [InlineData("LOCALHOST")]
        [InlineData("127.0.0.1")]
        [InlineData("[::1]")]
        public async Task DefaultExcludesCommonLocalhostDomains_DoesNotSetHstsHeader(string host)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHsts();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.BaseAddress = new Uri($"https://{host}:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("127.0.0.1")]
        [InlineData("[::1]")]
        public async Task AllowLocalhostDomainsIfListIsReset_SetHstsHeader(string host)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHsts(options =>
                    {
                        options.ExcludedHosts.Clear();
                    });
                })
                .Configure(app =>
                {
                    app.UseHsts();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.BaseAddress = new Uri($"https://{host}:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(response.Headers);
        }
        
        [Theory]
        [InlineData("example.com")]
        [InlineData("Example.com")]
        [InlineData("EXAMPLE.COM")]
        public async Task AddExcludedDomains_DoesNotAddHstsHeader(string host)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHsts(options => {
                        options.ExcludedHosts.Add(host);
                    });
                })
                .Configure(app =>
                {
                    app.UseHsts();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.BaseAddress = new Uri($"https://{host}:5050");
            var request = new HttpRequestMessage(HttpMethod.Get, "");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);
        }
    }
}
