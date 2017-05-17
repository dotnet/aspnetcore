// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
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
                .UseIISIntegration()
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
            var response = await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task MiddlewareRejectsRequestIfTokenHeaderIsMissing()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIISIntegration()
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
            var response = await server.CreateClient().SendAsync(req);
            Assert.False(assertsExecuted);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public void UrlDelayRegisteredAndPreferHostingUrlsSet()
        {
            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.Run(context => Task.FromResult(0));
                });

            Assert.Null(builder.GetSetting(WebHostDefaults.ServerUrlsKey));
            Assert.Null(builder.GetSetting(WebHostDefaults.PreferHostingUrlsKey));

            // Adds a server and calls Build()
            var server = new TestServer(builder);

            Assert.Equal("http://localhost:12345", builder.GetSetting(WebHostDefaults.ServerUrlsKey));
            Assert.Equal("true", builder.GetSetting(WebHostDefaults.PreferHostingUrlsKey));
        }

        [Fact]
        public void PathBaseHiddenFromServer()
        {
            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/pathBase")
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.Run(context => Task.FromResult(0));
                });
            new TestServer(builder);

            Assert.Equal("http://localhost:12345", builder.GetSetting(WebHostDefaults.ServerUrlsKey));
        }

        [Fact]
        public async Task AddsUsePathBaseMiddlewareWhenPathBaseSpecified()
        {
            var requestPathBase = string.Empty;
            var requestPath = string.Empty;
            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/pathbase")
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        requestPathBase = context.Request.PathBase.Value;
                        requestPath = context.Request.Path.Value;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var request = new HttpRequestMessage(HttpMethod.Get, "/PathBase/Path");
            request.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
            var response = await server.CreateClient().SendAsync(request);

            Assert.Equal("/PathBase", requestPathBase);
            Assert.Equal("/Path", requestPath);
        }

        [Fact]
        public async Task AddsAuthenticationHandlerByDefault()
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.Run(async context => 
                    {
                        var auth = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                        var windows = await auth.GetSchemeAsync(IISMiddleware.AuthenticationScheme);
                        Assert.NotNull(windows);
                        Assert.Null(windows.DisplayName);
                        Assert.Equal("Microsoft.AspNetCore.Server.IISIntegration.AuthenticationHandler", windows.HandlerType.FullName);
                        assertsExecuted = true;
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
            await server.CreateClient().SendAsync(req);

            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task OnlyAddAuthenticationHandlerIfForwardWindowsAuthentication(bool forward)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIISIntegration()
                .ConfigureServices(services =>
                {
                    services.Configure<IISOptions>(options =>
                    {
                        options.ForwardWindowsAuthentication = forward;
                    });
                })
                .Configure(app =>
                {
                    app.Run(async context => 
                    {
                        var auth = context.RequestServices.GetService<IAuthenticationSchemeProvider>();
                        Assert.NotNull(auth);
                        var windowsAuth = await auth.GetSchemeAsync(IISMiddleware.AuthenticationScheme);
                        if (forward)
                        {
                            Assert.NotNull(windowsAuth);
                            Assert.Null(windowsAuth.DisplayName);
                            Assert.Equal("AuthenticationHandler", windowsAuth.HandlerType.Name);
                        }
                        else
                        {
                            Assert.Null(windowsAuth);
                        }
                        assertsExecuted = true;
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            req.Headers.TryAddWithoutValidation("MS-ASPNETCORE-TOKEN", "TestToken");
            await server.CreateClient().SendAsync(req);

            Assert.True(assertsExecuted);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DoesNotBlowUpWithoutAuth(bool forward)
        {
            var assertsExecuted = false;

            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIISIntegration()
                .ConfigureServices(services =>
                {
                    services.Configure<IISOptions>(options =>
                    {
                        options.ForwardWindowsAuthentication = forward;
                    });
                })
                .Configure(app =>
                {
                    app.Run(context =>
                    {
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
