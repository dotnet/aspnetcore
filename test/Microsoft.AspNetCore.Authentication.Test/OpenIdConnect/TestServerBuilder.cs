// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.Tests.OpenIdConnect
{
    internal class TestServerBuilder
    {
        public static readonly string Challenge = "/challenge";
        public static readonly string ChallengeWithOutContext = "/challengeWithOutContext";
        public static readonly string ChallengeWithProperties = "/challengeWithProperties";
        public static readonly string Signin = "/signin";
        public static readonly string Signout = "/signout";

        public static TestServer CreateServer(OpenIdConnectOptions options)
        {
            return CreateServer(options, handler: null, properties: null);
        }

        public static TestServer CreateServer(
            OpenIdConnectOptions options,
            Func<HttpContext, Task> handler,
            AuthenticationProperties properties)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme
                    });

                    app.UseOpenIdConnectAuthentication(options);

                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;

                        if (req.Path == new PathString(Challenge))
                        {
                            await context.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
                        }
                        else if (req.Path == new PathString(ChallengeWithProperties))
                        {
                            await context.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
                        }
                        else if (req.Path == new PathString(ChallengeWithOutContext))
                        {
                            res.StatusCode = 401;
                        }
                        else if (req.Path == new PathString(Signin))
                        {
                            // REVIEW: this used to just be res.SignIn()
                            await context.Authentication.SignInAsync(OpenIdConnectDefaults.AuthenticationScheme, new ClaimsPrincipal());
                        }
                        else if (req.Path == new PathString(Signout))
                        {
                            await context.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                        }
                        else if (req.Path == new PathString("/signout_with_specific_redirect_uri"))
                        {
                            await context.Authentication.SignOutAsync(
                                OpenIdConnectDefaults.AuthenticationScheme,
                                new AuthenticationProperties() { RedirectUri = "http://www.example.com/specific_redirect_uri" });
                        }
                        else if (handler != null)
                        {
                            await handler(context);
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddAuthentication();
                    services.Configure<SharedAuthenticationOptions>(authOptions => authOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
                });

            return new TestServer(builder);
        }
    }
}