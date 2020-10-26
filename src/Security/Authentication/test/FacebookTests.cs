// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Facebook
{
    public class FacebookTests : RemoteAuthenticationTests<FacebookOptions>
    {
        protected override string DefaultScheme => FacebookDefaults.AuthenticationScheme;
        protected override Type HandlerType => typeof(FacebookHandler);
        protected override bool SupportsSignIn { get => false; }
        protected override bool SupportsSignOut { get => false; }

        protected override void RegisterAuth(AuthenticationBuilder services, Action<FacebookOptions> configure)
        {
            services.AddFacebook(o =>
            {
                ConfigureDefaults(o);
                configure.Invoke(o);
            });
        }
 
        protected override void ConfigureDefaults(FacebookOptions o)
        {
            o.AppId = "whatever";
            o.AppSecret = "whatever";
            o.SignInScheme = "auth1";
        }

        [Fact]
        public async Task ThrowsIfAppIdMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddAuthentication().AddFacebook(o => o.SignInScheme = "Whatever"),
                async context =>
                {
                    await Assert.ThrowsAsync<ArgumentException>("AppId", () => context.ChallengeAsync("Facebook"));
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ThrowsIfAppSecretMissing()
        {
            var server = CreateServer(
                app => { },
                services => services.AddAuthentication().AddFacebook(o => o.AppId = "Whatever"),
                async context =>
                {
                    await Assert.ThrowsAsync<ArgumentException>("AppSecret", () => context.ChallengeAsync("Facebook"));
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(
                app =>
                {
                    app.UseAuthentication();
                },
                services =>
                {
                    services.AddAuthentication("External")
                        .AddCookie("External", o => { })
                        .AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                        o.Events = new OAuthEvents
                        {
                            OnRedirectToAuthorizationEndpoint = context =>
                            {
                                context.Response.Redirect(context.RedirectUri + "&custom=test");
                                return Task.FromResult(0);
                            }
                        };
                    });
                },
                async context =>
                {
                    await context.ChallengeAsync("Facebook");
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("custom=test", query);
        }

        [Fact]
        public async Task ChallengeWillIncludeScopeAsConfigured()
        {
            var server = CreateServer(
                app => app.UseAuthentication(),
                services =>
                {
                    services.AddAuthentication().AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                        o.Scope.Clear();
                        o.Scope.Add("foo");
                        o.Scope.Add("bar");
                    });
                },
                async context =>
                {
                    await context.ChallengeAsync(FacebookDefaults.AuthenticationScheme);
                    return true;
                });

            var transaction = await server.SendAsync("http://example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=foo,bar", res.Headers.Location.Query);
        }

        [Fact]
        public async Task ChallengeWillIncludeScopeAsOverwritten()
        {
            var server = CreateServer(
                app => app.UseAuthentication(),
                services =>
                {
                    services.AddAuthentication().AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                        o.Scope.Clear();
                        o.Scope.Add("foo");
                        o.Scope.Add("bar");
                    });
                },
                async context =>
                {
                    var properties = new OAuthChallengeProperties();
                    properties.SetScope("baz", "qux");
                    await context.ChallengeAsync(FacebookDefaults.AuthenticationScheme, properties);
                    return true;
                });

            var transaction = await server.SendAsync("http://example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz,qux", res.Headers.Location.Query);
        }

        [Fact]
        public async Task ChallengeWillIncludeScopeAsOverwrittenWithBaseAuthenticationProperties()
        {
            var server = CreateServer(
                app => app.UseAuthentication(),
                services =>
                {
                    services.AddAuthentication().AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                        o.Scope.Clear();
                        o.Scope.Add("foo");
                        o.Scope.Add("bar");
                    });
                },
                async context =>
                {
                    var properties = new AuthenticationProperties();
                    properties.SetParameter(OAuthChallengeProperties.ScopeKey, new string[] { "baz", "qux" });
                    await context.ChallengeAsync(FacebookDefaults.AuthenticationScheme, properties);
                    return true;
                });

            var transaction = await server.SendAsync("http://example.com/challenge");
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("scope=baz,qux", res.Headers.Location.Query);
        }

        [Fact]
        public async Task NestedMapWillNotAffectRedirect()
        {
            var server = CreateServer(app => app.Map("/base", map =>
            {
                map.UseAuthentication();
                map.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
            }),
            services =>
            {
                services.AddAuthentication()
                    .AddCookie("External", o => { })
                    .AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                });
            },
            handler: null);

            var transaction = await server.SendAsync("http://example.com/base/login");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v4.0/dialog/oauth", location);
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
                    app.UseAuthentication();
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
                },
                services =>
                {
                    services.AddAuthentication()
                        .AddCookie("External", o => { })
                        .AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                        o.SignInScheme = "External";
                    });
                },
                handler: null);
            var transaction = await server.SendAsync("http://example.com/login");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v4.0/dialog/oauth", location);
            Assert.Contains("response_type=code", location);
            Assert.Contains("client_id=", location);
            Assert.Contains("redirect_uri=" + UrlEncoder.Default.Encode("http://example.com/signin-facebook"), location);
            Assert.Contains("scope=", location);
            Assert.Contains("state=", location);
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(
                app => app.UseAuthentication(),
                services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultSignInScheme = "External";
                    })
                        .AddCookie()
                        .AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                    });
                },
                async context =>
                {
                    await context.ChallengeAsync("Facebook");
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://www.facebook.com/v4.0/dialog/oauth", location);
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
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("FacebookTest"));
            var server = CreateServer(
                app => app.UseAuthentication(),
                services =>
                {
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie()
                        .AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                        o.StateDataFormat = stateFormat;
                        o.UserInformationEndpoint = customUserInfoEndpoint;
                        o.BackchannelHttpHandler = new TestHttpMessageHandler
                        {
                            Sender = req =>
                            {
                                if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == FacebookDefaults.TokenEndpoint)
                                {
                                    var res = new HttpResponseMessage(HttpStatusCode.OK);
                                    var graphResponse = "{ \"access_token\": \"TestAuthToken\" }";
                                    res.Content = new StringContent(graphResponse, Encoding.UTF8);
                                    return res;
                                }
                                if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) ==
                                    new Uri(customUserInfoEndpoint).GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped))
                                {
                                    finalUserInfoEndpoint = req.RequestUri.ToString();
                                    var res = new HttpResponseMessage(HttpStatusCode.OK);
                                    var graphResponse = "{ \"id\": \"TestProfileId\", \"name\": \"TestName\" }";
                                    res.Content = new StringContent(graphResponse, Encoding.UTF8);
                                    return res;
                                }
                                return null;
                            }
                        };
                    });
                },
                handler: null);

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

        private static TestServer CreateServer(Action<IApplicationBuilder> configure, Action<IServiceCollection> configureServices, Func<HttpContext, Task<bool>> handler)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    configure?.Invoke(app);
                    app.Use(async (context, next) =>
                    {
                        if (handler == null || !await handler(context))
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
