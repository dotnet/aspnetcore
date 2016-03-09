// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public class IISMiddlewareTests
    {
        [Fact]
        public async Task MiddlewareSkippedIfTokenIsMissing()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        var auth = context.Features.Get<IHttpAuthenticationFeature>();
                        Assert.Null(auth);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task AddsAuthenticationHandlerByDefault()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    Environment.SetEnvironmentVariable("ASPNETCORE_TOKEN", "TestToken");
                    app.Use((context, next) =>
                    {
                        context.Request.Headers["MS-ASPNETCORE-TOKEN"] = "TestToken";
                        return next();
                    });
                    app.UseIIS();
                    app.Run(context =>
                    {
                        var auth = context.Features.Get<IHttpAuthenticationFeature>();
                        Assert.NotNull(auth);
                        Assert.Equal("Microsoft.AspNetCore.Server.IISIntegration.AuthenticationHandler", auth.Handler.GetType().FullName);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }


        [Fact]
        public async Task DoesNotAddAuthenticationHandlerIfWindowsAuthDisabled()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    Environment.SetEnvironmentVariable("ASPNETCORE_TOKEN", "TestToken");
                    app.Use((context, next) =>
                    {
                        context.Request.Headers["MS-ASPNETCORE-TOKEN"] = "TestToken";
                        return next();
                    });
                    app.UseIIS(new IISOptions
                    {
                        ForwardWindowsAuthentication = false
                    });
                    app.Run(context =>
                    {
                        var auth = context.Features.Get<IHttpAuthenticationFeature>();
                        Assert.Null(auth);
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
