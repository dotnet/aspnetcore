// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    public class OAuthTests
    {
        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection().AddOAuthAuthentication("oauth", o => { });
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync("oauth");
            Assert.NotNull(scheme);
            Assert.Equal("OAuthHandler`1", scheme.HandlerType.Name);
            Assert.Equal("oauth", scheme.DisplayName);
        }

        [Fact]
        public async Task ThrowsIfClientIdMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddOAuthAuthentication("weeblie", o =>
                {
                    o.SignInScheme = "whatever";
                    o.CallbackPath = "/";
                    o.ClientSecret = "whatever";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                }),
                context =>
                {
                    // REVIEW: Gross.
                    Assert.Throws<ArgumentException>("ClientId", () => context.ChallengeAsync("weeblie").GetAwaiter().GetResult());
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ThrowsIfClientSecretMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddOAuthAuthentication("weeblie", o =>
                {
                    o.SignInScheme = "whatever";
                    o.ClientId = "Whatever;";
                    o.CallbackPath = "/";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                }),
                context =>
                {
                    // REVIEW: Gross.
                    Assert.Throws<ArgumentException>("ClientSecret", () => context.ChallengeAsync("weeblie").GetAwaiter().GetResult());
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        public async Task ThrowsIfCallbackPathMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddOAuthAuthentication("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                    o.SignInScheme = "eh";
                }),
                context =>
                {
                    // REVIEW: Gross.
                    Assert.Throws<ArgumentException>("CallbackPath", () => context.ChallengeAsync("weeblie").GetAwaiter().GetResult());
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ThrowsIfTokenEndpointMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddOAuthAuthentication("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.CallbackPath = "/";
                    o.AuthorizationEndpoint = "/";
                    o.SignInScheme = "eh";
                }),
                context =>
                {
                    // REVIEW: Gross.
                    Assert.Throws<ArgumentException>("TokenEndpoint", () => context.ChallengeAsync("weeblie").GetAwaiter().GetResult());
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        public async Task ThrowsIfAuthorizationEndpointMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddOAuthAuthentication("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.CallbackPath = "/";
                    o.TokenEndpoint = "/";
                    o.SignInScheme = "eh";
                }),
                context =>
                {
                    // REVIEW: Gross.
                    Assert.Throws<ArgumentException>("AuthorizationEndpoint", () => context.ChallengeAsync("weeblie").GetAwaiter().GetResult());
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ThrowsIfSignInSchemeMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddOAuthAuthentication("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.CallbackPath = "/";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                }),
                context =>
                {
                    // REVIEW: Gross.
                    Assert.Throws<ArgumentException>("SignInScheme", () => context.ChallengeAsync("weeblie").GetAwaiter().GetResult());
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }


        private static TestServer CreateServer(Action<IApplicationBuilder> configure, Action<IServiceCollection> configureServices, Func<HttpContext, bool> handler)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    configure?.Invoke(app);
                    app.Use(async (context, next) =>
                    {
                        if (handler == null || !handler(context))
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(configureServices);
            return new TestServer(builder);
        }
    }
}
