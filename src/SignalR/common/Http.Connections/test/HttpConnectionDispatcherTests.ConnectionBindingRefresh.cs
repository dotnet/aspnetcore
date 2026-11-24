// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public partial class HttpConnectionDispatcherTests
{
    // A competing poll is only run through the authentication-refresh policies when its token actually
    // replaces the connection's principal. Whatever the relationship between the competing token's
    // expiration and the connection's, the poll must still belong to the connection's identity. The
    // "earlier" case is the one that skips the refresh policies entirely; the others are covered here so
    // the rejection can't quietly regress to depending on expiration ordering.
    [Theory]
    [InlineData("earlier", HttpStatusCode.Forbidden)]
    [InlineData("equal", HttpStatusCode.Conflict)]
    [InlineData("later", HttpStatusCode.Conflict)]
    [InlineData("none", HttpStatusCode.Conflict)]
    public async Task LongPollingPollFromDifferentUserIsRejectedForAnyTokenExpiration(
    string competingExpiration, HttpStatusCode expectedStatusCode)
    {
        using var host = CreateConnectionBindingHost(out var tracker, out var tokens);
        host.Start();

        var url = GetHostUrl(host);
        using var client = new HttpClient();

        var connectionExpiration = DateTime.UtcNow.AddMinutes(30);
        var aliceToken = tokens.Create("alice", isPrivileged: false, connectionExpiration);
        var connectionToken = await NegotiateAsync(client, url, "/lowpriv", aliceToken);

        // Alice establishes the connection.
        var aliceResponse = await PollAsync(client, url, "/lowpriv", connectionToken, aliceToken);
        Assert.Equal(HttpStatusCode.OK, aliceResponse.StatusCode);

        var activation = await tracker.FirstActivation.DefaultTimeout();
        Assert.Equal("lowpriv", activation.Endpoint);
        Assert.Equal("alice", activation.UserId);

        // Mallory holds the connection token and a valid credential of her own.
        DateTime? malloryExpiration = competingExpiration switch
        {
            "earlier" => connectionExpiration.AddMinutes(-20),
            "equal" => connectionExpiration,
            "later" => connectionExpiration.AddMinutes(20),
            "none" => null,
            _ => throw new ArgumentOutOfRangeException(nameof(competingExpiration)),
        };
        var malloryToken = tokens.Create("mallory", isPrivileged: false, malloryExpiration);
        var malloryResponse = await PollAsync(client, url, "/lowpriv", connectionToken, malloryToken);

        Assert.Equal(expectedStatusCode, malloryResponse.StatusCode);
        if (expectedStatusCode == HttpStatusCode.Conflict)
        {
            AssertRefreshError(
                JObject.Parse(await malloryResponse.Content.ReadAsStringAsync()),
                "authentication_refresh_required");
        }

        // The connection is left intact for its real owner.
        var manager = host.Services.GetRequiredService<HttpConnectionManager>();
        Assert.True(manager.TryGetConnection(connectionToken, out var connection));
        Assert.Equal("alice", connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    // The first transport GET selects the transport before it awaits the authentication-refresh policy, so
    // there is a window where the connection has a user and a transport but no application. A competing GET
    // that arrives in that window must not be able to bind itself to the connection. Both requests use the
    // same mapping because endpoint metadata independently rejects cross-mapping connection tokens.
    [Fact]
    public async Task CompetingGetCannotActivateConnectionWhileFirstConnectRefreshIsPending()
    {
        var refreshEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var host = CreateConnectionBindingHost(out var tracker, out var tokens, async ctx =>
        {
            // Hold open only the refresh that runs on the first transport GET. The /refresh endpoint is a
            // POST and must complete normally so the connection has a published user before that GET.
            if (HttpMethods.IsGet(ctx.HttpContext.Request.Method) && refreshEntered.TrySetResult())
            {
                await releaseRefresh.Task;
            }

            return true;
        });
        host.Start();

        var url = GetHostUrl(host);
        using var client = new HttpClient();

        var aliceNegotiateToken = tokens.Create("alice", isPrivileged: true, DateTime.UtcNow.AddMinutes(30));
        var connectionToken = await NegotiateAsync(client, url, "/privileged", aliceNegotiateToken);

        // Alice refreshes before her first transport GET, which publishes her principal on the connection
        // while the transport is still unselected.
        var aliceRefreshToken = tokens.Create("alice", isPrivileged: true, DateTime.UtcNow.AddMinutes(30));
        var refreshResponse = await RefreshAsync(client, url, "/privileged", connectionToken, aliceRefreshToken);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        // Alice's first transport GET carries a newer token, so it runs the refresh policy and parks there.
        var aliceConnectToken = tokens.Create("alice", isPrivileged: true, DateTime.UtcNow.AddMinutes(60));
        var aliceGet = PollAsync(client, url, "/privileged", connectionToken, aliceConnectToken);

        try
        {
            await refreshEntered.Task.DefaultTimeout();

            // Mallory passes the endpoint's authorization on her own merits and holds Alice's connection
            // token. Her credential expires sooner than the connection's, so the refresh path treats it as
            // stale.
            var malloryToken = tokens.Create("mallory", isPrivileged: true, DateTime.UtcNow.AddMinutes(2));
            var malloryResponse = await PollAsync(client, url, "/privileged", connectionToken, malloryToken);

            Assert.Equal(HttpStatusCode.Forbidden, malloryResponse.StatusCode);
            Assert.Empty(tracker.Activations);
        }
        finally
        {
            releaseRefresh.TrySetResult();
        }

        // Alice's own connect still succeeds once her refresh completes, and it activates the endpoint she
        // actually negotiated against.
        var aliceResponse = await aliceGet.DefaultTimeout();
        Assert.Equal(HttpStatusCode.OK, aliceResponse.StatusCode);

        var activation = await tracker.FirstActivation.DefaultTimeout();
        Assert.Equal("privileged", activation.Endpoint);
        Assert.Equal("alice", activation.UserId);
        Assert.Single(tracker.Activations);
    }

    private static string GetHostUrl(IHost host)
        => host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();

    private static async Task<string> NegotiateAsync(HttpClient client, string url, string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{url}{path}/negotiate?negotiateVersion=1");
        request.Headers.Add("Authorization", $"Bearer {token}");
        using var response = await client.SendAsync(request).DefaultTimeout();
        response.EnsureSuccessStatusCode();

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        return json.Value<string>("connectionToken");
    }

    private static async Task<HttpResponseMessage> RefreshAsync(HttpClient client, string url, string path, string connectionToken, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{url}{path}/refresh");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add(ConnectionTokenHeaderName, connectionToken);
        return await client.SendAsync(request).DefaultTimeout();
    }

    private static Task<HttpResponseMessage> PollAsync(HttpClient client, string url, string path, string connectionToken, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{url}{path}?id={connectionToken}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        return client.SendAsync(request);
    }

    private IHost CreateConnectionBindingHost(
        out ConnectionActivationTracker tracker,
        out TestTokenFactory tokens,
        Func<AuthenticationRefreshContext, Task<bool>> onAuthenticationRefresh = null)
    {
        var activationTracker = new ConnectionActivationTracker();
        var tokenFactory = new TestTokenFactory();
        tracker = activationTracker;
        tokens = tokenFactory;

        void ConfigureEndpoint(HttpConnectionDispatcherOptions options)
        {
            options.Transports = HttpTransportType.LongPolling;
            options.EnableAuthenticationRefresh = true;
            // Bound the red-test failure mode: a wrongly-accepted poll would otherwise hang for the default timeout.
            options.LongPolling.PollTimeout = TimeSpan.FromSeconds(3);
            if (onAuthenticationRefresh is not null)
            {
                options.OnAuthenticationRefresh = onAuthenticationRefresh;
            }
        }

        return CreateHost(services =>
        {
            services.AddSingleton(activationTracker);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateActor = false,
                        ValidateLifetime = true,
                        // Lets a token omit "exp" entirely, which is how the dispatcher sees a
                        // non-expiring credential.
                        RequireExpirationTime = false,
                        IssuerSigningKey = tokenFactory.SecurityKey,
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Privileged", policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireClaim("privilege", "elevated");
                });
            });
        }, endpoints =>
        {
            endpoints.MapConnectionHandler<LowPrivilegeConnectionHandler>("/lowpriv", ConfigureEndpoint)
                .RequireAuthorization();
            endpoints.MapConnectionHandler<PrivilegedConnectionHandler>("/privileged", ConfigureEndpoint)
                .RequireAuthorization("Privileged");
        }, LoggerFactory);
    }

    private sealed class TestTokenFactory
    {
        private readonly JwtSecurityTokenHandler _handler = new();

        public SymmetricSecurityKey SecurityKey { get; } = new(SHA256.HashData(Guid.NewGuid().ToByteArray()));

        public string Create(string user, bool isPrivileged, DateTime? expiresUtc)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user) };
            if (isPrivileged)
            {
                claims.Add(new Claim("privilege", "elevated"));
            }

            // Distinguishes otherwise-identical tokens so the dispatcher sees a genuinely changed principal.
            claims.Add(new Claim("jti", Guid.NewGuid().ToString()));

            var token = new JwtSecurityToken(
                "SignalRTestServer",
                "SignalRTests",
                claims,
                expires: expiresUtc,
                signingCredentials: new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256));
            return _handler.WriteToken(token);
        }
    }

    private sealed class ConnectionActivationTracker
    {
        private readonly List<(string Endpoint, string UserId)> _activations = new();
        private readonly TaskCompletionSource<(string Endpoint, string UserId)> _firstActivation =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<(string Endpoint, string UserId)> FirstActivation => _firstActivation.Task;

        public IReadOnlyList<(string Endpoint, string UserId)> Activations
        {
            get
            {
                lock (_activations)
                {
                    return _activations.ToArray();
                }
            }
        }

        public void Record(string endpoint, ClaimsPrincipal user)
        {
            var activation = (endpoint, user?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            lock (_activations)
            {
                _activations.Add(activation);
            }

            _firstActivation.TrySetResult(activation);
        }
    }

    private abstract class TrackingConnectionHandler : ConnectionHandler
    {
        private readonly ConnectionActivationTracker _tracker;
        private readonly string _endpoint;

        protected TrackingConnectionHandler(ConnectionActivationTracker tracker, string endpoint)
        {
            _tracker = tracker;
            _endpoint = endpoint;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            _tracker.Record(_endpoint, connection.Features.Get<IConnectionUserFeature>()?.User);

            // Complete the waiting poll immediately so the test never depends on the poll timeout.
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes(_endpoint));

            while (true)
            {
                var result = await connection.Transport.Input.ReadAsync();
                try
                {
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    connection.Transport.Input.AdvanceTo(result.Buffer.End);
                }
            }
        }
    }

    private sealed class LowPrivilegeConnectionHandler : TrackingConnectionHandler
    {
        public LowPrivilegeConnectionHandler(ConnectionActivationTracker tracker)
            : base(tracker, "lowpriv")
        {
        }
    }

    private sealed class PrivilegedConnectionHandler : TrackingConnectionHandler
    {
        public PrivilegedConnectionHandler(ConnectionActivationTracker tracker)
            : base(tracker, "privileged")
        {
        }
    }
}
