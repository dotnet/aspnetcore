// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Tests.MicrosoftAccount
{
    public class MicrosoftAccountTests : RemoteAuthenticationTests<MicrosoftAccountOptions>
    {
        protected override string DefaultScheme => MicrosoftAccountDefaults.AuthenticationScheme;
        protected override Type HandlerType => typeof(MicrosoftAccountHandler);
        protected override bool SupportsSignIn { get => false; }
        protected override bool SupportsSignOut { get => false; }

        protected override void RegisterAuth(AuthenticationBuilder services, Action<MicrosoftAccountOptions> configure)
        {
            services.AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                configure.Invoke(o);
            });
        }

        protected override void ConfigureDefaults(MicrosoftAccountOptions o)
        {
            o.ClientId = "whatever";
            o.ClientSecret = "whatever";
            o.SignInScheme = "auth1";
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
            Assert.Contains("code_challenge=", location);
            Assert.Contains("code_challenge_method=S256", location);
        }

        [Fact]
        public async Task ChallengeWillIncludeScopeAsConfigured()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.Scope.Clear();
                o.Scope.Add("foo");
                o.Scope.Add("bar");
            });
            var transaction = await server.SendAsync("http://example.com/challenge");
            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=foo%20bar", res.Headers.Location.Query);
        }

        [Fact]
        public async Task ChallengeWillIncludeScopeAsOverwritten()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.Scope.Clear();
                o.Scope.Add("foo");
                o.Scope.Add("bar");
            });
            var transaction = await server.SendAsync("http://example.com/challengeWithOtherScope");
            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
        }

        [Fact]
        public async Task ChallengeWillIncludeScopeAsOverwrittenWithBaseAuthenticationProperties()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.Scope.Clear();
                o.Scope.Add("foo");
                o.Scope.Add("bar");
            });
            var transaction = await server.SendAsync("http://example.com/challengeWithOtherScopeWithBaseAuthenticationProperties");
            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
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
                        context.Principal.AddIdentity(new ClaimsIdentity(new Claim[] { new Claim("RefreshToken", refreshToken, ClaimValueTypes.String, "Microsoft") }, "Microsoft"));
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

        [Fact]
        public async Task ChallengeWillUseAuthenticationPropertiesParametersAsQueryArguments()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("MicrosoftTest"));
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.StateDataFormat = stateFormat;
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

            // verify query arguments
            var query = QueryHelpers.ParseQuery(transaction.Response.Headers.Location.Query);
            Assert.Equal("https://graph.microsoft.com/user.read", query["scope"]);
            Assert.Equal("consumers", query["domain_hint"]);
            Assert.Equal("username", query["login_hint"]);
            Assert.Equal("select_account", query["prompt"]);
            Assert.Equal("query", query["response_mode"]);

            // verify that the passed items were not serialized
            var stateProperties = stateFormat.Unprotect(query["state"]);
            Assert.DoesNotContain("scope", stateProperties.Items.Keys);
            Assert.DoesNotContain("domain_hint", stateProperties.Items.Keys);
            Assert.DoesNotContain("login_hint", stateProperties.Items.Keys);
            Assert.DoesNotContain("prompt", stateProperties.Items.Keys);
            Assert.DoesNotContain("response_mode", stateProperties.Items.Keys);
        }

        [Fact]
        public async Task PkceSentToTokenEndpoint()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Client Id";
                o.ClientSecret = "Test Client Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://login.microsoftonline.com/common/oauth2/v2.0/token")
                        {
                            var body = req.Content.ReadAsStringAsync().Result;
                            var form = new FormReader(body);
                            var entries = form.ReadForm();
                            Assert.Equal("Test Client Id", entries["client_id"]);
                            Assert.Equal("https://example.com/signin-microsoft", entries["redirect_uri"]);
                            Assert.Equal("Test Client Secret", entries["client_secret"]);
                            Assert.Equal("TestCode", entries["code"]);
                            Assert.Equal("authorization_code", entries["grant_type"]);
                            Assert.False(string.IsNullOrEmpty(entries["code_verifier"]));

                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expire_in = 3600,
                                token_type = "Bearer",
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
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var locationUri = transaction.Response.Headers.Location;
            Assert.StartsWith("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", locationUri.AbsoluteUri);

            var queryParams = QueryHelpers.ParseQuery(locationUri.Query);
            Assert.False(string.IsNullOrEmpty(queryParams["code_challenge"]));
            Assert.Equal("S256", queryParams["code_challenge_method"]);

            var nonceCookie = transaction.SetCookie.Single();
            nonceCookie = nonceCookie.Substring(0, nonceCookie.IndexOf(';'));

            transaction = await server.SendAsync(
                "https://example.com/signin-microsoft?code=TestCode&state=" + queryParams["state"],
                nonceCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.StartsWith(".AspNetCore.Correlation.Microsoft.", transaction.SetCookie[0]);
            Assert.StartsWith(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);
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
                            await context.ChallengeAsync("Microsoft", new MicrosoftChallengeProperties
                            {
                                Prompt = "select_account",
                                LoginHint = "username",
                                DomainHint = "consumers",
                                ResponseMode = "query",
                                RedirectUri = "/me"
                            });
                        }
                        else if (req.Path == new PathString("/challengeWithOtherScope"))
                        {
                            var properties = new OAuthChallengeProperties();
                            properties.SetScope("baz", "qux");
                            await context.ChallengeAsync("Microsoft", properties);
                        }
                        else if (req.Path == new PathString("/challengeWithOtherScopeWithBaseAuthenticationProperties"))
                        {
                            var properties = new AuthenticationProperties();
                            properties.SetParameter(OAuthChallengeProperties.ScopeKey, new string[] { "baz", "qux" });
                            await context.ChallengeAsync("Microsoft", properties);
                        }
                        else if (req.Path == new PathString("/me"))
                        {
                            await res.DescribeAsync(context.User);
                        }
                        else if (req.Path == new PathString("/signIn"))
                        {
                            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("Microsoft", new ClaimsPrincipal()));
                        }
                        else if (req.Path == new PathString("/signOut"))
                        {
                            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync("Microsoft"));
                        }
                        else if (req.Path == new PathString("/forbid"))
                        {
                            await Assert.ThrowsAsync<InvalidOperationException>(() => context.ForbidAsync("Microsoft"));
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(TestExtensions.CookieAuthenticationScheme)
                        .AddCookie(TestExtensions.CookieAuthenticationScheme, o => { })
                        .AddMicrosoftAccount(configureOptions);
                });
            return new TestServer(builder);
        }

        private static HttpResponseMessage ReturnJsonResponse(object content)
        {
            var res = new HttpResponseMessage(HttpStatusCode.OK);
            var text = Newtonsoft.Json.JsonConvert.SerializeObject(content);
            res.Content = new StringContent(text, Encoding.UTF8, "application/json");
            return res;
        }
    }
}
