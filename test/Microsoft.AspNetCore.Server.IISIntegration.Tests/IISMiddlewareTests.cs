// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIIS()
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
            req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task MiddlewareSkippedIfTokenHeaderIsMissing()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIIS()
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
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIIS()
                .Configure(app =>
                {
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
            req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
            await server.CreateClient().SendAsync(req);

            Assert.True(assertsExecuted);
        }


        [Fact]
        public async Task DoesNotAddAuthenticationHandlerIfWindowsAuthDisabled()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIIS()
                .ConfigureServices(services =>
                {
                    services.Configure<IISOptions>(options =>
                    {
                        options.ForwardWindowsAuthentication = false;
                    });
                })
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
            req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
            await server.CreateClient().SendAsync(req);

            Assert.True(assertsExecuted);
        }
    }
}
