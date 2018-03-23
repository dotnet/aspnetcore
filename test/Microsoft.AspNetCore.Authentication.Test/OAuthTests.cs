// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    public class OAuthTests
    {
        [Fact]
        public async Task CanForwardDefault()
        {
            var services = new ServiceCollection().AddLogging();

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = "default";
                o.AddScheme<TestHandler>("auth1", "auth1");
            })
            .AddOAuth("default", o =>
            {
                ConfigureDefaults(o);
                o.SignInScheme = "auth1";
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
                o.DefaultScheme = "default";
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddOAuth("default", o =>
            {
                ConfigureDefaults(o);
                o.SignInScheme = "auth1";
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
                o.DefaultScheme = "default";
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddOAuth("default", o =>
            {
                ConfigureDefaults(o);
                o.SignInScheme = "auth1";
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
                o.DefaultScheme = "default";
                o.DefaultSignInScheme = "auth1";
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddOAuth("default", o =>
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
                o.DefaultScheme = "default";
                o.DefaultSignInScheme = "auth1";
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddOAuth("default", o =>
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
                o.DefaultScheme = "default";
                o.DefaultSignInScheme = "auth1";
                o.AddScheme<TestHandler>("specific", "specific");
                o.AddScheme<TestHandler2>("auth1", "auth1");
            })
            .AddOAuth("default", o =>
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
                o.DefaultScheme = "default";
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddOAuth("default", o =>
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
                o.DefaultScheme = "default";
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddOAuth("default", o =>
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
                o.DefaultScheme = "default";
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            })
            .AddOAuth("default", o =>
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
            var server = CreateServer(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.SignInScheme = "weeblie";
                    o.ClientId = "whatever";
                    o.ClientSecret = "whatever";
                    o.CallbackPath = "/whatever";
                    o.AuthorizationEndpoint = "/whatever";
                    o.TokenEndpoint = "/whatever";
                }));
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("https://example.com/"));
            Assert.Contains("cannot be set to itself", error.Message);
        }

        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddOAuth("oauth", o => { });
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync("oauth");
            Assert.NotNull(scheme);
            Assert.Equal("OAuthHandler`1", scheme.HandlerType.Name);
            Assert.Equal(OAuthDefaults.DisplayName, scheme.DisplayName);
        }

        [Fact]
        public async Task ThrowsIfClientIdMissing()
        {
            var server = CreateServer(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.SignInScheme = "whatever";
                    o.CallbackPath = "/";
                    o.ClientSecret = "whatever";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                }));
            await Assert.ThrowsAsync<ArgumentException>("ClientId", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfClientSecretMissing()
        {
            var server = CreateServer(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.SignInScheme = "whatever";
                    o.ClientId = "Whatever;";
                    o.CallbackPath = "/";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                }));
            await Assert.ThrowsAsync<ArgumentException>("ClientSecret", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfCallbackPathMissing()
        {
            var server = CreateServer(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                    o.SignInScheme = "eh";
                }));
            await Assert.ThrowsAsync<ArgumentException>("CallbackPath", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfTokenEndpointMissing()
        {
            var server = CreateServer(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.CallbackPath = "/";
                    o.AuthorizationEndpoint = "/";
                    o.SignInScheme = "eh";
                }));
            await Assert.ThrowsAsync<ArgumentException>("TokenEndpoint", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfAuthorizationEndpointMissing()
        {
            var server = CreateServer(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.CallbackPath = "/";
                    o.TokenEndpoint = "/";
                    o.SignInScheme = "eh";
                }));
            await Assert.ThrowsAsync<ArgumentException>("AuthorizationEndpoint", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task RedirectToIdentityProvider_SetsCorrelationIdCookiePath_ToCallBackPath()
        {
            var server = CreateServer(
                s => s.AddAuthentication().AddOAuth(
                    "Weblie",
                    opt =>
                    {
                        ConfigureDefaults(opt);
                    }),
                async ctx =>
                {
                    await ctx.ChallengeAsync("Weblie");
                    return true;
                });

            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
            var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
            var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation."));
            Assert.Contains("path=/oauth-callback", correlation);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_CorrelationIdCookieOptions_CanBeOverriden()
        {
            var server = CreateServer(
                s => s.AddAuthentication().AddOAuth(
                    "Weblie",
                    opt =>
                    {
                        ConfigureDefaults(opt);
                        opt.CorrelationCookie.Path = "/";
                    }),
                async ctx =>
                {
                    await ctx.ChallengeAsync("Weblie");
                    return true;
                });

            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
            var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
            var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation."));
            Assert.Contains("path=/", correlation);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_HasScopeAsConfigured()
        {
            var server = CreateServer(
                s => s.AddAuthentication().AddOAuth(
                    "Weblie",
                    opt =>
                    {
                        ConfigureDefaults(opt);
                        opt.Scope.Clear();
                        opt.Scope.Add("foo");
                        opt.Scope.Add("bar");
                    }),
                async ctx =>
                {
                    await ctx.ChallengeAsync("Weblie");
                    return true;
                });

            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=foo%20bar", res.Headers.Location.Query);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_HasScopeAsOverwritten()
        {
            var server = CreateServer(
                s => s.AddAuthentication().AddOAuth(
                    "Weblie",
                    opt =>
                    {
                        ConfigureDefaults(opt);
                        opt.Scope.Clear();
                        opt.Scope.Add("foo");
                        opt.Scope.Add("bar");
                    }),
                async ctx =>
                {
                    var properties = new OAuthChallengeProperties();
                    properties.SetScope("baz", "qux");
                    await ctx.ChallengeAsync("Weblie", properties);
                    return true;
                });

            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_HasScopeAsOverwrittenWithBaseAuthenticationProperties()
        {
            var server = CreateServer(
                s => s.AddAuthentication().AddOAuth(
                    "Weblie",
                    opt =>
                    {
                        ConfigureDefaults(opt);
                        opt.Scope.Clear();
                        opt.Scope.Add("foo");
                        opt.Scope.Add("bar");
                    }),
                async ctx =>
                {
                    var properties = new AuthenticationProperties();
                    properties.SetParameter(OAuthChallengeProperties.ScopeKey, new string[] { "baz", "qux" });
                    await ctx.ChallengeAsync("Weblie", properties);
                    return true;
                });

            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
        }

        private void ConfigureDefaults(OAuthOptions o)
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "secret";
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.AuthorizationEndpoint = "https://example.com/provider/login";
            o.TokenEndpoint = "https://example.com/provider/token";
            o.CallbackPath = "/oauth-callback";
        }

        [Fact]
        public async Task RemoteAuthenticationFailed_OAuthError_IncludesProperties()
        {
            var server = CreateServer(
                s => s.AddAuthentication().AddOAuth(
                    "Weblie",
                    opt =>
                    {
                        opt.ClientId = "Test Id";
                        opt.ClientSecret = "secret";
                        opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        opt.AuthorizationEndpoint = "https://example.com/provider/login";
                        opt.TokenEndpoint = "https://example.com/provider/token";
                        opt.CallbackPath = "/oauth-callback";
                        opt.StateDataFormat = new TestStateDataFormat();
                        opt.Events = new OAuthEvents()
                        {
                            OnRemoteFailure = context =>
                            {
                                Assert.Contains("declined", context.Failure.Message);
                                Assert.Equal("testvalue", context.Properties.Items["testkey"]);
                                context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    }));

            var transaction = await server.SendAsync("https://www.example.com/oauth-callback?error=declined&state=protected_state",
                ".AspNetCore.Correlation.Weblie.corrilationId=N");

            Assert.Equal(HttpStatusCode.NotAcceptable, transaction.Response.StatusCode);
            Assert.Null(transaction.Response.Headers.Location);
        }

        private static TestServer CreateServer(Action<IServiceCollection> configureServices, Func<HttpContext, Task<bool>> handler = null)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        if (handler == null || ! await handler(context))
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(configureServices);
            return new TestServer(builder);
        }

        private class TestStateDataFormat : ISecureDataFormat<AuthenticationProperties>
        {
            private AuthenticationProperties Data { get; set; }

            public string Protect(AuthenticationProperties data)
            {
                return "protected_state";
            }

            public string Protect(AuthenticationProperties data, string purpose)
            {
                throw new NotImplementedException();
            }

            public AuthenticationProperties Unprotect(string protectedText)
            {
                Assert.Equal("protected_state", protectedText);
                var properties = new AuthenticationProperties(new Dictionary<string, string>()
                {
                    { ".xsrf", "corrilationId" },
                    { "testkey", "testvalue" }
                });
                properties.RedirectUri = "http://testhost/redirect";
                return properties;
            }

            public AuthenticationProperties Unprotect(string protectedText, string purpose)
            {
                throw new NotImplementedException();
            }
        }
    }
}
