// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using Shouldly;
using Newtonsoft.Json;
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
                    services.AddAuthentication(options =>
                    {
                        options.SignInScheme = "External";
                    });
                    services.AddFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.Notifications = new OAuthAuthenticationNotifications
                        {
                            OnApplyRedirect = context =>
                            {
                                context.Response.Redirect(context.RedirectUri + "&custom=test");
                            }
                        };
                    });
                    services.AddCookieAuthentication(options =>
                    {
                        options.AuthenticationScheme = "External";
                        options.AutomaticAuthentication = true;
                    });
                },
                context =>
                {
                    // REVIEW: Gross.
                    context.Authentication.ChallengeAsync("Facebook").GetAwaiter().GetResult();
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("custom=test");
        }

        [Fact]
        public async Task NestedMapWillNotAffectRedirect()
        {
            var server = CreateServer(app =>
                app.Map("/base", map => {
                    map.UseFacebookAuthentication();
                    map.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
                }),
                services =>
                {
                    services.AddAuthentication();
                    services.AddFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.SignInScheme = "External";
                    });
                },
                handler: null);
            var transaction = await server.SendAsync("http://example.com/base/login");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            location.ShouldContain("https://www.facebook.com/v2.2/dialog/oauth");
            location.ShouldContain("response_type=code");
            location.ShouldContain("client_id=");
            location.ShouldContain("redirect_uri=" + UrlEncoder.Default.UrlEncode("http://example.com/base/signin-facebook"));
            location.ShouldContain("scope=");
            location.ShouldContain("state=");
        }

        [Fact]
        public async Task MapWillNotAffectRedirect()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication();
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
                },
                services =>
                {
                    services.AddAuthentication();
                    services.AddFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.SignInScheme = "External";
                    });
                },
                handler: null);
            var transaction = await server.SendAsync("http://example.com/login");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            location.ShouldContain("https://www.facebook.com/v2.2/dialog/oauth");
            location.ShouldContain("response_type=code");
            location.ShouldContain("client_id=");
            location.ShouldContain("redirect_uri="+ UrlEncoder.Default.UrlEncode("http://example.com/signin-facebook"));
            location.ShouldContain("scope=");
            location.ShouldContain("state=");
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
                    services.AddAuthentication(options =>
                    {
                        options.SignInScheme = "External";
                    });
                    services.AddFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                    });
                    services.AddCookieAuthentication(options =>
                    {
                        options.AuthenticationScheme = "External";
                    });
                },
                context =>
                {
                    // REVIEW: gross
                    context.Authentication.ChallengeAsync("Facebook").GetAwaiter().GetResult();
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

        [Fact]
        public async Task CustomUserInfoEndpointHasValidGraphQuery()
        {
            var customUserInfoEndpoint = "https://graph.facebook.com/me?fields=email,timezone,picture";
            string finalUserInfoEndpoint = string.Empty;
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("FacebookTest"));
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication();
                    app.UseCookieAuthentication();
                },
                services =>
                {
                    services.AddAuthentication();
                    services.AddFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.StateDataFormat = stateFormat;
                        options.UserInformationEndpoint = customUserInfoEndpoint;
                        options.BackchannelHttpHandler = new TestHttpMessageHandler
                        {
                            Sender = req =>
                            {
                                if (req.RequestUri.GetLeftPart(UriPartial.Path) == FacebookAuthenticationDefaults.TokenEndpoint)
                                {
                                    var res = new HttpResponseMessage(HttpStatusCode.OK);
                                    var tokenResponse = new Dictionary<string, string>
                                    {
                                        { "access_token", "TestAuthToken" },
                                    };
                                    res.Content = new FormUrlEncodedContent(tokenResponse);
                                    return res;
                                }
                                if (req.RequestUri.GetLeftPart(UriPartial.Path) ==
                                    new Uri(customUserInfoEndpoint).GetLeftPart(UriPartial.Path))
                                {
                                    finalUserInfoEndpoint = req.RequestUri.ToString();
                                    var res = new HttpResponseMessage(HttpStatusCode.OK);
                                    var graphResponse = JsonConvert.SerializeObject(new
                                    {
                                        id = "TestProfileId",
                                        name = "TestName"
                                    });
                                    res.Content = new StringContent(graphResponse, Encoding.UTF8);
                                    return res;
                                }
                                return null;
                            }
                        };
                    });
                }, handler: null);

            var properties = new AuthenticationProperties();
            var correlationKey = ".AspNet.Correlation.Facebook";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-facebook?code=TestCode&state=" + UrlEncoder.Default.UrlEncode(state),
                correlationKey + "=" + correlationValue);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.ToString().ShouldBe("/me");
            finalUserInfoEndpoint.Count(c => c == '?').ShouldBe(1);
            finalUserInfoEndpoint.ShouldContain("fields=email,timezone,picture");
            finalUserInfoEndpoint.ShouldContain("&access_token=");
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
