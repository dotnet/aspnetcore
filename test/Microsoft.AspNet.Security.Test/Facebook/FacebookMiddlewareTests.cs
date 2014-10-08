// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Security.Facebook
{
    public class FacebookMiddlewareTests
    {
        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseServices(services =>
                    {
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
                            options.AuthenticationType = "External";
                        });
                        services.ConfigureOptions<ExternalAuthenticationOptions>(options =>
                        {
                            options.SignInAsAuthenticationType = "External";
                        });
                    });
                    app.UseFacebookAuthentication();
                    app.UseCookieAuthentication();
                },
                context =>
                {
                    context.Response.Challenge("Facebook");
                    return true;
                });
            var transaction = await SendAsync(server, "http://example.com/challenge");
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
                    app.UseServices(services =>
                    {
                        services.ConfigureFacebookAuthentication(options =>
                        {
                            options.AppId = "Test App Id";
                            options.AppSecret = "Test App Secret";
                        });
                        services.ConfigureCookieAuthentication(options =>
                        {
                            options.AuthenticationType = "External";
                        });
                        services.ConfigureOptions<ExternalAuthenticationOptions>(options =>
                        {
                            options.SignInAsAuthenticationType = "External";
                        });
                    });
                    app.UseFacebookAuthentication();
                    app.UseCookieAuthentication();
                },
                context =>
                {
                    context.Response.Challenge("Facebook");
                    return true;
                });
            var transaction = await SendAsync(server, "http://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            location.ShouldContain("https://www.facebook.com/dialog/oauth");
            location.ShouldContain("response_type=code");
            location.ShouldContain("client_id=");
            location.ShouldContain("redirect_uri=");
            location.ShouldContain("scope=");
            location.ShouldContain("state=");
        }

        private static TestServer CreateServer(Action<IApplicationBuilder> configure, Func<HttpContext, bool> handler)
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
            });
        }

        private static async Task<Transaction> SendAsync(TestServer server, string uri, string cookieHeader = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }
            var transaction = new Transaction
            {
                Request = request,
                Response = await server.CreateClient().SendAsync(request),
            };
            if (transaction.Response.Headers.Contains("Set-Cookie"))
            {
                transaction.SetCookie = transaction.Response.Headers.GetValues("Set-Cookie").ToList();
            }
            transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

            return transaction;
        }

        private class Transaction
        {
            public HttpRequestMessage Request { get; set; }
            public HttpResponseMessage Response { get; set; }
            public IList<string> SetCookie { get; set; }
            public string ResponseText { get; set; }
        }
    }
}
