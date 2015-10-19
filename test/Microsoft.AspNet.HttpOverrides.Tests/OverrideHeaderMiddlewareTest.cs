// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.HttpOverrides
{
    public class OverrideMiddlewareHeaderTests
    {
        [Fact]
        public async Task XForwardedForOverrideChangesRemoteIp()
        {
            var assertsExecuted = false;
            var options = new OverrideHeaderMiddlewareOptions();
            options.ForwardedOptions = ForwardedHeaders.XForwardedFor;

            var server = TestServer.Create(app =>
            {
                app.UseOverrideHeaders(options);
                app.Run(context =>
                {
                    Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XForwardedForOverrideBadIpDoesntChangeRemoteIp()
        {
            var assertsExecuted = false;
            var options = new OverrideHeaderMiddlewareOptions();
            options.ForwardedOptions = ForwardedHeaders.XForwardedFor;

            var server = TestServer.Create(app =>
            {
                app.UseOverrideHeaders(options);
                app.Run(context =>
                {
                    Assert.Null(context.Connection.RemoteIpAddress);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "BAD-IP");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XForwardedHostOverrideChangesRequestHost()
        {
            var assertsExecuted = false;
            var options = new OverrideHeaderMiddlewareOptions();
            options.ForwardedOptions = ForwardedHeaders.XForwardedHost;

            var server = TestServer.Create(app =>
            {
                app.UseOverrideHeaders(options);
                app.Run(context =>
                {
                    Assert.Equal("testhost", context.Request.Host.ToString());
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-Host", "testhost");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XForwardedProtoOverrideChangesRequestProtocol()
        {
            var assertsExecuted = false;
            var options = new OverrideHeaderMiddlewareOptions();
            options.ForwardedOptions = ForwardedHeaders.XForwardedProto;

            var server = TestServer.Create(app =>
            {
                app.UseOverrideHeaders(options);
                app.Run(context =>
                {
                    Assert.Equal("TestProtocol", context.Request.Scheme);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-Proto", "TestProtocol");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public void AllForwardsDisabledByDefault()
        {
            var options = new OverrideHeaderMiddlewareOptions();
            Assert.True(options.ForwardedOptions == 0);
        }

        [Fact]
        public async Task AllForwardsEnabledChangeRequestRemoteIpHostandProtocol()
        {
            var assertsExecuted = false;
            var options = new OverrideHeaderMiddlewareOptions();
            options.ForwardedOptions = ForwardedHeaders.All;

            var server = TestServer.Create(app =>
            {
                app.UseOverrideHeaders(options);
                app.Run(context =>
                {
                    Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
                    Assert.Equal("testhost", context.Request.Host.ToString());
                    Assert.Equal("Protocol", context.Request.Scheme);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

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
            var options = new OverrideHeaderMiddlewareOptions();
            options.ForwardedOptions = ForwardedHeaders.None;

            var server = TestServer.Create(app =>
            {
                app.UseOverrideHeaders(options);
                app.Run(context =>
                {
                    Assert.Null(context.Connection.RemoteIpAddress);
                    Assert.Equal("localhost", context.Request.Host.ToString());
                    Assert.Equal("http", context.Request.Scheme);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

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
            var XForwardedForAndProto = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            var options = new OverrideHeaderMiddlewareOptions();
            options.ForwardedOptions = XForwardedForAndProto;

            var server = TestServer.Create(app =>
            {
                app.UseOverrideHeaders(options);
                app.Run(context =>
                {
                    Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
                    Assert.Equal("localhost", context.Request.Host.ToString());
                    Assert.Equal("Protocol", context.Request.Scheme);
                    assertsExecuted = true;
                    return Task.FromResult(0);

                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11");
            req.Headers.Add("X-Forwarded-Proto", "Protocol");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }
    }
}
