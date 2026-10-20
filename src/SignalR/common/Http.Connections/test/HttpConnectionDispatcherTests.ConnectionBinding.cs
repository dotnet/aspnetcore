// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public partial class HttpConnectionDispatcherTests
{
    [Theory]
    [InlineData(HttpTransportType.LongPolling, 0)]
    [InlineData(HttpTransportType.LongPolling, 1)]
    [InlineData(HttpTransportType.ServerSentEvents, 0)]
    [InlineData(HttpTransportType.ServerSentEvents, 1)]
    [InlineData(HttpTransportType.WebSockets, 0)]
    [InlineData(HttpTransportType.WebSockets, 1)]
    public async Task NegotiatedConnectionRejectsDifferentUserOnFirstGet(
        HttpTransportType transportType,
        int negotiateVersion)
    {
        await AssertFirstGetStatusAsync(
            transportType,
            MakeUser("alice"),
            MakeUser("mallory"),
            StatusCodes.Status403Forbidden,
            negotiateVersion);
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.WebSockets)]
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
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var alice = MakeUser("alice");
            var connection = manager.CreateConnection(options, negotiatedUser: alice);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var context = MakeRequest("/foo", connection, services);
            context.Request.Method = HttpMethods.Post;
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload-before-first-transport"));
            context.User = alice;

            await dispatcher.ExecuteAsync(context, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.True(connection.Transport.Input.TryRead(out var result));
            Assert.Equal("payload-before-first-transport", Encoding.UTF8.GetString(result.Buffer.ToArray()));
            connection.Transport.Input.AdvanceTo(result.Buffer.End);
            Assert.Equal(HttpTransportType.None, connection.TransportType);
            Assert.True(connection.HasNegotiatedUser);
            Assert.Null(connection.User);
        }
    }

    [Fact]
    public async Task NegotiatedConnectionRejectsDifferentUserSendBeforeFirstTransport()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var connection = manager.CreateConnection(options, negotiatedUser: MakeUser("alice"));
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var context = MakeRequest("/foo", connection, services);
            context.Request.Method = HttpMethods.Post;
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload-before-first-transport"));
            context.User = MakeUser("mallory");

            await dispatcher.ExecuteAsync(context, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.False(connection.Transport.Input.TryRead(out _));
            Assert.Equal(HttpTransportType.None, connection.TransportType);
            Assert.True(connection.HasNegotiatedUser);
            Assert.Null(connection.User);
        }
    }

    [Fact]
    public async Task NegotiatedConnectionAllowsDifferentUserOnSubsequentLongPollingRequest()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var alice = MakeUser("alice");
            var mallory = MakeUser("mallory");
            var connection = manager.CreateConnection(options, negotiatedUser: alice);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var application = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                var firstContext = MakeRequest("/foo", connection, services);
                firstContext.User = alice;

                await dispatcher.ExecuteAsync(firstContext, options, _ => application.Task).DefaultTimeout();

                Assert.Equal(StatusCodes.Status200OK, firstContext.Response.StatusCode);
                Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
                Assert.False(connection.HasNegotiatedUser);
                Assert.Same(alice, connection.User);
                Assert.Equal("alice", connection.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var laterContext = MakeRequest("/foo", connection, services);
                laterContext.User = mallory;

                var laterRequest = dispatcher.ExecuteAsync(laterContext, options, _ => application.Task);
                await connection.Transport.Output.WriteAsync(
                    Encoding.UTF8.GetBytes("payload-from-subsequent-long-poll")).AsTask().DefaultTimeout();
                await laterRequest.DefaultTimeout();

                Assert.Equal(StatusCodes.Status200OK, laterContext.Response.StatusCode);
                Assert.Equal(
                    "payload-from-subsequent-long-poll",
                    Encoding.UTF8.GetString(((MemoryStream)laterContext.Response.Body).ToArray()));
                Assert.False(connection.HasNegotiatedUser);
                Assert.Same(mallory, connection.User);
                Assert.Equal("mallory", connection.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var userNameChanged = Assert.Single(TestSink.Writes, write => write.EventId.Name == "UserNameChanged");
                Assert.Equal("The name of the user changed from 'alice' to 'mallory'.", userNameChanged.Message);
            }
            finally
            {
                application.TrySetResult(new object());
                if (connection.ApplicationTask is not null)
                {
                    await connection.ApplicationTask.DefaultTimeout();
                }
            }
        }
    }

    [Fact]
    public async Task NegotiatedConnectionAllowsDifferentUserSendAfterFirstTransport()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var alice = MakeUser("alice");
            var connection = manager.CreateConnection(options, negotiatedUser: alice);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var firstContext = MakeRequest("/foo", connection, services);
            firstContext.User = alice;

            await dispatcher.ExecuteAsync(firstContext, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, firstContext.Response.StatusCode);
            Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
            Assert.False(connection.HasNegotiatedUser);
            Assert.Same(alice, connection.User);
            Assert.Equal("alice", connection.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var context = MakeRequest("/foo", connection, services);
            context.Request.Method = HttpMethods.Post;
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload-after-release"));
            context.User = MakeUser("mallory");

            await dispatcher.ExecuteAsync(context, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.True(connection.Transport.Input.TryRead(out var result));
            Assert.Equal("payload-after-release", Encoding.UTF8.GetString(result.Buffer.ToArray()));
            connection.Transport.Input.AdvanceTo(result.Buffer.End);
            Assert.False(connection.HasNegotiatedUser);
            Assert.Same(alice, connection.User);
            Assert.Equal("alice", connection.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }

    [Fact]
    public async Task SendWithDifferentUserIsRejectedWhileFirstGetIsInitializing()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var connection = manager.CreateConnection(options, negotiatedUser: MakeUser("alice"));
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var context = MakeRequest("/foo", connection, services);
            context.Request.Method = HttpMethods.Post;
            context.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes("payload-during-first-transport-initialization"));
            context.User = MakeUser("mallory");

            await dispatcher.ExecuteAsync(context, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.False(connection.Transport.Input.TryRead(out _));
            Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
            Assert.True(connection.HasNegotiatedUser);
            Assert.Null(connection.User);
        }
    }

    [Fact]
    public async Task NegotiatedConnectionAllowsDifferentUserDeleteAfterFirstTransport()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var alice = MakeUser("alice");
            var connection = manager.CreateConnection(options, negotiatedUser: alice);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var firstContext = MakeRequest("/foo", connection, services);
            firstContext.User = alice;

            await dispatcher.ExecuteAsync(firstContext, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, firstContext.Response.StatusCode);
            Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
            Assert.False(connection.HasNegotiatedUser);
            Assert.Same(alice, connection.User);
            Assert.Equal("alice", connection.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var context = MakeRequest("/foo", connection, services);
            context.Request.Method = HttpMethods.Delete;
            context.User = MakeUser("mallory");

            await dispatcher.ExecuteAsync(context, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
            Assert.Equal("text/plain", context.Response.ContentType);
            Assert.NotNull(connection.DisposeAndRemoveTask);
            await connection.DisposeAndRemoveTask!.DefaultTimeout();
            Assert.Equal(HttpConnectionStatus.Disposed, connection.Status);
            Assert.False(manager.TryGetConnection(connection.ConnectionToken, out _));
            Assert.Same(alice, connection.User);
            Assert.Equal("alice", connection.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }

    private async Task AssertFirstGetStatusAsync(
        HttpTransportType transportType,
        ClaimsPrincipal negotiateUser,
        ClaimsPrincipal connectUser,
        int expectedStatusCode,
        int negotiateVersion = 1)
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var negotiateContext = new DefaultHttpContext();
            negotiateContext.Request.Path = "/foo";
            negotiateContext.Request.Method = HttpMethods.Post;
            negotiateContext.Request.QueryString = negotiateVersion > 0
                ? new QueryString($"?negotiateVersion={negotiateVersion}")
                : QueryString.Empty;
            negotiateContext.Response.Body = new MemoryStream();
            negotiateContext.RequestServices = services.BuildServiceProvider();
            negotiateContext.User = negotiateUser;

            await dispatcher.ExecuteNegotiateAsync(negotiateContext, new HttpConnectionDispatcherOptions());

            var negotiateResponse = JsonConvert.DeserializeObject<JObject>(
                Encoding.UTF8.GetString(((MemoryStream)negotiateContext.Response.Body).ToArray()));
            var connectionToken = negotiateResponse.Value<string>("connectionToken")
                ?? negotiateResponse.Value<string>("connectionId");
            Assert.True(manager.TryGetConnection(connectionToken, out var connection));

            var connectContext = MakeRequest("/foo", connection, services);
            SetTransport(connectContext, transportType);
            connectContext.User = connectUser;
            var options = new HttpConnectionDispatcherOptions();
            options.WebSockets.CloseTimeout = TimeSpan.FromMilliseconds(50);

            await dispatcher.ExecuteAsync(
                connectContext,
                options,
                _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(expectedStatusCode, connectContext.Response.StatusCode);
            Assert.Equal(expectedStatusCode == StatusCodes.Status403Forbidden, connection.HasNegotiatedUser);
        }
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        return services;
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
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, $"{name}-id"));
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task NegotiatedConnectionRejectsAuthenticatedUserAfterNullUserNegotiate()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var connection = manager.CreateConnection(options, negotiatedUser: null);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var context = MakeRequest("/foo", connection, services);
            var connectingUser = MakeUser("alice");
            context.User = connectingUser;

            await dispatcher.ExecuteAsync(context, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.Equal(HttpTransportType.None, connection.TransportType);
            Assert.Null(connection.User);
            Assert.True(connection.HasNegotiatedUser);
        }
    }

    [Fact]
    public async Task NonNegotiatedConnectionAllowsAuthenticatedUserOnFirstGet()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions();
            var connection = manager.CreateConnection(options);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var services = CreateServices();
            var context = MakeRequest("/foo", connection, services);
            var connectingUser = MakeUser("alice");
            context.User = connectingUser;

            await dispatcher.ExecuteAsync(context, options, _ => Task.CompletedTask).DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal(HttpTransportType.LongPolling, connection.TransportType);
            Assert.False(connection.HasNegotiatedUser);
            Assert.Same(connectingUser, connection.User);
            Assert.Equal("alice", connection.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }

    [Fact]
    public async Task NegotiatedConnectionAllowsSameClaimlessNamedIdentityUserOnFirstGet()
    {
        var negotiatedIdentity = new ClaimlessNamedIdentity("alice");
        var connectingIdentity = new ClaimlessNamedIdentity("alice");
        var negotiatedUser = new ClaimsPrincipal(negotiatedIdentity);
        var connectingUser = new ClaimsPrincipal(connectingIdentity);

        Assert.NotSame(negotiatedIdentity, connectingIdentity);
        Assert.NotSame(negotiatedUser, connectingUser);
        Assert.True(negotiatedIdentity.IsAuthenticated);
        Assert.True(connectingIdentity.IsAuthenticated);
        Assert.Equal("alice", negotiatedIdentity.Name);
        Assert.Equal("alice", connectingIdentity.Name);
        Assert.Empty(negotiatedIdentity.Claims);
        Assert.Empty(connectingIdentity.Claims);

        await AssertFirstGetStatusAsync(
            HttpTransportType.LongPolling,
            negotiatedUser,
            connectingUser,
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
        var negotiatedUser = new ClaimsPrincipal(negotiatedIdentity);
        var connectingUser = new ClaimsPrincipal(connectingIdentity);

        Assert.NotSame(negotiatedIdentity, connectingIdentity);
        Assert.NotSame(negotiatedUser, connectingUser);
        Assert.True(negotiatedIdentity.IsAuthenticated);
        Assert.True(connectingIdentity.IsAuthenticated);
        Assert.Equal("alice", negotiatedIdentity.Name);
        Assert.Equal("mallory", connectingIdentity.Name);
        Assert.Empty(negotiatedIdentity.Claims);
        Assert.Empty(connectingIdentity.Claims);

        await AssertFirstGetStatusAsync(
            HttpTransportType.LongPolling,
            negotiatedUser,
            connectingUser,
            StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task NegotiatedConnectionAllowsClaimlessAuthenticatedIdentityWithNullOrEmptyNameOnFirstGet()
    {
        var negotiatedIdentity = new ClaimsIdentity("SchemeA");
        var connectingIdentity = new ClaimlessNamedIdentity(string.Empty);
        var negotiatedUser = new ClaimsPrincipal(negotiatedIdentity);
        var connectingUser = new ClaimsPrincipal(connectingIdentity);

        Assert.NotSame(negotiatedIdentity, connectingIdentity);
        Assert.NotSame(negotiatedUser, connectingUser);
        Assert.True(negotiatedIdentity.IsAuthenticated);
        Assert.True(connectingIdentity.IsAuthenticated);
        Assert.Null(negotiatedIdentity.Name);
        Assert.Equal(string.Empty, connectingIdentity.Name);
        Assert.Empty(negotiatedIdentity.Claims);
        Assert.Empty(connectingIdentity.Claims);

        await AssertFirstGetStatusAsync(
            HttpTransportType.LongPolling,
            negotiatedUser,
            connectingUser,
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
