// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public partial class HttpConnectionDispatcherTests
{
    [Fact]
    public async Task RefreshReturnsMethodNotAllowedForNonPost()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthRefresh = true });

            Assert.Equal(StatusCodes.Status405MethodNotAllowed, context.Response.StatusCode);
        }
    }

    [Fact]
    public async Task RefreshReturnsNotFoundWhenDisabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthRefresh = false });

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            var json = ReadJson(context.Response.Body);
            Assert.Equal("refresh_disabled", json.Value<string>("error"));
            Assert.False(string.IsNullOrEmpty(json.Value<string>("error_description")));
        }
    }

    [Fact]
    public async Task RefreshReturnsBadRequestWhenMissingConnectionToken()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthRefresh = true });

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            Assert.Equal("missing_connection_token", json.Value<string>("error"));
        }
    }

    [Fact]
    public async Task RefreshReturnsNotFoundWhenConnectionDoesNotExist()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = "does-not-exist",
            });

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthRefresh = true });

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            Assert.Equal("connection_not_found", json.Value<string>("error"));
        }
    }

    [Fact]
    public async Task RefreshReturnsUnauthorizedWhenAuthenticationFails()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var services = new ServiceCollection();
            services.AddSingleton<IAuthenticationService>(new FakeAuthenticationService(
                AuthenticateResult.Fail("Bad token")));
            services.AddSingleton<IAuthenticationSchemeProvider>(new FakeAuthenticationSchemeProvider());

            var context = new DefaultHttpContext();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            Assert.Equal("invalid_token", json.Value<string>("error"));
        }
    }

    [Fact]
    public async Task RefreshUpdatesConnectionUserAndReturnsTokenLifetime()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.User = originalUser;
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddMinutes(1);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var newExpires = DateTimeOffset.UtcNow.AddMinutes(30);
            var authProps = new AuthenticationProperties { ExpiresUtc = newExpires };
            var ticket = new AuthenticationTicket(newUser, authProps, "Test");

            var services = new ServiceCollection();
            services.AddSingleton<IAuthenticationService>(new FakeAuthenticationService(
                AuthenticateResult.Success(ticket)));
            services.AddSingleton<IAuthenticationSchemeProvider>(new FakeAuthenticationSchemeProvider());

            var context = new DefaultHttpContext();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            var json = ReadJson(context.Response.Body);
            Assert.Equal(connection.ConnectionId, json.Value<string>("connectionId"));
            Assert.False(string.IsNullOrEmpty(json.Value<string>("refreshedAt")));
            var ttl = json.Value<int?>("tokenLifetimeSeconds");
            Assert.NotNull(ttl);
            Assert.InRange(ttl.Value, 1, 1801);

            Assert.Same(newUser, connection.User);
            Assert.Equal(newExpires, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task RefreshSucceedsWithoutExpiresUtcOmitsTokenLifetimeSeconds()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            // No ExpiresUtc on AuthenticationProperties => server should fall back to MaxValue and omit TTL
            var ticket = new AuthenticationTicket(newUser, new AuthenticationProperties(), "Test");

            var services = new ServiceCollection();
            services.AddSingleton<IAuthenticationService>(new FakeAuthenticationService(
                AuthenticateResult.Success(ticket)));
            services.AddSingleton<IAuthenticationSchemeProvider>(new FakeAuthenticationSchemeProvider());

            var context = new DefaultHttpContext();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            Assert.Null(json["tokenLifetimeSeconds"]);
            Assert.Equal(connection.ConnectionId, json.Value<string>("connectionId"));
            Assert.Equal(DateTimeOffset.MaxValue, connection.AuthenticationExpiration);
            Assert.Same(newUser, connection.User);
        }
    }

    [Fact]
    public async Task RefreshWorksWithNegotiateVersionZero()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 0);
            // Sanity: with v0, id == token
            Assert.Equal(connection.ConnectionId, connection.ConnectionToken);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
            var ticket = new AuthenticationTicket(newUser, new AuthenticationProperties(), "Test");

            var services = new ServiceCollection();
            services.AddSingleton<IAuthenticationService>(new FakeAuthenticationService(
                AuthenticateResult.Success(ticket)));
            services.AddSingleton<IAuthenticationSchemeProvider>(new FakeAuthenticationSchemeProvider());

            var context = new DefaultHttpContext();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionId,
            });

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Same(newUser, connection.User);
        }
    }

    [Fact]
    public async Task RefreshOnDisposedConnectionReturnsNotFound()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            var token = connection.ConnectionToken;

            // Remove the connection from the manager (mimics dispose/timeout cleanup)
            manager.RemoveConnection(token, HttpTransportType.None, HttpConnectionStopStatus.NormalClosure);

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = token,
            });

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            Assert.Equal("connection_not_found", json.Value<string>("error"));
        }
    }

    [Fact]
    public async Task NegotiateSetsTokenLifetimeSecondsWhenAuthRefreshEnabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = BuildNegotiateContext(out var body);
            var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(30);
            SetAuthenticateResultFeature(context, expiresUtc);

            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { EnableAuthRefresh = true });

            var response = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(body.ToArray()));
            var ttl = response.Value<int?>("tokenLifetimeSeconds");
            Assert.NotNull(ttl);
            Assert.InRange(ttl.Value, 1, 1801);
        }
    }

    [Fact]
    public async Task NegotiateDoesNotSetTokenLifetimeSecondsWhenAuthRefreshDisabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = BuildNegotiateContext(out var body);
            SetAuthenticateResultFeature(context, DateTimeOffset.UtcNow.AddMinutes(30));

            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { EnableAuthRefresh = false });

            var response = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(body.ToArray()));
            Assert.Null(response["tokenLifetimeSeconds"]);
        }
    }

    [Fact]
    public async Task NegotiateDoesNotSetTokenLifetimeSecondsWhenExpiresUtcMissing()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = BuildNegotiateContext(out var body);
            SetAuthenticateResultFeature(context, expiresUtc: null);

            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { EnableAuthRefresh = true });

            var response = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(body.ToArray()));
            Assert.Null(response["tokenLifetimeSeconds"]);
        }
    }

    [Fact]
    public void UpdateUserSetsUserAndAuthenticationExpiration()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
            var expiration = DateTimeOffset.UtcNow.AddMinutes(15);

            // HttpContext is not set on this connection; UpdateUser should still update User + expiration.
            connection.UpdateUser(user, expiration);

            Assert.Same(user, connection.User);
            Assert.Equal(expiration, connection.AuthenticationExpiration);
        }
    }

    [Fact]
    public void UpdateUserAlsoUpdatesHttpContextUserWhenPresent()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "old") }, "Test"));
            connection.HttpContext = httpContext;

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "new") }, "Test"));
            connection.UpdateUser(newUser, DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.Same(newUser, connection.User);
            Assert.Same(newUser, httpContext.User);
        }
    }

    [Fact]
    public void ScanDoesNotCloseExpiredConnectionDuringAuthRefreshGracePeriod()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                CloseOnAuthenticationExpiration = true,
                EnableAuthRefresh = true,
                AuthRefreshGracePeriod = TimeSpan.FromMinutes(5),
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            // Expired 1 minute ago, still inside the 5-minute grace period.
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddMinutes(-1);

            manager.Scan();

            Assert.False(connection.ConnectionClosedRequested.IsCancellationRequested);
        }
    }

    [Fact]
    public void ScanClosesExpiredConnectionAfterAuthRefreshGracePeriod()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                CloseOnAuthenticationExpiration = true,
                EnableAuthRefresh = true,
                AuthRefreshGracePeriod = TimeSpan.FromMinutes(1),
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            // Expired well outside the grace period.
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddMinutes(-10);

            manager.Scan();

            // RequestClose() queues cancellation on the ThreadPool; wait for it.
            Assert.True(connection.ConnectionClosedRequested.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
        }
    }

    [Fact]
    public void ScanClosesExpiredConnectionImmediatelyWhenAuthRefreshDisabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                CloseOnAuthenticationExpiration = true,
                EnableAuthRefresh = false,
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            // Expired 1 second ago — no grace period since auth refresh is off.
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddSeconds(-1);

            manager.Scan();

            Assert.True(connection.ConnectionClosedRequested.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
        }
    }

    [Fact]
    public void HttpConnectionContextExposesUserUpdateFeature()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var feature = connection.Features.Get<IConnectionUserUpdateFeature>();

            Assert.NotNull(feature);
        }
    }

    [Fact]
    public void UpdateUserRaisesUserUpdatedEventWithNewPrincipal()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "old") }, "Test"));
            connection.User = originalUser;

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "new") }, "Test"));
            ClaimsPrincipal capturedCurrent = null;
            var feature = connection.Features.Get<IConnectionUserUpdateFeature>();
            Assert.NotNull(feature);
            feature.UserUpdated += current =>
            {
                capturedCurrent = current;
            };

            connection.UpdateUser(newUser, DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.Same(newUser, capturedCurrent);
        }
    }

    [Fact]
    public void UpdateUserSwallowsExceptionFromUserUpdatedHandler()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "UserUpdatedHandlerFailed"))
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var feature = connection.Features.Get<IConnectionUserUpdateFeature>();
            Assert.NotNull(feature);
            feature.UserUpdated += _ => throw new InvalidOperationException("boom");

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
            var expiration = DateTimeOffset.UtcNow.AddMinutes(15);

            connection.UpdateUser(newUser, expiration);

            Assert.Same(newUser, connection.User);
            Assert.Equal(expiration, connection.AuthenticationExpiration);
        }
    }

    [Fact]
    public void UpdateUserNotifiesAllSubscribersInOrder()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var feature = connection.Features.Get<IConnectionUserUpdateFeature>();
            Assert.NotNull(feature);

            var order = new List<int>();
            feature.UserUpdated += _ => { lock (order) { order.Add(1); } };
            feature.UserUpdated += _ => { lock (order) { order.Add(2); } };
            feature.UserUpdated += _ => { lock (order) { order.Add(3); } };

            connection.UpdateUser(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test")),
                DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.Equal(new[] { 1, 2, 3 }, order);
        }
    }

    [Fact]
    public void UpdateUserDoesNotInvokeUnsubscribedHandlers()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var feature = connection.Features.Get<IConnectionUserUpdateFeature>();
            Assert.NotNull(feature);

            var calls = 0;
            void Handler(ClaimsPrincipal current) => calls++;
            feature.UserUpdated += Handler;

            connection.UpdateUser(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "1") }, "Test")),
                DateTimeOffset.UtcNow.AddMinutes(15));
            Assert.Equal(1, calls);

            feature.UserUpdated -= Handler;

            connection.UpdateUser(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "2") }, "Test")),
                DateTimeOffset.UtcNow.AddMinutes(15));
            Assert.Equal(1, calls);
        }
    }

    [Fact]
    public void UpdateUserWithNoSubscribersDoesNotThrow()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            // No subscriber attached to IConnectionUserUpdateFeature.UserUpdated.
            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
            connection.UpdateUser(newUser, DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.Same(newUser, connection.User);
        }
    }

    private static DefaultHttpContext BuildNegotiateContext(out MemoryStream body)
    {
        body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Request.Path = "/foo";
        context.Request.Method = "POST";
        context.Request.QueryString = new QueryString("?negotiateVersion=1");
        context.Response.Body = body;
        var services = new ServiceCollection();
        services.AddOptions();
        context.RequestServices = services.BuildServiceProvider();
        return context;
    }

    private static void SetAuthenticateResultFeature(HttpContext context, DateTimeOffset? expiresUtc)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
        var props = new AuthenticationProperties();
        if (expiresUtc.HasValue)
        {
            props.ExpiresUtc = expiresUtc.Value;
        }
        var ticket = new AuthenticationTicket(principal, props, "Test");
        context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));
    }

    private sealed class TestAuthenticateResultFeature : IAuthenticateResultFeature
    {
        public TestAuthenticateResultFeature(AuthenticateResult result) => AuthenticateResult = result;
        public AuthenticateResult AuthenticateResult { get; set; }
    }

    private static JObject ReadJson(Stream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body);
        return JObject.Parse(reader.ReadToEnd());
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        private readonly AuthenticateResult _result;

        public FakeAuthenticationService(AuthenticateResult result)
        {
            _result = result;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) => Task.FromResult(_result);
        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
    }

    private sealed class FakeAuthenticationSchemeProvider : IAuthenticationSchemeProvider
    {
        public Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync() => Task.FromResult<AuthenticationScheme>(null);
        public Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync() => Task.FromResult<AuthenticationScheme>(null);
        public Task<AuthenticationScheme> GetDefaultForbidSchemeAsync() => Task.FromResult<AuthenticationScheme>(null);
        public Task<AuthenticationScheme> GetDefaultSignInSchemeAsync() => Task.FromResult<AuthenticationScheme>(null);
        public Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync() => Task.FromResult<AuthenticationScheme>(null);
        public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync() => Task.FromResult(Enumerable.Empty<AuthenticationScheme>());
        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync() => Task.FromResult(Enumerable.Empty<AuthenticationScheme>());
        public Task<AuthenticationScheme> GetSchemeAsync(string name) => Task.FromResult<AuthenticationScheme>(null);
        public void AddScheme(AuthenticationScheme scheme) { }
        public void RemoveScheme(string name) { }
    }
}
