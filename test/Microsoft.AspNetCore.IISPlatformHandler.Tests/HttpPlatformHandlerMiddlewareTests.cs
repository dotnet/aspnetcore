// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.IISPlatformHandler
{
    public class HttpPlatformHandlerMiddlewareTests
    {
        [Fact]
        public async Task AddsAuthenticationHandlerByDefault()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseIISPlatformHandler();
                    app.Run(context =>
                    {
                        var auth = (IHttpAuthenticationFeature)context.Features[typeof(IHttpAuthenticationFeature)];
                        Assert.NotNull(auth);
                        Assert.Equal("Microsoft.AspNetCore.IISPlatformHandler.AuthenticationHandler", auth.Handler.GetType().FullName);
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
                    app.UseIISPlatformHandler(new IISPlatformHandlerOptions
                    {
                        ForwardWindowsAuthentication = false
                    });
                    app.Run(context =>
                    {
                        var auth = (IHttpAuthenticationFeature)context.Features[typeof(IHttpAuthenticationFeature)];
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
