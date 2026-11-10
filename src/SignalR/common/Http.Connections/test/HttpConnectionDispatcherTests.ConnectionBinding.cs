// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public partial class HttpConnectionDispatcherTests
    {
        [Theory]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        [InlineData(HttpTransportType.WebSockets)]
        public async Task NegotiatedConnectionRejectsDifferentUserOnFirstGet(HttpTransportType transportType)
        {
            await AssertFirstGetStatusAsync(
                transportType,
                MakeUser("alice"),
                MakeUser("mallory"),
                StatusCodes.Status403Forbidden);
        }

        [Theory]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        public async Task NegotiatedConnectionAllowsSameUserOnFirstGet(HttpTransportType transportType)
        {
            await AssertFirstGetStatusAsync(
                transportType,
                MakeUser("alice"),
                MakeUser("alice"),
                StatusCodes.Status200OK);
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsSameGenericIdentityUserOnFirstGet()
        {
            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                MakeGenericIdentityUser("alice"),
                MakeGenericIdentityUser("alice"),
                StatusCodes.Status200OK);
        }

        [Fact]
        public async Task NegotiatedConnectionRejectsDifferentGenericIdentityUserOnFirstGet()
        {
            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                MakeGenericIdentityUser("alice"),
                MakeGenericIdentityUser("mallory"),
                StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task NegotiatedConnectionFallbackIgnoresAuthenticatedClaimOrdering()
        {
            var negotiated = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("employeeId", "1"), new Claim("department", "eng") },
                "Test"));
            var connecting = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("department", "eng"), new Claim("employeeId", "1") },
                "Test"));

            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                negotiated,
                connecting,
                StatusCodes.Status200OK);
        }

        [Fact]
        public async Task NegotiatedConnectionRejectsAuthenticatedUserAfterAnonymousNegotiate()
        {
            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                new ClaimsPrincipal(new ClaimsIdentity()),
                MakeUser("alice"),
                StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsSameUserSendBeforeFirstTransport()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var alice = MakeUser("alice");
                var connection = manager.CreateConnection(PipeOptions.Default, PipeOptions.Default, alice);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var context = MakeRequest("/foo", connection);
                context.Request.Method = HttpMethods.Post;
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload-before-first-transport"));
                context.User = alice;

                await dispatcher.ExecuteAsync(
                    context,
                    new HttpConnectionDispatcherOptions(),
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
                Assert.True(connection.Transport.Input.TryRead(out var result));
                Assert.Equal(
                    "payload-before-first-transport",
                    Encoding.UTF8.GetString(result.Buffer.ToArray()));
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
                Assert.Equal(HttpTransportType.None, connection.TransportType);
                Assert.True(connection.HasNegotiatedUser);
                Assert.Null(connection.User);
            }
        }

        [Fact]
        public async Task NegotiatedConnectionRejectsDifferentUserSendBeforeFirstTransport()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var alice = MakeUser("alice");
                var connection = manager.CreateConnection(PipeOptions.Default, PipeOptions.Default, alice);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var context = MakeRequest("/foo", connection);
                context.Request.Method = HttpMethods.Post;
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload-before-first-transport"));
                context.User = MakeUser("mallory");

                await dispatcher.ExecuteAsync(
                    context,
                    new HttpConnectionDispatcherOptions(),
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
                Assert.False(connection.Transport.Input.TryRead(out var result));
                Assert.Equal(HttpTransportType.None, connection.TransportType);
                Assert.True(connection.HasNegotiatedUser);
                Assert.Null(connection.User);
            }
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsDifferentUserOnSubsequentLongPollingRequest()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var alice = MakeUser("alice");
                var mallory = MakeUser("mallory");
                var connection = manager.CreateConnection(PipeOptions.Default, PipeOptions.Default, alice);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var options = new HttpConnectionDispatcherOptions();
                var applicationTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                try
                {
                    var firstContext = MakeRequest("/foo", connection);
                    firstContext.User = alice;

                    await dispatcher.ExecuteAsync(
                        firstContext,
                        options,
                        _ => applicationTcs.Task).OrTimeout();

                    Assert.Equal(StatusCodes.Status200OK, firstContext.Response.StatusCode);
                    Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
                    Assert.False(connection.HasNegotiatedUser);
                    Assert.Same(alice, connection.User);
                    Assert.Equal(
                        "alice",
                        connection.User.FindFirst(ClaimTypes.NameIdentifier).Value);

                    var laterContext = MakeRequest("/foo", connection);
                    laterContext.User = mallory;
                    var pollTask = dispatcher.ExecuteAsync(
                        laterContext,
                        options,
                        _ => applicationTcs.Task);

                    await connection.Transport.Output
                        .WriteAsync(Encoding.UTF8.GetBytes("payload-from-subsequent-long-poll"))
                        .AsTask()
                        .OrTimeout();
                    await pollTask.OrTimeout();

                    Assert.Equal(StatusCodes.Status200OK, laterContext.Response.StatusCode);
                    Assert.Equal(
                        "payload-from-subsequent-long-poll",
                        Encoding.UTF8.GetString(((MemoryStream)laterContext.Response.Body).ToArray()));
                    Assert.False(connection.HasNegotiatedUser);
                    Assert.Same(mallory, connection.User);
                    Assert.Equal(
                        "mallory",
                        connection.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                }
                finally
                {
                    applicationTcs.TrySetResult(null);
                }
            }
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsDifferentUserSendAfterFirstTransport()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var alice = MakeUser("alice");
                var connection = manager.CreateConnection(PipeOptions.Default, PipeOptions.Default, alice);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var options = new HttpConnectionDispatcherOptions();
                var firstContext = MakeRequest("/foo", connection);
                firstContext.User = alice;

                await dispatcher.ExecuteAsync(
                    firstContext,
                    options,
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status200OK, firstContext.Response.StatusCode);
                Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
                Assert.False(connection.HasNegotiatedUser);
                Assert.Same(alice, connection.User);
                Assert.Equal(
                    "alice",
                    connection.User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var context = MakeRequest("/foo", connection);
                context.Request.Method = HttpMethods.Post;
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload-after-release"));
                context.User = MakeUser("mallory");

                await dispatcher.ExecuteAsync(
                    context,
                    options,
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
                Assert.True(connection.Transport.Input.TryRead(out var result));
                Assert.Equal(
                    "payload-after-release",
                    Encoding.UTF8.GetString(result.Buffer.ToArray()));
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
                Assert.False(connection.HasNegotiatedUser);
                Assert.Same(alice, connection.User);
                Assert.Equal(
                    "alice",
                    connection.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            }
        }

        [Fact]
        public async Task SendWithDifferentUserIsRejectedWhileFirstGetIsInitializing()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var connection = manager.CreateConnection(
                    PipeOptions.Default,
                    PipeOptions.Default,
                    MakeUser("alice"));
                connection.TransportType = HttpTransportType.LongPolling;
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var context = MakeRequest("/foo", connection);
                context.Request.Method = HttpMethods.Post;
                context.Request.Body = new MemoryStream(
                    Encoding.UTF8.GetBytes("payload-during-first-transport-initialization"));
                context.User = MakeUser("mallory");

                await dispatcher.ExecuteAsync(
                    context,
                    new HttpConnectionDispatcherOptions(),
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
                Assert.False(connection.Transport.Input.TryRead(out var result));
                Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
                Assert.True(connection.HasNegotiatedUser);
                Assert.Null(connection.User);
            }
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsDifferentUserDeleteAfterFirstTransport()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var alice = MakeUser("alice");
                var connection = manager.CreateConnection(PipeOptions.Default, PipeOptions.Default, alice);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var options = new HttpConnectionDispatcherOptions();
                var firstContext = MakeRequest("/foo", connection);
                firstContext.User = alice;

                await dispatcher.ExecuteAsync(
                    firstContext,
                    options,
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status200OK, firstContext.Response.StatusCode);
                Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
                Assert.False(connection.HasNegotiatedUser);
                Assert.Same(alice, connection.User);
                Assert.Equal(
                    "alice",
                    connection.User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var context = MakeRequest("/foo", connection);
                context.Request.Method = HttpMethods.Delete;
                context.User = MakeUser("mallory");

                await dispatcher.ExecuteAsync(
                    context,
                    options,
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
                Assert.Equal("text/plain", context.Response.ContentType);
                Assert.NotNull(connection.DisposeAndRemoveTask);
                await connection.DisposeAndRemoveTask.OrTimeout();
                Assert.Equal(HttpConnectionStatus.Disposed, connection.Status);
                Assert.False(manager.TryGetConnection(connection.ConnectionId, out var remainingConnection));
                Assert.False(connection.HasNegotiatedUser);
                Assert.Same(alice, connection.User);
                Assert.Equal(
                    "alice",
                    connection.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            }
        }

        private async Task AssertFirstGetStatusAsync(
            HttpTransportType transportType,
            ClaimsPrincipal negotiateUser,
            ClaimsPrincipal connectUser,
            int expectedStatusCode)
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var negotiateContext = new DefaultHttpContext();
                negotiateContext.Request.Path = "/foo";
                negotiateContext.Request.Method = HttpMethods.Post;
                negotiateContext.Response.Body = new MemoryStream();
                negotiateContext.User = negotiateUser;

                await dispatcher.ExecuteNegotiateAsync(negotiateContext, new HttpConnectionDispatcherOptions());

                var negotiateResponse = JsonConvert.DeserializeObject<JObject>(
                    Encoding.UTF8.GetString(((MemoryStream)negotiateContext.Response.Body).ToArray()));
                var connectionId = negotiateResponse.Value<string>("connectionId");
                Assert.True(manager.TryGetConnection(connectionId, out var connection));

                var connectContext = MakeRequest("/foo", connection);
                SetTransport(connectContext, transportType);
                connectContext.User = connectUser;
                var options = new HttpConnectionDispatcherOptions();
                options.WebSockets.CloseTimeout = TimeSpan.FromMilliseconds(50);

                await dispatcher.ExecuteAsync(
                    connectContext,
                    options,
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(expectedStatusCode, connectContext.Response.StatusCode);
                Assert.Equal(expectedStatusCode == StatusCodes.Status403Forbidden, connection.HasNegotiatedUser);
            }
        }

        private static ClaimsPrincipal MakeUser(string nameIdentifier)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, nameIdentifier) },
                "Test"));
        }

        private static ClaimsPrincipal MakeGenericIdentityUser(string name)
        {
            var identity = new GenericIdentity(name);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, name + "-id"));
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task NegotiatedConnectionRejectsAuthenticatedUserAfterNullUserNegotiate()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var options = new HttpConnectionDispatcherOptions();
                var connection = manager.CreateConnection(PipeOptions.Default, PipeOptions.Default, null);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var context = MakeRequest("/foo", connection);
                var connectingUser = MakeUser("alice");
                context.User = connectingUser;

                await dispatcher.ExecuteAsync(
                    context,
                    options,
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
                Assert.Equal(HttpTransportType.None, connection.TransportType);
                Assert.Null(connection.User);
                Assert.True(connection.HasNegotiatedUser);
            }
        }

        [Fact]
        public async Task NonNegotiatedConnectionAllowsAuthenticatedUserOnFirstGet()
        {
            using (StartVerifiableLog(out var loggerFactory, LogLevel.Debug))
            {
                var manager = CreateConnectionManager(loggerFactory);
                var options = new HttpConnectionDispatcherOptions();
                var connection = manager.CreateConnection(PipeOptions.Default, PipeOptions.Default);
                var dispatcher = new HttpConnectionDispatcher(manager, loggerFactory);
                var context = MakeRequest("/foo", connection);
                var connectingUser = MakeUser("alice");
                context.User = connectingUser;

                await dispatcher.ExecuteAsync(
                    context,
                    options,
                    _ => Task.CompletedTask).OrTimeout();

                Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
                Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
                Assert.False(connection.HasNegotiatedUser);
                Assert.Same(connectingUser, connection.User);
                Assert.Equal("alice", connection.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            }
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsSameClaimlessNamedIdentityUserOnFirstGet()
        {
            var negotiatedIdentity = new ClaimlessNamedIdentity("alice");
            var connectingIdentity = new ClaimlessNamedIdentity("alice");

            Assert.True(negotiatedIdentity.IsAuthenticated);
            Assert.True(connectingIdentity.IsAuthenticated);
            Assert.Equal("alice", negotiatedIdentity.Name);
            Assert.Equal("alice", connectingIdentity.Name);
            Assert.Empty(negotiatedIdentity.Claims);
            Assert.Empty(connectingIdentity.Claims);

            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                new ClaimsPrincipal(negotiatedIdentity),
                new ClaimsPrincipal(connectingIdentity),
                StatusCodes.Status200OK);
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsCaseInsensitiveClaimlessNamedIdentityUserOnFirstGet()
        {
            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                new ClaimsPrincipal(new ClaimlessNamedIdentity("ALICE")),
                new ClaimsPrincipal(new ClaimlessNamedIdentity("alice")),
                StatusCodes.Status200OK);
        }

        [Fact]
        public async Task NegotiatedConnectionRejectsCaseChangedUriClaimlessNamedIdentityUserOnFirstGet()
        {
            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                new ClaimsPrincipal(new ClaimlessNamedIdentity("https://example.com/users/alice")),
                new ClaimsPrincipal(new ClaimlessNamedIdentity("https://example.com/users/ALICE")),
                StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task NegotiatedConnectionRejectsClaimIdentityAndNameFallbackIdentityWithSameName()
        {
            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "alice") },
                    "Test")),
                new ClaimsPrincipal(new ClaimlessNamedIdentity("alice")),
                StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task NegotiatedConnectionRejectsDifferentClaimlessNamedIdentityUserOnFirstGet()
        {
            var negotiatedIdentity = new ClaimlessNamedIdentity("alice");
            var connectingIdentity = new ClaimlessNamedIdentity("mallory");

            Assert.True(negotiatedIdentity.IsAuthenticated);
            Assert.True(connectingIdentity.IsAuthenticated);
            Assert.Equal("alice", negotiatedIdentity.Name);
            Assert.Equal("mallory", connectingIdentity.Name);
            Assert.Empty(negotiatedIdentity.Claims);
            Assert.Empty(connectingIdentity.Claims);

            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                new ClaimsPrincipal(negotiatedIdentity),
                new ClaimsPrincipal(connectingIdentity),
                StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task NegotiatedConnectionAllowsClaimlessAuthenticatedIdentityWithNullOrEmptyNameOnFirstGet()
        {
            var negotiatedIdentity = new ClaimsIdentity("SchemeA");
            var connectingIdentity = new ClaimlessNamedIdentity(string.Empty);

            Assert.True(negotiatedIdentity.IsAuthenticated);
            Assert.True(connectingIdentity.IsAuthenticated);
            Assert.Null(negotiatedIdentity.Name);
            Assert.Equal(string.Empty, connectingIdentity.Name);
            Assert.Empty(negotiatedIdentity.Claims);
            Assert.Empty(connectingIdentity.Claims);

            await AssertFirstGetStatusAsync(
                HttpTransportType.LongPolling,
                new ClaimsPrincipal(negotiatedIdentity),
                new ClaimsPrincipal(connectingIdentity),
                StatusCodes.Status200OK);
        }

        private sealed class ClaimlessNamedIdentity : ClaimsIdentity
        {
            private readonly string _name;

            public ClaimlessNamedIdentity(string name)
            {
                _name = name;
            }

            public override bool IsAuthenticated => true;

            public override string Name => _name;
        }
    }
}
