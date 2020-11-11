// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    public class OAuthTests : RemoteAuthenticationTests<OAuthOptions>
    {
        protected override string DefaultScheme => OAuthDefaults.DisplayName;
        protected override Type HandlerType => typeof(OAuthHandler<OAuthOptions>);
        protected override bool SupportsSignIn { get => false; }
        protected override bool SupportsSignOut { get => false; }

        protected override void RegisterAuth(AuthenticationBuilder services, Action<OAuthOptions> configure)
        {
            services.AddOAuth(DefaultScheme, o =>
            {
                ConfigureDefaults(o);
                configure.Invoke(o);
            });
        }

        [Fact]
        public async Task ThrowsIfClientIdMissing()
        {
            using var host = await CreateHost(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.SignInScheme = "whatever";
                    o.CallbackPath = "/";
                    o.ClientSecret = "whatever";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                }));
            using var server = host.GetTestServer();
            await Assert.ThrowsAsync<ArgumentException>("ClientId", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfClientSecretMissing()
        {
            using var host = await CreateHost(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.SignInScheme = "whatever";
                    o.ClientId = "Whatever;";
                    o.CallbackPath = "/";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                }));
            using var server = host.GetTestServer();
            await Assert.ThrowsAsync<ArgumentException>("ClientSecret", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfCallbackPathMissing()
        {
            using var host = await CreateHost(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.TokenEndpoint = "/";
                    o.AuthorizationEndpoint = "/";
                    o.SignInScheme = "eh";
                }));
            using var server = host.GetTestServer();
            await Assert.ThrowsAsync<ArgumentException>("CallbackPath", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfTokenEndpointMissing()
        {
            using var host = await CreateHost(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.CallbackPath = "/";
                    o.AuthorizationEndpoint = "/";
                    o.SignInScheme = "eh";
                }));
            using var server = host.GetTestServer();
            await Assert.ThrowsAsync<ArgumentException>("TokenEndpoint", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task ThrowsIfAuthorizationEndpointMissing()
        {
            using var host = await CreateHost(
                services => services.AddAuthentication().AddOAuth("weeblie", o =>
                {
                    o.ClientId = "Whatever;";
                    o.ClientSecret = "Whatever;";
                    o.CallbackPath = "/";
                    o.TokenEndpoint = "/";
                    o.SignInScheme = "eh";
                }));
            using var server = host.GetTestServer();
            await Assert.ThrowsAsync<ArgumentException>("AuthorizationEndpoint", () => server.SendAsync("http://example.com/"));
        }

        [Fact]
        public async Task RedirectToIdentityProvider_SetsCorrelationIdCookiePath_ToCallBackPath()
        {
            using var host = await CreateHost(
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

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
            var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
            var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
            Assert.Contains("path=/oauth-callback", correlation);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_CorrelationIdCookieOptions_CanBeOverriden()
        {
            using var host = await CreateHost(
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

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
            var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
            var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
            Assert.Contains("path=/", correlation);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_HasScopeAsConfigured()
        {
            using var host = await CreateHost(
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

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=foo%20bar", res.Headers.Location.Query);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_HasScopeAsOverwritten()
        {
            using var host = await CreateHost(
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

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
        }

        [Fact]
        public async Task RedirectToAuthorizeEndpoint_HasScopeAsOverwrittenWithBaseAuthenticationProperties()
        {
            using var host = await CreateHost(
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

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
        }

        protected override void ConfigureDefaults(OAuthOptions o)
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "secret";
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.AuthorizationEndpoint = "https://example.com/provider/login";
            o.TokenEndpoint = "https://example.com/provider/token";
            o.CallbackPath = "/oauth-callback";
        }

        [Fact]
        public async Task HandleRequestAsync_RedirectsToAccessDeniedPathWhenExplicitlySet()
        {
            using var host = await CreateHost(
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
                        opt.AccessDeniedPath = "/access-denied";
                        opt.StateDataFormat = new TestStateDataFormat();
                        opt.Events.OnRemoteFailure = context => throw new InvalidOperationException("This event should not be called.");
                    }));

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/oauth-callback?error=access_denied&state=protected_state",
                ".AspNetCore.Correlation.correlationId=N");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("https://www.example.com/access-denied?ReturnUrl=http%3A%2F%2Ftesthost%2Fredirect", transaction.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task HandleRequestAsync_InvokesAccessDeniedEvent()
        {
            using var host = await CreateHost(
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
                            OnAccessDenied = context =>
                            {
                                Assert.Equal("testvalue", context.Properties.Items["testkey"]);
                                context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    }));

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/oauth-callback?error=access_denied&state=protected_state",
                ".AspNetCore.Correlation.correlationId=N");

            Assert.Equal(HttpStatusCode.NotAcceptable, transaction.Response.StatusCode);
            Assert.Null(transaction.Response.Headers.Location);
        }

        [Fact]
        public async Task HandleRequestAsync_InvokesRemoteFailureEventWhenAccessDeniedPathIsNotExplicitlySet()
        {
            using var host = await CreateHost(
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
                                Assert.Equal("Access was denied by the resource owner or by the remote server.", context.Failure.Message);
                                Assert.Equal("testvalue", context.Properties.Items["testkey"]);
                                context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    }));

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/oauth-callback?error=access_denied&state=protected_state",
                ".AspNetCore.Correlation.correlationId=N");

            Assert.Equal(HttpStatusCode.NotAcceptable, transaction.Response.StatusCode);
            Assert.Null(transaction.Response.Headers.Location);
        }

        [Fact]
        public async Task RemoteAuthenticationFailed_OAuthError_IncludesProperties()
        {
            using var host = await CreateHost(
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
                                Assert.Contains("custom_error", context.Failure.Message);
                                Assert.Equal("testvalue", context.Properties.Items["testkey"]);
                                context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    }));

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://www.example.com/oauth-callback?error=custom_error&state=protected_state",
                ".AspNetCore.Correlation.correlationId=N");

            Assert.Equal(HttpStatusCode.NotAcceptable, transaction.Response.StatusCode);
            Assert.Null(transaction.Response.Headers.Location);
        }

        private static async Task<IHost> CreateHost(Action<IServiceCollection> configureServices, Func<HttpContext, Task<bool>> handler = null)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(builder =>
                    builder.UseTestServer()
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
                        .ConfigureServices(configureServices))
                    .Build();
            await host.StartAsync();
            return host;
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
                    { ".xsrf", "correlationId" },
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
