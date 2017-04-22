// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.HttpOverrides
{
    public class ForwardedHeadersMiddlewareTests
    {
        [Fact]
        public async Task XForwardedForDefaultSettingsChangeRemoteIpAndPort()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
                        Assert.Equal(9090, context.Connection.RemotePort);
                        // No Original set if RemoteIpAddress started null.
                        Assert.False(context.Request.Headers.ContainsKey("X-Original-For"));
                        // Should have been consumed and removed
                        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-For"));
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11:9090");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(1, "11.111.111.11.12345", "10.0.0.1", 99)] // Invalid
        public async Task XForwardedForFirstValueIsInvalid(int limit, string header, string expectedIp, int expectedPort)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
                        context.Connection.RemotePort = 99;
                        return next();
                    });
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                        ForwardLimit = limit,
                    });
                    app.Run(context =>
                    {
                        Assert.Equal(expectedIp, context.Connection.RemoteIpAddress.ToString());
                        Assert.Equal(expectedPort, context.Connection.RemotePort);
                        Assert.False(context.Request.Headers.ContainsKey("X-Original-For"));
                        Assert.True(context.Request.Headers.ContainsKey("X-Forwarded-For"));
                        Assert.Equal(header, context.Request.Headers["X-Forwarded-For"]);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.TryAddWithoutValidation("X-Forwarded-For", header);
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(1, "11.111.111.11:12345", "11.111.111.11", 12345, "", false)]
        [InlineData(1, "11.111.111.11:12345", "11.111.111.11", 12345, "", true)]
        [InlineData(10, "11.111.111.11:12345", "11.111.111.11", 12345, "", false)]
        [InlineData(10, "11.111.111.11:12345", "11.111.111.11", 12345, "", true)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12:23456", false)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12:23456", true)]
        [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", false)]
        [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", true)]
        [InlineData(10, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", false)]
        [InlineData(10, "12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "", true)]
        [InlineData(10, "12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12.23456", false)] // Invalid 2nd value
        [InlineData(10, "12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "12.112.112.12.23456", true)] // Invalid 2nd value
        [InlineData(10, "13.113.113.13:34567, 12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "13.113.113.13:34567,12.112.112.12.23456", false)] // Invalid 2nd value
        [InlineData(10, "13.113.113.13:34567, 12.112.112.12.23456, 11.111.111.11:12345", "11.111.111.11", 12345, "13.113.113.13:34567,12.112.112.12.23456", true)] // Invalid 2nd value
        [InlineData(2, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "13.113.113.13:34567", false)]
        [InlineData(2, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "12.112.112.12", 23456, "13.113.113.13:34567", true)]
        [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "13.113.113.13", 34567, "", false)]
        [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "13.113.113.13", 34567, "", true)]
        public async Task XForwardedForForwardLimit(int limit, string header, string expectedIp, int expectedPort, string remainingHeader, bool requireSymmetry)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
                        context.Connection.RemotePort = 99;
                        return next();
                    });
                    var options = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = requireSymmetry,
                        ForwardLimit = limit,
                    };
                    options.KnownProxies.Clear();
                    options.KnownNetworks.Clear();
                    app.UseForwardedHeaders(options);
                    app.Run(context =>
                    {
                        Assert.Equal(expectedIp, context.Connection.RemoteIpAddress.ToString());
                        Assert.Equal(expectedPort, context.Connection.RemotePort);
                        Assert.Equal(remainingHeader, context.Request.Headers["X-Forwarded-For"].ToString());
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.TryAddWithoutValidation("X-Forwarded-For", header);
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData("11.111.111.11", false)]
        [InlineData("127.0.0.1", true)]
        [InlineData("127.0.1.1", true)]
        [InlineData("::1", true)]
        [InlineData("::", false)]
        public async Task XForwardedForLoopback(string originalIp, bool expectForwarded)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse(originalIp);
                        context.Connection.RemotePort = 99;
                        return next();
                    });
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                    });
                    app.Run(context =>
                    {
                        if (expectForwarded)
                        {
                            Assert.Equal("10.0.0.1", context.Connection.RemoteIpAddress.ToString());
                            Assert.Equal(1234, context.Connection.RemotePort);
                            Assert.True(context.Request.Headers.ContainsKey("X-Original-For"));
                            Assert.Equal(new IPEndPoint(IPAddress.Parse(originalIp), 99).ToString(),
                                context.Request.Headers["X-Original-For"]);
                        }
                        else
                        {
                            Assert.Equal(originalIp, context.Connection.RemoteIpAddress.ToString());
                            Assert.Equal(99, context.Connection.RemotePort);
                            Assert.False(context.Request.Headers.ContainsKey("X-Original-For"));
                        }
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.TryAddWithoutValidation("X-Forwarded-For", "10.0.0.1:1234");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(1, "11.111.111.11:12345", "20.0.0.1", "10.0.0.1", 99, false)]
        [InlineData(1, "11.111.111.11:12345", "20.0.0.1", "10.0.0.1", 99, true)]
        [InlineData(1, "", "10.0.0.1", "10.0.0.1", 99, false)]
        [InlineData(1, "", "10.0.0.1", "10.0.0.1", 99, true)]
        [InlineData(1, "11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, false)]
        [InlineData(1, "11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, true)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, false)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1", "11.111.111.11", 12345, true)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "11.111.111.11", 12345, false)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "11.111.111.11", 12345, true)]
        [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "12.112.112.12", 23456, false)]
        [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11", "12.112.112.12", 23456, true)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, false)]
        [InlineData(1, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, true)]
        [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, false)]
        [InlineData(2, "12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, true)]
        [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "13.113.113.13", 34567, false)]
        [InlineData(3, "13.113.113.13:34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "13.113.113.13", 34567, true)]
        [InlineData(3, "13.113.113.13:34567, 12.112.112.12;23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, false)] // Invalid 2nd IP
        [InlineData(3, "13.113.113.13:34567, 12.112.112.12;23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "11.111.111.11", 12345, true)] // Invalid 2nd IP
        [InlineData(3, "13.113.113.13;34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, false)] // Invalid 3rd IP
        [InlineData(3, "13.113.113.13;34567, 12.112.112.12:23456, 11.111.111.11:12345", "10.0.0.1,11.111.111.11,12.112.112.12", "12.112.112.12", 23456, true)] // Invalid 3rd IP
        public async Task XForwardedForForwardKnownIps(int limit, string header, string knownIPs, string expectedIp, int expectedPort, bool requireSymmetry)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
                        context.Connection.RemotePort = 99;
                        return next();
                    });
                    var options = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = requireSymmetry,
                        ForwardLimit = limit,
                    };
                    foreach (var ip in knownIPs.Split(',').Select(text => IPAddress.Parse(text)))
                    {
                        options.KnownProxies.Add(ip);
                    }
                    app.UseForwardedHeaders(options);
                    app.Run(context =>
                    {
                        Assert.Equal(expectedIp, context.Connection.RemoteIpAddress.ToString());
                        Assert.Equal(expectedPort, context.Connection.RemotePort);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.TryAddWithoutValidation("X-Forwarded-For", header);
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XForwardedForOverrideBadIpDoesntChangeRemoteIp()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    });
                    app.Run(context =>
                    {
                        Assert.Null(context.Connection.RemoteIpAddress);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "BAD-IP");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XForwardedHostOverrideChangesRequestHost()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedHost
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("testhost", context.Request.Host.ToString());
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-Host", "testhost");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(0, "h1", "http")]
        [InlineData(1, "", "http")]
        [InlineData(1, "h1", "h1")]
        [InlineData(3, "h1", "h1")]
        [InlineData(1, "h2, h1", "h1")]
        [InlineData(2, "h2, h1", "h2")]
        [InlineData(10, "h3, h2, h1", "h3")]
        public async Task XForwardedProtoOverrideChangesRequestProtocol(int limit, string header, string expected)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
                        ForwardLimit = limit,
                    });
                    app.Run(context =>
                    {
                        Assert.Equal(expected, context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-Proto", header);
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(0, "h1", "::1", "http")]
        [InlineData(1, "", "::1", "http")]
        [InlineData(1, "h1", "::1", "h1")]
        [InlineData(3, "h1", "::1", "h1")]
        [InlineData(3, "h2, h1", "::1", "http")]
        [InlineData(5, "h2, h1", "::1, ::1", "h2")]
        [InlineData(10, "h3, h2, h1", "::1, ::1, ::1", "h3")]
        [InlineData(10, "h3, h2, h1", "::1, badip, ::1", "h1")]
        public async Task XForwardedProtoOverrideLimitedByXForwardedForCount(int limit, string protoHeader, string forHeader, string expected)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = true,
                        ForwardLimit = limit,
                    });
                    app.Run(context =>
                    {
                        Assert.Equal(expected, context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-Proto", protoHeader);
            req.Headers.Add("X-Forwarded-For", forHeader);
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(0, "h1", "::1", "http")]
        [InlineData(1, "", "::1", "http")]
        [InlineData(1, "h1", "", "h1")]
        [InlineData(1, "h1", "::1", "h1")]
        [InlineData(3, "h1", "::1", "h1")]
        [InlineData(3, "h1", "::1, ::1", "h1")]
        [InlineData(3, "h2, h1", "::1", "h2")]
        [InlineData(5, "h2, h1", "::1, ::1", "h2")]
        [InlineData(10, "h3, h2, h1", "::1, ::1, ::1", "h3")]
        [InlineData(10, "h3, h2, h1", "::1, badip, ::1", "h1")]
        public async Task XForwardedProtoOverrideCanBeIndependentOfXForwardedForCount(int limit, string protoHeader, string forHeader, string expected)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = false,
                        ForwardLimit = limit,
                    });
                    app.Run(context =>
                    {
                        Assert.Equal(expected, context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-Proto", protoHeader);
            req.Headers.Add("X-Forwarded-For", forHeader);
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData("", "", "::1", false, "http")]
        [InlineData("h1", "", "::1", false, "http")]
        [InlineData("h1", "F::", "::1", false, "h1")]
        [InlineData("h1", "F::", "E::", false, "h1")]
        [InlineData("", "", "::1", true, "http")]
        [InlineData("h1", "", "::1", true, "http")]
        [InlineData("h1", "F::", "::1", true, "h1")]
        [InlineData("h1", "", "F::", true, "http")]
        [InlineData("h1", "E::", "F::", true, "http")]
        [InlineData("h2, h1", "", "::1", true, "http")]
        [InlineData("h2, h1", "F::, D::", "::1", true, "h1")]
        [InlineData("h2, h1", "E::, D::", "F::", true, "http")]
        [InlineData("h2, h1", "E::, D::", "F::", true, "http")]
        public async Task XForwardedProtoOverrideLimitedByLoopback(string protoHeader, string forHeader, string remoteIp, bool loopback, string expected)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
                        return next();
                    });
                    var options = new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = true,
                        ForwardLimit = 5,
                    };
                    if (!loopback)
                    {
                        options.KnownNetworks.Clear();
                        options.KnownProxies.Clear();
                    }
                    app.UseForwardedHeaders(options);
                    app.Run(context =>
                    {
                        Assert.Equal(expected, context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-Proto", protoHeader);
            req.Headers.Add("X-Forwarded-For", forHeader);
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public void AllForwardsDisabledByDefault()
        {
            var options = new ForwardedHeadersOptions();
            Assert.True(options.ForwardedHeaders == ForwardedHeaders.None);
            Assert.Equal(1, options.ForwardLimit);
            Assert.Equal(1, options.KnownNetworks.Count());
            Assert.Equal(1, options.KnownProxies.Count());
        }

        [Fact]
        public async Task AllForwardsEnabledChangeRequestRemoteIpHostandProtocol()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.All
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
                        Assert.Equal("testhost", context.Request.Host.ToString());
                        Assert.Equal("Protocol", context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11");
            req.Headers.Add("X-Forwarded-Host", "testhost");
            req.Headers.Add("X-Forwarded-Proto", "Protocol");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task AllOptionsDisabledRequestDoesntChange()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.None
                    });
                    app.Run(context =>
                    {
                        Assert.Null(context.Connection.RemoteIpAddress);
                        Assert.Equal("localhost", context.Request.Host.ToString());
                        Assert.Equal("http", context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11");
            req.Headers.Add("X-Forwarded-Host", "otherhost");
            req.Headers.Add("X-Forwarded-Proto", "Protocol");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task PartiallyEnabledForwardsPartiallyChangesRequest()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
                        Assert.Equal("localhost", context.Request.Host.ToString());
                        Assert.Equal("Protocol", context.Request.Scheme);
                        assertsExecuted = true;
                        return Task.FromResult(0);

                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11");
            req.Headers.Add("X-Forwarded-Proto", "Protocol");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }
    }
}
