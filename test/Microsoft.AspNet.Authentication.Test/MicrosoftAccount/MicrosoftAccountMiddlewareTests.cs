// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders;
using Newtonsoft.Json;
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
                    options.Events = new OAuthEvents
                    {
                        OnRedirectToAuthorizationEndpoint = context =>
                        {
                            context.Response.Redirect(context.RedirectUri + "&custom=test");
                            return Task.FromResult(0);
                        }
                    };
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("custom=test", query);
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
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
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
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
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
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
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
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://login.live.com/oauth20_authorize.srf", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri=", location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
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
                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = context =>
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
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains(correlationKey, transaction.SetCookie[0]);
            Assert.Contains(".AspNet." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/me", authCookie);
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.Equal("Test Refresh Token", transaction.FindClaimValue("RefreshToken"));
        }

        private static TestServer CreateServer(Action<MicrosoftAccountOptions> configureOptions)
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
