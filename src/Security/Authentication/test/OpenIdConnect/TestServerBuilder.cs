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
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    internal class TestServerBuilder
    {
        public static readonly string DefaultAuthority = @"https://login.microsoftonline.com/common";
        public static readonly string TestHost = @"https://example.com";
        public static readonly string Challenge = "/challenge";
        public static readonly string ChallengeWithOutContext = "/challengeWithOutContext";
        public static readonly string ChallengeWithProperties = "/challengeWithProperties";
        public static readonly string Signin = "/signin";
        public static readonly string Signout = "/signout";

        public static OpenIdConnectOptions CreateOpenIdConnectOptions() =>
            new OpenIdConnectOptions
            {
                Authority = DefaultAuthority,
                ClientId = Guid.NewGuid().ToString(),
                Configuration = CreateDefaultOpenIdConnectConfiguration()
            };

        public static OpenIdConnectOptions CreateOpenIdConnectOptions(Action<OpenIdConnectOptions> update)
        {
            var options = CreateOpenIdConnectOptions();
            update?.Invoke(options);
            return options;
        }

        public static OpenIdConnectConfiguration CreateDefaultOpenIdConnectConfiguration() =>
            new OpenIdConnectConfiguration()
            {
                AuthorizationEndpoint = DefaultAuthority + "/oauth2/authorize",
                EndSessionEndpoint = DefaultAuthority + "/oauth2/endsessionendpoint",
                TokenEndpoint = DefaultAuthority + "/oauth2/token"
            };

        public static IConfigurationManager<OpenIdConnectConfiguration> CreateDefaultOpenIdConnectConfigurationManager() =>
            new StaticConfigurationManager<OpenIdConnectConfiguration>(CreateDefaultOpenIdConnectConfiguration());

        public static TestServer CreateServer(Action<OpenIdConnectOptions> options)
        {
            return CreateServer(options, handler: null, properties: null);
        }

        public static TestServer CreateServer(
            Action<OpenIdConnectOptions> options,
            Func<HttpContext, Task> handler,
            AuthenticationProperties properties)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;

                        if (req.Path == new PathString(Challenge))
                        {
                            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
                        }
                        else if (req.Path == new PathString(ChallengeWithProperties))
                        {
                            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
                        }
                        else if (req.Path == new PathString(ChallengeWithOutContext))
                        {
                            res.StatusCode = 401;
                        }
                        else if (req.Path == new PathString(Signin))
                        {
                            await context.SignInAsync(OpenIdConnectDefaults.AuthenticationScheme, new ClaimsPrincipal());
                        }
                        else if (req.Path == new PathString(Signout))
                        {
                            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                        }
                        else if (req.Path == new PathString("/signout_with_specific_redirect_uri"))
                        {
                            await context.SignOutAsync(
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
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie()
                        .AddOpenIdConnect(options);
                });

            return new TestServer(builder);
        }
    }
}
