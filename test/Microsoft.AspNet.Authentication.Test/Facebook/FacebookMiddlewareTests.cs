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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders;
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
                    app.UseFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.Events = new OAuthEvents
                        {
                            OnRedirectToAuthorizationEndpoint = context =>
                            {
                                context.Response.Redirect(context.RedirectUri + "&custom=test");
                                return Task.FromResult(0);
                            }
                        };
                    });
                    app.UseCookieAuthentication(options =>
                    {
                        options.AuthenticationScheme = "External";
                        options.AutomaticAuthentication = true;
                    });
                },
                services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.SignInScheme = "External";
                    });
                },
                context =>
                {
                    // REVIEW: Gross.
                    context.Authentication.ChallengeAsync("Facebook").GetAwaiter().GetResult();
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("custom=test", query);
        }

        [Fact]
        public async Task NestedMapWillNotAffectRedirect()
        {
            var server = CreateServer(app =>
                app.Map("/base", map => {
                    map.UseFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.SignInScheme = "External";
                    });
                    map.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
                }),
                services => services.AddAuthentication(),
                handler: null);
            var transaction = await server.SendAsync("http://example.com/base/login");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v2.2/dialog/oauth", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri=" + UrlEncoder.Default.UrlEncode("http://example.com/base/signin-facebook"), location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
        }

        [Fact]
        public async Task MapWillNotAffectRedirect()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.SignInScheme = "External";
                    });
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
                },
                services => services.AddAuthentication(),
                handler: null);
            var transaction = await server.SendAsync("http://example.com/login");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v2.2/dialog/oauth", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri="+ UrlEncoder.Default.UrlEncode("http://example.com/signin-facebook"), location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                    });
                    app.UseCookieAuthentication(options => options.AuthenticationScheme = "External");
                },
                services =>
                {
                    services.AddAuthentication(options => options.SignInScheme = "External");
                },
                context =>
                {
                    // REVIEW: gross
                    context.Authentication.ChallengeAsync("Facebook").GetAwaiter().GetResult();
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v2.2/dialog/oauth", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri=", location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
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
                    app.UseFacebookAuthentication(options =>
                    {
                        options.AppId = "Test App Id";
                        options.AppSecret = "Test App Secret";
                        options.StateDataFormat = stateFormat;
                        options.UserInformationEndpoint = customUserInfoEndpoint;
                        options.BackchannelHttpHandler = new TestHttpMessageHandler
                        {
                            Sender = req =>
                            {
                                if (req.RequestUri.GetLeftPart(UriPartial.Path) == FacebookDefaults.TokenEndpoint)
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
                    app.UseCookieAuthentication();
                },
                services =>
                {
                    services.AddAuthentication();
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
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(1, finalUserInfoEndpoint.Count(c => c == '?'));
            Assert.Contains("fields=email,timezone,picture", finalUserInfoEndpoint);
            Assert.Contains("&access_token=", finalUserInfoEndpoint);
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
