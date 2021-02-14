// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    public class TwitterTests : RemoteAuthenticationTests<TwitterOptions>
    {
        protected override string DefaultScheme => TwitterDefaults.AuthenticationScheme;
        protected override Type HandlerType => typeof(TwitterHandler);
        protected override bool SupportsSignIn { get => false; }
        protected override bool SupportsSignOut { get => false; }

        protected override void RegisterAuth(AuthenticationBuilder services, Action<TwitterOptions> configure)
        {
            services.AddTwitter(o =>
            {
                ConfigureDefaults(o);
                configure.Invoke(o);
            });
        }

        protected override void ConfigureDefaults(TwitterOptions o)
        {
            o.ConsumerKey = "whatever";
            o.ConsumerSecret = "whatever";
            o.SignInScheme = "auth1";
        }

        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            using var host = await CreateHost(o =>
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
                    Sender = BackchannelRequestToken
                };
            },
            async context =>
            {
                await context.ChallengeAsync("Twitter");
                return true;
            });
            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("custom=test", query);
        }

        /// <summary>
        /// Validates the Twitter Options to check if the Consumer Key is missing in the TwitterOptions and if so throws the ArgumentException
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ThrowsIfClientIdMissing()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerSecret = "Test Consumer Secret";
            });

            using var server = host.GetTestServer();
            await Assert.ThrowsAsync<ArgumentException>("ConsumerKey", async () => await server.SendAsync("http://example.com/challenge"));
        }

        /// <summary>
        /// Validates the Twitter Options to check if the Consumer Secret is missing in the TwitterOptions and if so throws the ArgumentException
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ThrowsIfClientSecretMissing()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
            });

            using var server = host.GetTestServer();
            await Assert.ThrowsAsync<ArgumentException>("ConsumerSecret", async () => await server.SendAsync("http://example.com/challenge"));
        }

        [Fact]
        public async Task BadSignInWillThrow()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });

            // Send a bogus sign in
            using var server = host.GetTestServer();
            var error = await Assert.ThrowsAnyAsync<Exception>(() => server.SendAsync("https://example.com/signin-twitter"));
            Assert.Equal("Invalid state cookie.", error.GetBaseException().Message);
        }

        [Fact]
        public async Task SignInThrows()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });
            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://example.com/signIn");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task SignOutThrows()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });
            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ForbidThrows()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
            });
            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = BackchannelRequestToken
                };
            },
            async context =>
            {
                await context.ChallengeAsync("Twitter");
                return true;
            });
            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://api.twitter.com/oauth/authenticate?oauth_token=", location);
        }

        [Fact]
        public async Task HandleRequestAsync_RedirectsToAccessDeniedPathWhenExplicitlySet()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = BackchannelRequestToken
                };
                o.AccessDeniedPath = "/access-denied";
                o.Events.OnRemoteFailure = context => throw new InvalidOperationException("This event should not be called.");
            },
            async context =>
            {
                var properties = new AuthenticationProperties();
                properties.Items["testkey"] = "testvalue";
                await context.ChallengeAsync("Twitter", properties);
                return true;
            });
            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://api.twitter.com/oauth/authenticate?oauth_token=", location);
            Assert.True(transaction.Response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookie));
            Assert.True(SetCookieHeaderValue.TryParseList(setCookie.ToList(), out var setCookieValues));
            Assert.Single(setCookieValues);
            var setCookieValue = setCookieValues.Single();
            var cookie = new CookieHeaderValue(setCookieValue.Name, setCookieValue.Value);

            var request = new HttpRequestMessage(HttpMethod.Get, "/signin-twitter?denied=ABCDEFG");
            request.Headers.Add(HeaderNames.Cookie, cookie.ToString());
            var client = server.CreateClient();
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("http://localhost/access-denied?ReturnUrl=%2Fchallenge", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task BadCallbackCallsAccessDeniedWithState()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = BackchannelRequestToken
                };
                o.Events = new TwitterEvents()
                {
                    OnAccessDenied = context =>
                    {
                        Assert.NotNull(context.Properties);
                        Assert.Equal("testvalue", context.Properties.Items["testkey"]);
                        context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            },
            async context =>
            {
                var properties = new AuthenticationProperties();
                properties.Items["testkey"] = "testvalue";
                await context.ChallengeAsync("Twitter", properties);
                return true;
            });
            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://api.twitter.com/oauth/authenticate?oauth_token=", location);
            Assert.True(transaction.Response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookie));
            Assert.True(SetCookieHeaderValue.TryParseList(setCookie.ToList(), out var setCookieValues));
            Assert.Single(setCookieValues);
            var setCookieValue = setCookieValues.Single();
            var cookie = new CookieHeaderValue(setCookieValue.Name, setCookieValue.Value);

            var request = new HttpRequestMessage(HttpMethod.Get, "/signin-twitter?denied=ABCDEFG");
            request.Headers.Add(HeaderNames.Cookie, cookie.ToString());
            var client = server.CreateClient();
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        [Fact]
        public async Task TwitterError_Json_ThrowsParsedException()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = JsonErroredBackchannelRequestToken
                };
            },
            async context =>
            {
                await context.ChallengeAsync("Twitter");
                return true;
            });
            using var server = host.GetTestServer();
            
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await server.SendAsync("http://example.com/challenge");
            });

            var expectedErrorMessage = "An error has occurred while calling the Twitter API, error's returned:" + Environment.NewLine
                + "Code: 32, Message: 'Could not authenticate you.'";

            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public async Task TwitterError_UnknownContentType_ThrowsHttpException()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = UnknownContentTypeErroredBackchannelRequestToken
                };
            },
            async context =>
            {
                await context.ChallengeAsync("Twitter");
                return true;
            });
            using var server = host.GetTestServer();

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await server.SendAsync("http://example.com/challenge");
            });
        }

        [Fact]
        public async Task BadCallbackCallsRemoteAuthFailedWithState()
        {
            using var host = await CreateHost(o =>
            {
                o.ConsumerKey = "Test Consumer Key";
                o.ConsumerSecret = "Test Consumer Secret";
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = BackchannelRequestToken
                };
                o.Events = new TwitterEvents()
                {
                    OnRemoteFailure = context =>
                    {
                        Assert.NotNull(context.Failure);
                        Assert.Equal("Access was denied by the resource owner or by the remote server.", context.Failure.Message);
                        Assert.NotNull(context.Properties);
                        Assert.Equal("testvalue", context.Properties.Items["testkey"]);
                        context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            },
            async context =>
            {
                var properties = new AuthenticationProperties();
                properties.Items["testkey"] = "testvalue";
                await context.ChallengeAsync("Twitter", properties);
                return true;
            });

            using var server = host.GetTestServer();
            var transaction = await server.SendAsync("http://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.AbsoluteUri;
            Assert.Contains("https://api.twitter.com/oauth/authenticate?oauth_token=", location);
            Assert.True(transaction.Response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookie));
            Assert.True(SetCookieHeaderValue.TryParseList(setCookie.ToList(), out var setCookieValues));
            Assert.Single(setCookieValues);
            var setCookieValue = setCookieValues.Single();
            var cookie = new CookieHeaderValue(setCookieValue.Name, setCookieValue.Value);

            var request = new HttpRequestMessage(HttpMethod.Get, "/signin-twitter?denied=ABCDEFG");
            request.Headers.Add(HeaderNames.Cookie, cookie.ToString());
            var client = server.CreateClient();
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        private static async Task<IHost> CreateHost(Action<TwitterOptions> options, Func<HttpContext, Task<bool>> handler = null)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(builder =>
                    builder.UseTestServer()
                        .Configure(app =>
                        {
                            app.UseAuthentication();
                            app.Use(async (context, next) =>
                            {
                                var req = context.Request;
                                var res = context.Response;
                                if (req.Path == new PathString("/signIn"))
                                {
                                    await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("Twitter", new ClaimsPrincipal()));
                                }
                                else if (req.Path == new PathString("/signOut"))
                                {
                                    await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync("Twitter"));
                                }
                                else if (req.Path == new PathString("/forbid"))
                                {
                                    await Assert.ThrowsAsync<InvalidOperationException>(() => context.ForbidAsync("Twitter"));
                                }
                                else if (handler == null || !await handler(context))
                                {
                                    await next();
                                }
                            });
                        })
                        .ConfigureServices(services =>
                        {
                            Action<TwitterOptions> wrapOptions = o =>
                            {
                                o.SignInScheme = "External";
                                options(o);
                            };
                            services.AddAuthentication()
                                .AddCookie("External", _ => { })
                                .AddTwitter(wrapOptions);
                        }))
                .Build();

            await host.StartAsync();
            return host;
        }

        private HttpResponseMessage BackchannelRequestToken(HttpRequestMessage req)
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
            throw new NotImplementedException(req.RequestUri.AbsoluteUri);
        }

        private HttpResponseMessage JsonErroredBackchannelRequestToken(HttpRequestMessage req)
        {
            if (req.RequestUri.AbsoluteUri == "https://api.twitter.com/oauth/request_token")
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content =
                        new StringContent("{\"errors\":[{\"code\":32,\"message\":\"Could not authenticate you.\"}]}",
                            Encoding.UTF8,
                            "application/json")
                };
            }
            throw new NotImplementedException(req.RequestUri.AbsoluteUri);
        }

        private HttpResponseMessage UnknownContentTypeErroredBackchannelRequestToken(HttpRequestMessage req)
        {
            if (req.RequestUri.AbsoluteUri == "https://api.twitter.com/oauth/request_token")
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content =
                        new StringContent("example response text",
                            Encoding.UTF8,
                            "text/html")
                };
            }
            throw new NotImplementedException(req.RequestUri.AbsoluteUri);
        }
    }
}
