// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HostFiltering
{
    public class HostFilteringMiddlewareTests
    {
        [Fact]
        public async Task MissingConfigThrows()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHostFiltering();
                });
            await Assert.ThrowsAsync<InvalidOperationException>(() => new TestServer(builder).SendAsync(_ => { }));
        }

        [Theory]
        [InlineData(true, 200)]
        [InlineData(false, 400)]
        public async Task AllowsMissingHost(bool allowed, int status)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostFiltering(options =>
                    {
                        options.AllowEmptyHosts = allowed;
                        options.AllowedHosts.Add("Localhost");
                    });
                })
                .Configure(app =>
                {
                    app.Use((ctx, next) =>
                    {
                        ctx.Request.Headers.Remove(HeaderNames.Host);
                        return next();
                    });
                    app.UseHostFiltering();
                    app.Run(c =>
                    {
                        Assert.False(c.Request.Headers.TryGetValue(HeaderNames.Host, out var host));
                        return Task.CompletedTask;
                    });
                });
            var server = new TestServer(builder);
            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(status, (int)response.StatusCode);
        }

        [Theory]
        [InlineData(true, 200)]
        [InlineData(false, 400)]
        public async Task AllowsEmptyHost(bool allowed, int status)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostFiltering(options =>
                    {
                        options.AllowEmptyHosts = allowed;
                        options.AllowedHosts.Add("Localhost");
                    });
                })
                .Configure(app =>
                {
                    app.Use((ctx, next) =>
                    {
                        ctx.Request.Headers[HeaderNames.Host] = "";
                        return next();
                    });
                    app.UseHostFiltering();
                    app.Run(c =>
                    {
                        Assert.True(c.Request.Headers.TryGetValue(HeaderNames.Host, out var host));
                        Assert.True(StringValues.Equals("", host));
                        return Task.CompletedTask;
                    });
                    app.Run(c => Task.CompletedTask);
                });
            var server = new TestServer(builder);
            var response = await server.CreateClient().GetAsync("/");
            Assert.Equal(status, (int)response.StatusCode);
        }

        [Theory]
        [InlineData("localHost", "localhost")]
        [InlineData("localHost", "*")] // Any - Used by HttpSys
        [InlineData("localHost", "[::]")] // IPv6 Any - This is what Kestrel reports when binding to *
        [InlineData("localHost", "0.0.0.0")] // IPv4 Any
        [InlineData("localhost:9090", "example.com;localHost")]
        [InlineData("example.com:443", "example.com;localhost")]
        [InlineData("localHost:80", "localhost;")]
        [InlineData("foo.eXample.com:443", "*.exampLe.com")]
        [InlineData("f.eXample.com:443", "*.exampLe.com")]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("127.0.0.1:443", "127.0.0.1")]
        [InlineData("xn--c1yn36f:443", "xn--c1yn36f")]
        [InlineData("xn--c1yn36f:443", "點看")]
        [InlineData("[::ABC]", "[::aBc]")]
        [InlineData("[::1]:80", "[::1]")]
        public async Task AllowsSpecifiedHost(string host, string allowedHost)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostFiltering(options =>
                    {
                        options.AllowedHosts = allowedHost.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    });
                })
                .Configure(app =>
                {
                    app.Use((ctx, next) =>
                    {
                        // TestHost's ClientHandler doesn't let you set the host header, only the host in the URI
                        // and that would over-normalize some of our test conditions like casing.
                        ctx.Request.Headers[HeaderNames.Host] = host;
                        return next();
                    });
                    app.UseHostFiltering();
                    app.Run(c => Task.CompletedTask);
                });
            var server = new TestServer(builder);
            var response = await server.CreateRequest("/").GetAsync();
            Assert.Equal(200, (int)response.StatusCode);
        }

        [Theory]
        [InlineData("example.com", "localhost")]
        [InlineData("localhost:9090", "example.com;")]
        [InlineData(";", "example.com;localhost")]
        [InlineData(";:80", "example.com;localhost")]
        [InlineData(":80", "localhost")]
        [InlineData(":", "localhost")]
        [InlineData("example.com:443", "*.example.com")]
        [InlineData(".example.com:443", "*.example.com")]
        [InlineData("foo.com:443", "*.example.com")]
        [InlineData("foo.example.com.bar:443", "*.example.com")]
        [InlineData(".com:443", "*.com")]
        // Unicode in the host shouldn't be allowed without punycode anyways. This match fails because the middleware converts
        // its input to punycode.
        [InlineData("點看", "點看")]
        [InlineData("[::1", "[::1]")]
        [InlineData("[::1:80", "[::1]")]
        public async Task RejectsMismatchedHosts(string host, string allowedHost)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostFiltering(options =>
                    {
                        options.AllowedHosts = allowedHost.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    });
                })
                .Configure(app =>
                {
                    app.Use((ctx, next) =>
                    {
                        // TestHost's ClientHandler doesn't let you set the host header, only the host in the URI
                        // and that would reject some of our test conditions.
                        ctx.Request.Headers[HeaderNames.Host] = host;
                        return next();
                    });
                    app.UseHostFiltering();
                    app.Run(c => throw new NotImplementedException("App"));
                });
            var server = new TestServer(builder);
            var response = await server.CreateRequest("/").GetAsync();
            Assert.Equal(400, (int)response.StatusCode);
        }

        [Fact]
        public async Task SupportsDynamicOptionsReload()
        {
            var config = new ConfigurationBuilder().Add(new ReloadableMemorySource()).Build();
            config["AllowedHosts"] = "localhost";
            var currentHost = "otherHost";

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostFiltering(options =>
                    {
                        options.AllowedHosts = new[] { config["AllowedHosts"] };
                    });
                    services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(new ConfigurationChangeTokenSource<HostFilteringOptions>(config));
                })
                .Configure(app =>
                {
                    app.Use((ctx, next) =>
                    {
                        ctx.Request.Headers[HeaderNames.Host] = currentHost;
                        return next();
                    });
                    app.UseHostFiltering();
                    app.Run(c => Task.CompletedTask);
                });
            var server = new TestServer(builder);
            var response = await server.CreateRequest("/").GetAsync();
            Assert.Equal(400, (int)response.StatusCode);

            config["AllowedHosts"] = "otherHost";

            response = await server.CreateRequest("/").GetAsync();
            Assert.Equal(200, (int)response.StatusCode);
        }

        private class ReloadableMemorySource : IConfigurationSource
        {
            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                return new ReloadableMemoryProvider();
            }
        }

        internal class ReloadableMemoryProvider : ConfigurationProvider
        {
            public override void Set(string key, string value)
            {
                base.Set(key, value);
                OnReload();
            }
        }
    }
}
