// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Tests.MicrosoftAccount
{
    public class MicrosoftAccountTests
    {
        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection().AddMicrosoftAccountAuthentication().AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(MicrosoftAccountDefaults.AuthenticationScheme);
            Assert.NotNull(scheme);
            Assert.Equal("MicrosoftAccountHandler", scheme.HandlerType.Name);
            Assert.Equal(MicrosoftAccountDefaults.AuthenticationScheme, scheme.DisplayName);
        }

        [Fact]
        public void AddCanBindAgainstDefaultConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"Microsoft:ClientId", "<id>"},
                {"Microsoft:ClientSecret", "<secret>"},
                {"Microsoft:AuthorizationEndpoint", "<authEndpoint>"},
                {"Microsoft:BackchannelTimeout", "0.0:0:30"},
                //{"Microsoft:CallbackPath", "/callbackpath"}, // PathString doesn't convert
                {"Microsoft:ClaimsIssuer", "<issuer>"},
                {"Microsoft:DisplayName", "<display>"},
                {"Microsoft:RemoteAuthenticationTimeout", "0.0:0:30"},
                {"Microsoft:SaveTokens", "true"},
                {"Microsoft:SendAppSecretProof", "true"},
                {"Microsoft:SignInScheme", "<signIn>"},
                {"Microsoft:TokenEndpoint", "<tokenEndpoint>"},
                {"Microsoft:UserInformationEndpoint", "<userEndpoint>"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection().AddMicrosoftAccountAuthentication().AddSingleton<IConfiguration>(config);
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptionsSnapshot<MicrosoftAccountOptions>>().Get(MicrosoftAccountDefaults.AuthenticationScheme);
            Assert.Equal("<authEndpoint>", options.AuthorizationEndpoint);
            Assert.Equal(new TimeSpan(0, 0, 0, 30), options.BackchannelTimeout);
            //Assert.Equal("/callbackpath", options.CallbackPath); // NOTE: PathString doesn't convert
            Assert.Equal("<issuer>", options.ClaimsIssuer);
            Assert.Equal("<id>", options.ClientId);
            Assert.Equal("<secret>", options.ClientSecret);
            Assert.Equal("<display>", options.DisplayName);
            Assert.Equal(new TimeSpan(0, 0, 0, 30), options.RemoteAuthenticationTimeout);
            Assert.True(options.SaveTokens);
            Assert.Equal("<signIn>", options.SignInScheme);
            Assert.Equal("<tokenEndpoint>", options.TokenEndpoint);
            Assert.Equal("<userEndpoint>", options.UserInformationEndpoint);
        }

        [Fact]
        public void AddWithDelegateIgnoresConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"Microsoft:ClientId", "<id>"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection().AddMicrosoftAccountAuthentication(o => o.SaveTokens = true).AddSingleton<IConfiguration>(config);
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptionsSnapshot<MicrosoftAccountOptions>>().Get(MicrosoftAccountDefaults.AuthenticationScheme);
            Assert.Null(options.ClientId);
            Assert.True(options.SaveTokens);
        }

        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Client Id";
                o.ClientSecret = "Test Client Secret";
                o.Events = new OAuthEvents
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
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signIn");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task SignOutThrows()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ForbidThrows()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri=", location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
        }

        [Fact]
        public async Task AuthenticatedEventCanGetRefreshToken()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("MsftTest"));
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Client Id";
                o.ClientSecret = "Test Client Secret";
                o.StateDataFormat = stateFormat;
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://login.microsoftonline.com/common/oauth2/v2.0/token")
                        {
                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expire_in = 3600,
                                token_type = "Bearer",
                                refresh_token = "Test Refresh Token"
                            });
                        }
                        else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://graph.microsoft.com/v1.0/me")
                        {
                            return ReturnJsonResponse(new
                            {
                                id = "Test User ID",
                                displayName = "Test Name",
                                givenName = "Test Given Name",
                                surname = "Test Family Name",
                                mail = "Test email"
                            });
                        }

                        return null;
                    }
                };
                o.Events = new OAuthEvents
                {
                    OnCreatingTicket = context =>
                    {
                        var refreshToken = context.RefreshToken;
                        context.Ticket.Principal.AddIdentity(new ClaimsIdentity(new Claim[] { new Claim("RefreshToken", refreshToken, ClaimValueTypes.String, "Microsoft") }, "Microsoft"));
                        return Task.FromResult<object>(null);
                    }
                };
            });
            var properties = new AuthenticationProperties();
            var correlationKey = ".xsrf";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-microsoft?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Microsoft.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Microsoft.{correlationValue}", transaction.SetCookie[0]);
            Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/me", authCookie);
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.Equal("Test Refresh Token", transaction.FindClaimValue("RefreshToken"));
        }

        private static TestServer CreateServer(Action<MicrosoftAccountOptions> configureOptions)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;
                        if (req.Path == new PathString("/challenge"))
                        {
                            await context.ChallengeAsync("Microsoft");
                        }
                        else if (req.Path == new PathString("/me"))
                        {
                            res.Describe(context.User);
                        }
                        else if (req.Path == new PathString("/signIn"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.SignInAsync("Microsoft", new ClaimsPrincipal()));
                        }
                        else if (req.Path == new PathString("/signOut"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.SignOutAsync("Microsoft"));
                        }
                        else if (req.Path == new PathString("/forbid"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.ForbidAsync("Microsoft"));
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(o =>
                    {
                        o.DefaultAuthenticateScheme = TestExtensions.CookieAuthenticationScheme;
                        o.DefaultSignInScheme = TestExtensions.CookieAuthenticationScheme;
                    });
                    services.AddCookieAuthentication(TestExtensions.CookieAuthenticationScheme, o => { });
                    services.AddMicrosoftAccountAuthentication(configureOptions);
                });
            return new TestServer(builder);
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
