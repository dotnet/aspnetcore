// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.MicrosoftAccount;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.Tests.MicrosoftAccount
{
    public class MicrosoftAccountMiddlewareTests
    {
        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(
                options =>
                {
                    options.ClientId = "Test Client Id";
                    options.ClientSecret = "Test Client Secret";
                    options.Notifications = new OAuthAuthenticationNotifications
                    {
                        OnApplyRedirect = context =>
                        {
                            context.Response.Redirect(context.RedirectUri + "&custom=test");
                        }
                    };
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("custom=test");
        }

        [Fact]
        public async Task SignInThrows()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signIn");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SignOutThrows()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ForbidThrows()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(
                options =>
                {
                    options.ClientId = "Test Client Id";
                    options.ClientSecret = "Test Client Secret";
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            location.ShouldContain("https://login.live.com/oauth20_authorize.srf");
            location.ShouldContain("response_type=code");
            location.ShouldContain("client_id=");
            location.ShouldContain("redirect_uri=");
            location.ShouldContain("scope=");
            location.ShouldContain("state=");
        }

        [Fact]
        public async Task AuthenticatedEventCanGetRefreshToken()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("MsftTest"));
            var server = CreateServer(
                options =>
                {
                    options.ClientId = "Test Client Id";
                    options.ClientSecret = "Test Client Secret";
                    options.StateDataFormat = stateFormat;
                    options.BackchannelHttpHandler = new TestHttpMessageHandler
                    {
                        Sender = req =>
                        {
                            if (req.RequestUri.AbsoluteUri == "https://login.live.com/oauth20_token.srf")
                            {
                                return ReturnJsonResponse(new
                                {
                                    access_token = "Test Access Token",
                                    expire_in = 3600,
                                    token_type = "Bearer",
                                    refresh_token = "Test Refresh Token"
                                });
                            }
                            else if (req.RequestUri.GetLeftPart(UriPartial.Path) == "https://apis.live.net/v5.0/me")
                            {
                                return ReturnJsonResponse(new
                                {
                                    id = "Test User ID",
                                    name = "Test Name",
                                    first_name = "Test Given Name",
                                    last_name = "Test Family Name",
                                    emails = new
                                    {
                                        preferred = "Test email"
                                    }
                                });
                            }

                            return null;
                        }
                    };
                    options.Notifications = new OAuthAuthenticationNotifications
                    {
                        OnAuthenticated = context =>
                        {
                            var refreshToken = context.RefreshToken;
                            context.Principal.AddIdentity(new ClaimsIdentity(new Claim[] { new Claim("RefreshToken", refreshToken, ClaimValueTypes.String, "Microsoft") }, "Microsoft"));
                            return Task.FromResult<object>(null);
                        }
                    };
                });
            var properties = new AuthenticationProperties();
            var correlationKey = ".AspNet.Correlation.Microsoft";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-microsoft?code=TestCode&state=" + UrlEncoder.Default.UrlEncode(state),
                correlationKey + "=" + correlationValue);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.ToString().ShouldBe("/me");
            transaction.SetCookie.Count.ShouldBe(2);
            transaction.SetCookie[0].ShouldContain(correlationKey);
            transaction.SetCookie[1].ShouldContain(".AspNet." + TestExtensions.CookieAuthenticationScheme);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/me", authCookie);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            transaction.FindClaimValue("RefreshToken").ShouldBe("Test Refresh Token");
        }

        private static TestServer CreateServer(Action<MicrosoftAccountAuthenticationOptions> configureOptions)
        {
            return TestServer.Create(app =>
            {
                app.UseCookieAuthentication(options =>
                {
                    options.AuthenticationScheme = TestExtensions.CookieAuthenticationScheme;
                    options.AutomaticAuthentication = true;
                });
                app.UseMicrosoftAccountAuthentication(configureOptions);

                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    if (req.Path == new PathString("/challenge"))
                    {
                        await context.Authentication.ChallengeAsync("Microsoft");
                    }
                    else if (req.Path == new PathString("/me"))
                    {
                        res.Describe(context.User);
                    }
                    else if (req.Path == new PathString("/signIn"))
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.SignInAsync("Microsoft", new ClaimsPrincipal()));
                    }
                    else if (req.Path == new PathString("/signOut"))
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.SignOutAsync("Microsoft"));
                    }
                    else if (req.Path == new PathString("/forbid"))
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.ForbidAsync("Microsoft"));
                    }
                    else
                    {
                        await next();
                    }
                });
            },
            services =>
            {
                services.AddAuthentication();
                services.Configure<SharedAuthenticationOptions>(options =>
                {
                    options.SignInScheme = TestExtensions.CookieAuthenticationScheme;
                });
            });
        }

        private static HttpResponseMessage ReturnJsonResponse(object content)
        {
            var res = new HttpResponseMessage(HttpStatusCode.OK);
            var text = JsonConvert.SerializeObject(content);
            res.Content = new StringContent(text, Encoding.UTF8, "application/json");
            return res;
        }
    }
}
