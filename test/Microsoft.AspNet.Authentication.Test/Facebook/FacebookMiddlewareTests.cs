// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.Facebook
{
    public class FacebookMiddlewareTests
    {
        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication();
                    app.UseCookieAuthentication();
                },
                services =>
                {
                    services.AddAuthentication();
                    services.ConfigureFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.Notifications = new FacebookAuthenticationNotifications
                        {
                            OnApplyRedirect = context =>
                            {
                                context.Response.Redirect(context.RedirectUri + "&custom=test");
                            }
                        };
                    });
                    services.ConfigureCookieAuthentication(options =>
                    {
                        options.AuthenticationScheme = "External";
                        options.AutomaticAuthentication = true;
                    });
                    services.Configure<ExternalAuthenticationOptions>(options =>
                    {
                        options.SignInScheme = "External";
                    });
                },
                context =>
                {
                    context.Authentication.Challenge("Facebook");
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("custom=test");
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication();
                    app.UseCookieAuthentication();
                },
                services =>
                {
                    services.AddAuthentication();
                    services.ConfigureFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                    });
                    services.ConfigureCookieAuthentication(options =>
                    {
                        options.AuthenticationScheme = "External";
                    });
                    services.Configure<ExternalAuthenticationOptions>(options =>
                    {
                        options.SignInScheme = "External";
                    });
                },
                context =>
                {
                    context.Authentication.Challenge("Facebook");
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            location.ShouldContain("https://www.facebook.com/v2.2/dialog/oauth");
            location.ShouldContain("response_type=code");
            location.ShouldContain("client_id=");
            location.ShouldContain("redirect_uri=");
            location.ShouldContain("scope=");
            location.ShouldContain("state=");
        }

        private static TestServer CreateServer(Action<IApplicationBuilder> configure, Action<IServiceCollection> configureServices, Func<HttpContext, bool> handler)
        {
            return TestServer.Create(app =>
            {
                if (configure != null)
                {
                    configure(app);
                }
                app.Use(async (context, next) =>
                {
                    if (handler == null || !handler(context))
                    {
                        await next();
                    }
                });
            },
            configureServices);
        }
    }
}
