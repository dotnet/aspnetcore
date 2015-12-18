// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.HttpOverrides
{
    public class HttpMethodOverrideMiddlewareTest
    {
        [Fact]
        public async Task XHttpMethodOverrideHeaderAvaiableChangesRequestMethod()
        {
            var assertsExecuted = false;
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        assertsExecuted = true;
                        Assert.Equal("DELETE", context.Request.Method);
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Post, "");
            req.Headers.Add("X-Http-Method-Override", "DELETE");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XHttpMethodOverrideHeaderUnavaiableDoesntChangeRequestMethod()
        {
            var assertsExecuted = false;
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        Assert.Equal("POST",context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Post, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XHttpMethodOverrideFromGetRequestDoesntChangeMethodType()
        {
            var assertsExecuted = false;
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        Assert.Equal("GET", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }
    }
}
