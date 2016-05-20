// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Facebook
{
    public class FacebookMiddlewareTests
    {
        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication(new FacebookOptions
                    {
                        AppId = "Test App Id",
                        AppSecret = "Test App Secret",
                        Events = new OAuthEvents
                        {
                            OnRedirectToAuthorizationEndpoint = context =>
                            {
                                context.Response.Redirect(context.RedirectUri + "&custom=test");
                                return Task.FromResult(0);
                            }
                        }
                    });
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AuthenticationScheme = "External",
                        AutomaticAuthenticate = true
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
                    map.UseFacebookAuthentication(new FacebookOptions
                    {
                        AppId = "Test App Id",
                        AppSecret = "Test App Secret",
                        SignInScheme = "External"
                    });
                    map.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
                }),
                services => services.AddAuthentication(),
                handler: null);
            var transaction = await server.SendAsync("http://example.com/base/login");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v2.6/dialog/oauth", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri=" + UrlEncoder.Default.Encode("http://example.com/base/signin-facebook"), location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
        }

        [Fact]
        public async Task MapWillNotAffectRedirect()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication(new FacebookOptions
                    {
                        AppId = "Test App Id",
                        AppSecret = "Test App Secret",
                        SignInScheme = "External"
                    });
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
                },
                services => services.AddAuthentication(),
                handler: null);
            var transaction = await server.SendAsync("http://example.com/login");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v2.6/dialog/oauth", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri="+ UrlEncoder.Default.Encode("http://example.com/signin-facebook"), location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseFacebookAuthentication(new FacebookOptions
                    {
                        AppId = "Test App Id",
                        AppSecret = "Test App Secret"
                    });
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AuthenticationScheme = "External"
                    });
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
            Assert.Contains("https://www.facebook.com/v2.6/dialog/oauth", location);
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
            var finalUserInfoEndpoint = string.Empty;
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("FacebookTest"));
            var server = CreateServer(
                app =>
                {
                    app.UseCookieAuthentication();
                    app.UseFacebookAuthentication(new FacebookOptions
                    {
                        AppId = "Test App Id",
                        AppSecret = "Test App Secret",
                        StateDataFormat = stateFormat,
                        UserInformationEndpoint = customUserInfoEndpoint,
                        BackchannelHttpHandler = new TestHttpMessageHandler
                        {
                            Sender = req =>
                            {
                                if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == FacebookDefaults.TokenEndpoint)
                                {
                                    var res = new HttpResponseMessage(HttpStatusCode.OK);
                                    var graphResponse = JsonConvert.SerializeObject(new
                                    {
                                        access_token = "TestAuthToken"
                                    });
                                    res.Content = new StringContent(graphResponse, Encoding.UTF8);
                                    return res;
                                }
                                if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) ==
                                    new Uri(customUserInfoEndpoint).GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped))
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
                        }
                    });
                },
                services =>
                {
                    services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
                }, handler: null);

            var properties = new AuthenticationProperties();
            var correlationKey = ".xsrf";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-facebook?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Facebook.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(1, finalUserInfoEndpoint.Count(c => c == '?'));
            Assert.Contains("fields=email,timezone,picture", finalUserInfoEndpoint);
            Assert.Contains("&access_token=", finalUserInfoEndpoint);
        }

        private static TestServer CreateServer(Action<IApplicationBuilder> configure, Action<IServiceCollection> configureServices, Func<HttpContext, bool> handler)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
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
                })
                .ConfigureServices(configureServices);
            return new TestServer(builder);
        }
    }
}
