// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    public class NegotiateHandlerTests
    {
        [Fact]
        public async Task Anonymous_MissingConnectionFeatures_ThrowsNotSupported()
        {
            var server = await CreateServerAsync();

            var ex = await Assert.ThrowsAsync<NotSupportedException>(() => SendAsync(server, "/expectanonymous", connection: null));
            Assert.Equal("Negotiate authentication requires a server that supports IConnectionItemsFeature like Kestrel.", ex.Message);
        }

        [Fact]
        public async Task Anonymous_NoChallenge_NoOps()
        {
            var server = await CreateServerAsync();

            var result = await SendAsync(server, "/expectanonymous", new TestConnection());
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
        }

        [Fact]
        public async Task Anonymous_Http2_NoOps()
        {
            var server = await CreateServerAsync();

            var result = await SendAsync(server, "/expectanonymous", connection: null, http2: true);
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
        }

        [Fact]
        public async Task Anonymous_Challenge_401Negotiate()
        {
            var server = await CreateServerAsync();

            var result = await SendAsync(server, "/requireauth", new TestConnection());
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Fact]
        // TODO: Verify clients will downgrade to HTTP/1? Or do we need to send HTTP_1_1_REQUIRED?
        public async Task Anonymous_ChallengeHttp2_401Negotiate()
        {
            var server = await CreateServerAsync();

            var result = await SendAsync(server, "/requireauth", connection: null, http2: true);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Fact]
        public async Task Stage1Auth_401NegotiateServerBlob1()
        {
            var server = await CreateServerAsync();
            var result = await SendAsync(server, "/notexecuted", new TestConnection(), "Negotiate ClientBlob1");
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerBlob1", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Fact]
        public async Task AnonymousAfterStage1_Throws()
        {
            var server = await CreateServerAsync();
            var testConnection = new TestConnection();
            await Stage1Auth(server, testConnection);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(server, "/notexecuted", testConnection));
            Assert.Equal("An anonymous request was received in between authentication handshake requests.", ex.Message);
        }

        [Fact]
        public async Task Stage2Auth_WithoutStage1_Throws()
        {
            var server = await CreateServerAsync();

            var ex = await Assert.ThrowsAsync<TrueException>(() => SendAsync(server, "/requireauth", new TestConnection(), "Negotiate ClientBlob2"));
            Assert.Equal("Stage1Complete", ex.UserMessage);
        }

        [Fact]
        public async Task Stage1And2Auth_Success()
        {
            var server = await CreateServerAsync();
            var testConnection = new TestConnection();
            await Stage1And2Auth(server, testConnection);
        }

        [Fact]
        public async Task AnonymousAfterCompleted_Success()
        {
            var server = await CreateServerAsync();
            var testConnection = new TestConnection();
            await Stage1And2Auth(server, testConnection);

            var result = await SendAsync(server, "/requireauth", testConnection);
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
            Assert.False(result.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate));
        }

        [Fact]
        public async Task AuthHeaderAfterCompleted_Throws()
        {
            var server = await CreateServerAsync();
            var testConnection = new TestConnection();
            await Stage1And2Auth(server, testConnection);

            var ex = await Assert.ThrowsAsync<FalseException>(() => SendAsync(server, "/notexecuted", testConnection, "Negotiate ClientBlob2"));
            Assert.Equal("IsCompleted", ex.UserMessage);
        }

        private static async Task Stage1And2Auth(TestServer server, TestConnection testConnection)
        {
            await Stage1Auth(server, testConnection);
            await Stage2Auth(server, testConnection);
        }

        private static async Task Stage1Auth(TestServer server, TestConnection testConnection)
        {
            var result = await SendAsync(server, "/notexecuted", testConnection, "Negotiate ClientBlob1");
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerBlob1", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        private static async Task Stage2Auth(TestServer server, TestConnection testConnection)
        {
            var result = await SendAsync(server, "/requireauth", testConnection, "Negotiate ClientBlob2");
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerBlob2", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        private static async Task<TestServer> CreateServerAsync(Action<NegotiateOptions> configureOptions = null)
        {
            var builder = new HostBuilder()
                .ConfigureServices(services => services
                    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                    .AddNegotiate(options =>
                    {
                        options.StateFactory = new TestNegotiateStateFactory();
                        configureOptions?.Invoke(options);
                    }))
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Run(TestApp);

                    });                
                });

            var server = (await builder.StartAsync()).GetTestServer();
            return server;
        }

        private static async Task TestApp(HttpContext context)
        {
            if (context.Request.Path == new PathString("/expectanonymous"))
            {
                Assert.False(context.User.Identity.IsAuthenticated, "Anonymous");
                return;
            }
            else if (context.Request.Path == new PathString("/requireauth"))
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync();
                    return;
                }

                var name = context.User.Identity.Name;
                Assert.False(string.IsNullOrEmpty(name), "name");
                await context.Response.WriteAsync(name);
            }
            else if (context.Request.Path == new PathString("/unauthorized"))
            {
                // Simulate Authorization failure 
                var result = await context.AuthenticateAsync();
                await context.ChallengeAsync();
            }
            else if (context.Request.Path == new PathString("/signIn"))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
            }
            else if (context.Request.Path == new PathString("/signOut"))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            }
            else
            {
                throw new NotImplementedException(context.Request.Path);
            }
        }

        private static Task<HttpContext> SendAsync(TestServer server, string path, TestConnection connection, string authorizationHeader = null, bool http2 = false)
        {
            return server.SendAsync(context =>
            {
                if (http2)
                {
                    context.Request.Protocol = "HTTP/2";
                }
                context.Request.Path = path;
                if (!string.IsNullOrEmpty(authorizationHeader))
                {
                    context.Request.Headers[HeaderNames.Authorization] = authorizationHeader;
                }
                if (connection != null)
                {
                    context.Features.Set<IConnectionItemsFeature>(connection);
                    context.Features.Set<IConnectionCompleteFeature>(connection);
                }
            });
        }

        private class TestConnection : IConnectionItemsFeature, IConnectionCompleteFeature
        {
            public IDictionary<object, object> Items { get; set; } = new ConnectionItems();

            public void OnCompleted(Func<object, Task> callback, object state)
            {
            }
        }

        private class TestNegotiateStateFactory : INegotiateStateFactory
        {
            public INegotiateState CreateInstance() => new TestNegotiateState();
        }

        private class TestNegotiateState : INegotiateState
        {
            private bool Stage1Complete { get; set; }
            public bool IsCompleted { get; private set; }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IIdentity GetIdentity()
            {
                Assert.True(IsCompleted, nameof(IsCompleted));
                return new GenericIdentity("name", "Negotiate");
            }

            public string GetOutgoingBlob(string incomingBlob)
            {
                Assert.False(IsCompleted, nameof(IsCompleted));
                switch (incomingBlob)
                {
                    case "ClientBlob1":
                        Assert.False(Stage1Complete, nameof(Stage1Complete));
                        Stage1Complete = true;
                        return "ServerBlob1";
                    case "ClientBlob2":
                        Assert.True(Stage1Complete, nameof(Stage1Complete));
                        IsCompleted = true;
                        return "ServerBlob2";
                    default:
                        throw new InvalidOperationException(incomingBlob);
                }
            }
        }
    }
}
