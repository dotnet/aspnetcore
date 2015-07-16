// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.Twitter
{
    public class TwitterMiddlewareTests
    {
        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(options =>
                {
                    options.ConsumerKey = "Test Consumer Key";
                    options.ConsumerSecret = "Test Consumer Secret";
                    options.Notifications = new TwitterAuthenticationNotifications
                    {
                        OnApplyRedirect = context =>
                        {
                            context.Response.Redirect(context.RedirectUri + "&custom=test");
                        }
                    };
                    options.BackchannelHttpHandler = new TestHttpMessageHandler
                    {
                        Sender = req =>
                        {
                            if (req.RequestUri.AbsoluteUri == "https://api.twitter.com/oauth/request_token")
                            {
                                return new HttpResponseMessage(HttpStatusCode.OK)
                                {
                                    Content =
                                        new StringContent("oauth_callback_confirmed=true&oauth_token=test_oauth_token&oauth_token_secret=test_oauth_token_secret",
                                            Encoding.UTF8,
                                            "application/x-www-form-urlencoded")
                                };
                            }
                            return null;
                        }
                    };
                    options.BackchannelCertificateValidator = null;
                },
                context => 
                {
                    // REVIEW: Gross
                    context.Authentication.ChallengeAsync("Twitter").GetAwaiter().GetResult();
                    return true;
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
                options.ConsumerKey = "Test Consumer Key";
                options.ConsumerSecret = "Test Consumer Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signIn");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SignOutThrows()
        {
            var server = CreateServer(options =>
            {
                options.ConsumerKey = "Test Consumer Key";
                options.ConsumerSecret = "Test Consumer Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ForbidThrows()
        {
            var server = CreateServer(options =>
            {
                options.ConsumerKey = "Test Consumer Key";
                options.ConsumerSecret = "Test Consumer Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }


        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(options =>
                {
                    options.ConsumerKey = "Test Consumer Key";
                    options.ConsumerSecret = "Test Consumer Secret";
                    options.BackchannelHttpHandler = new TestHttpMessageHandler
                    {
                        Sender = req =>
                        {
                            if (req.RequestUri.AbsoluteUri == "https://api.twitter.com/oauth/request_token")
                            {
                                return new HttpResponseMessage(HttpStatusCode.OK)
                                {
                                    Content =
                                        new StringContent("oauth_callback_confirmed=true&oauth_token=test_oauth_token&oauth_token_secret=test_oauth_token_secret",
                                            Encoding.UTF8,
                                            "application/x-www-form-urlencoded")
                                };
                            }
                            return null;
                        }
                    };
                    options.BackchannelCertificateValidator = null;
                },
                context =>
                {
                    // REVIEW: gross
                    context.Authentication.ChallengeAsync("Twitter").GetAwaiter().GetResult();
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            location.ShouldContain("https://twitter.com/oauth/authenticate?oauth_token=");
        }

        private static TestServer CreateServer(Action<TwitterAuthenticationOptions> configure, Func<HttpContext, bool> handler = null)
        {
            return TestServer.Create(app =>
            {
                app.UseCookieAuthentication(options =>
                {
                    options.AuthenticationScheme = "External";
                });
                app.UseTwitterAuthentication(configure);
                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    if (req.Path == new PathString("/signIn"))
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.SignInAsync("Twitter", new ClaimsPrincipal()));
                    }
                    else if (req.Path == new PathString("/signOut"))
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.SignOutAsync("Twitter"));
                    }
                    else if (req.Path == new PathString("/forbid"))
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.ForbidAsync("Twitter"));
                    }
                    else if (handler == null || !handler(context))
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
                    options.SignInScheme = "External";
                });
            });
        }
    }
}
