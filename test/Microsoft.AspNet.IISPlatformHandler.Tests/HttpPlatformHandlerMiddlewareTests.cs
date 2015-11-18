// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.IISPlatformHandler
{
    public class HttpPlatformHandlerMiddlewareTests
    {
        [Fact]
        public async Task XForwardedForOverrideChangesRemoteIpAndPort()
        {
            var assertsExecuted = false;

            var server = TestServer.Create(app =>
            {
                app.UseIISPlatformHandler();
                app.Run(context =>
                {
                    Assert.Equal("11.111.111.11", context.Connection.RemoteIpAddress.ToString());
                    Assert.Equal(123, context.Connection.RemotePort);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11:123");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XForwardedForStoresOriginalIpAndPort()
        {
            var assertsExecuted = false;

            var server = TestServer.Create(app =>
            {
                app.Use((context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Loopback;
                    context.Connection.RemotePort = 1;
                    return next();
                });
                app.UseIISPlatformHandler();
                app.Run(context =>
                {
                    Assert.Equal("127.0.0.1", context.Request.Headers["X-Original-IP"]);
                    Assert.Equal("1", context.Request.Headers["X-Original-Port"]);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.Add("X-Forwarded-For", "11.111.111.11:123");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XForwardedForOverrideBadIpDoesntChangeRemoteIp()
        {
            var assertsExecuted = false;

            var server = TestServer.Create(app =>
            {
                app.UseIISPlatformHandler();
                app.Run(context =>
                {
                    Assert.Null(context.Connection.RemoteIpAddress);
                    Assert.Equal(0, context.Connection.RemotePort);
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
        public async Task XForwardedProtoOverrideChangesRequestProtocol()
        {
            var assertsExecuted = false;

            var server = TestServer.Create(app =>
            {
                app.UseIISPlatformHandler();
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
    }
}