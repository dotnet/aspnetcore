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
        private void ConfigureDefaults(MicrosoftAccountOptions o)
        {
            o.ClientId = "whatever";
            o.ClientSecret = "whatever";
            o.SignInScheme = "auth1";
        }

        [Fact]
        public async Task CanForwardDefault()
        {
            var services = new ServiceCollection().AddLogging();

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler>("auth1", "auth1");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
            });

            var forwardDefault = new TestHandler();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);

            await context.AuthenticateAsync();
            Assert.Equal(1, forwardDefault.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, forwardDefault.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, forwardDefault.ChallengeCount);

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
        }

        [Fact]
        public async Task ForwardSignInThrows()
        {
            var services = new ServiceCollection().AddLogging();

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardSignOut = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
        }

        [Fact]
        public async Task ForwardSignOutThrows()
        {
            var services = new ServiceCollection().AddLogging();

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardSignOut = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
        }

        [Fact]
        public async Task ForwardForbidWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardForbid = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.ForbidAsync();
            Assert.Equal(0, specific.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(1, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
        }

        [Fact]
        public async Task ForwardAuthenticateWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardAuthenticate = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(0, specific.SignOutCount);
            Assert.Equal(1, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
        }

        [Fact]
        public async Task ForwardChallengeWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler>("specific", "specific");
                o.AddScheme<TestHandler2>("auth1", "auth1");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardChallenge = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.ChallengeAsync();
            Assert.Equal(0, specific.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(1, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
        }

        [Fact]
        public async Task ForwardSelectorWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardDefaultSelector = _ => "selector";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);
            var selector = new TestHandler3();
            services.AddSingleton(selector);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(1, selector.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, selector.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, selector.ChallengeCount);

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);
            Assert.Equal(0, specific.SignOutCount);
        }

        [Fact]
        public async Task NullForwardSelectorUsesDefault()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardDefaultSelector = _ => null;
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);
            var selector = new TestHandler3();
            services.AddSingleton(selector);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(1, forwardDefault.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, forwardDefault.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, forwardDefault.ChallengeCount);

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));

            Assert.Equal(0, selector.AuthenticateCount);
            Assert.Equal(0, selector.ForbidCount);
            Assert.Equal(0, selector.ChallengeCount);
            Assert.Equal(0, selector.SignInCount);
            Assert.Equal(0, selector.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);
            Assert.Equal(0, specific.SignOutCount);
        }

        [Fact]
        public async Task SpecificForwardWinsOverSelectorAndDefault()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddMicrosoftAccount(o =>
            {
                ConfigureDefaults(o);
                o.ForwardDefault = "auth1";
                o.ForwardDefaultSelector = _ => "selector";
                o.ForwardAuthenticate = "specific";
                o.ForwardChallenge = "specific";
                o.ForwardSignIn = "specific";
                o.ForwardSignOut = "specific";
                o.ForwardForbid = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);
            var selector = new TestHandler3();
            services.AddSingleton(selector);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(1, specific.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, specific.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, specific.ChallengeCount);

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
            Assert.Equal(0, selector.AuthenticateCount);
            Assert.Equal(0, selector.ForbidCount);
            Assert.Equal(0, selector.ChallengeCount);
            Assert.Equal(0, selector.SignInCount);
            Assert.Equal(0, selector.SignOutCount);
        }

        [Fact]
        public async Task VerifySignInSchemeCannotBeSetToSelf()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.SignInScheme = MicrosoftAccountDefaults.AuthenticationScheme;
            });
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("https://example.com/challenge"));
            Assert.Contains("cannot be set to itself", error.Message);
        }

        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddMicrosoftAccount();
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(MicrosoftAccountDefaults.AuthenticationScheme);
            Assert.NotNull(scheme);
            Assert.Equal("MicrosoftAccountHandler", scheme.HandlerType.Name);
            Assert.Equal(MicrosoftAccountDefaults.AuthenticationScheme, scheme.DisplayName);
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
                            res.Describe(context.User);
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
            var text = JsonConvert.SerializeObject(content);
            res.Content = new StringContent(text, Encoding.UTF8, "application/json");
            return res;
        }
    }
}
