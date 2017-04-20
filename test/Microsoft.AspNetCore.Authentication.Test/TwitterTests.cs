// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    public class TwitterTests
    {
        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection().AddTwitterAuthentication().AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(TwitterDefaults.AuthenticationScheme);
            Assert.NotNull(scheme);
            Assert.Equal("TwitterHandler", scheme.HandlerType.Name);
            Assert.Equal(TwitterDefaults.AuthenticationScheme, scheme.DisplayName);
        }

        [Fact]
        public void AddCanBindAgainstDefaultConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"Twitter:ConsumerKey", "<key>"},
                {"Twitter:ConsumerSecret", "<secret>"},
                {"Twitter:BackchannelTimeout", "0.0:0:30"},
                //{"Twitter:CallbackPath", "/callbackpath"}, // PathString doesn't convert
                {"Twitter:ClaimsIssuer", "<issuer>"},
                {"Twitter:DisplayName", "<display>"},
                {"Twitter:RemoteAuthenticationTimeout", "0.0:0:30"},
                {"Twitter:SaveTokens", "true"},
                {"Twitter:SendAppSecretProof", "true"},
                {"Twitter:SignInScheme", "<signIn>"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection().AddTwitterAuthentication().AddSingleton<IConfiguration>(config);
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptionsSnapshot<TwitterOptions>>().Get(TwitterDefaults.AuthenticationScheme);
            Assert.Equal(new TimeSpan(0, 0, 0, 30), options.BackchannelTimeout);
            //Assert.Equal("/callbackpath", options.CallbackPath); // NOTE: PathString doesn't convert
            Assert.Equal("<issuer>", options.ClaimsIssuer);
            Assert.Equal("<key>", options.ConsumerKey);
            Assert.Equal("<secret>", options.ConsumerSecret);
            Assert.Equal("<display>", options.DisplayName);
            Assert.Equal(new TimeSpan(0, 0, 0, 30), options.RemoteAuthenticationTimeout);
            Assert.True(options.SaveTokens);
            Assert.Equal("<signIn>", options.SignInScheme);
        }

        [Fact]
        public void AddWithDelegateIgnoresConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"Twitter:ConsumerKey", "<key>"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection().AddTwitterAuthentication(o => o.SaveTokens = true).AddSingleton<IConfiguration>(config);
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptionsSnapshot<TwitterOptions>>().Get(TwitterDefaults.AuthenticationScheme);
            Assert.Null(options.ConsumerKey);
            Assert.True(options.SaveTokens);
        }

        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.Events = new TwitterEvents
                {
                    OnRedirectToAuthorizationEndpoint = context =>
                    {
                        context.Response.Redirect(context.RedirectUri + "&custom=test");
                        return Task.FromResult(0);
                    }
                };
                o.BackchannelHttpHandler = new TestHttpMessageHandler
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
            },
            context => 
            {
                // REVIEW: Gross
                context.ChallengeAsync("Twitter").GetAwaiter().GetResult();
                return true;
            });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("custom=test", query);
        }

        [Fact]
        public async Task BadSignInWillThrow()
        {
            var server = CreateServer(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });

            // Send a bogus sign in
            var error = await Assert.ThrowsAnyAsync<Exception>(() => server.SendAsync("https://example.com/signin-twitter"));
            Assert.Equal("Invalid state cookie.", error.GetBaseException().Message);
        }

        [Fact]
        public async Task SignInThrows()
        {
            var server = CreateServer(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signIn");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task SignOutThrows()
        {
            var server = CreateServer(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ForbidThrows()
        {
            var server = CreateServer(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }


        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
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
            },
                context =>
                {
                    // REVIEW: gross
                    context.ChallengeAsync("Twitter").GetAwaiter().GetResult();
                    return true;
                });
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://api.twitter.com/oauth/authenticate?oauth_token=", location);
        }

        private static TestServer CreateServer(Action<TwitterOptions> options, Func<HttpContext, bool> handler = null)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;
                        if (req.Path == new PathString("/signIn"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.SignInAsync("Twitter", new ClaimsPrincipal()));
                        }
                        else if (req.Path == new PathString("/signOut"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.SignOutAsync("Twitter"));
                        }
                        else if (req.Path == new PathString("/forbid"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.ForbidAsync("Twitter"));
                        }
                        else if (handler == null || !handler(context))
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddCookieAuthentication("External", _ => { });
                    Action<TwitterOptions> wrapOptions = o =>
                    {
                        o.SignInScheme = "External";
                        options(o);
                    };
                    services.AddTwitterAuthentication(wrapOptions);
                });
            return new TestServer(builder);
        }
    }
}
