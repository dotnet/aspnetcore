// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Tests;
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

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true });

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

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = false });

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "refresh_disabled");
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

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true });

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "missing_connection_token");
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

            await dispatcher.ExecuteRefreshAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true });

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "connection_not_found");
        }
    }

    [Fact]
    public async Task RefreshReturnsUnauthorizedWhenAuthenticationFails()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            // The authorization middleware produced a failed authentication result for this request.
            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Fail("Bad token")));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "invalid_token");
        }
    }

    [Fact]
    public async Task RefreshUpdatesConnectionUserAndReturnsTokenLifetime()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.User = originalUser;
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddMinutes(1);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var newExpires = DateTimeOffset.UtcNow.AddMinutes(30);
            var authProps = new AuthenticationProperties { ExpiresUtc = newExpires };
            var ticket = new AuthenticationTicket(newUser, authProps, "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            var json = ReadJson(context.Response.Body);
            var ttl = json.Value<int?>("tokenLifetimeSeconds");
            Assert.NotNull(ttl);
            Assert.InRange(ttl.Value, 1, 1801);

            Assert.Same(newUser, connection.User);
            // ExpiresUtc round-trips through AuthenticationProperties' whole-second ("r") format, so the
            // stored expiration is truncated to the second. Allow headroom so the sub-second truncation
            // can't push this over the tolerance.
            Assert.Equal(newExpires, connection.AuthenticationExpiration, TimeSpan.FromSeconds(2));
        }
    }

    [Fact]
    public async Task RefreshClampsTokenLifetimeToMaxInt()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            connection.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddMinutes(1);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var authProps = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddYears(100) };
            var ticket = new AuthenticationTicket(newUser, authProps, "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var json = ReadJson(context.Response.Body);
            Assert.Equal(int.MaxValue, json.Value<int?>("tokenLifetimeSeconds"));
        }
    }

    [Fact]
    public async Task RefreshSucceedsWithoutExpiresUtcOmitsTokenLifetime()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            // No ExpiresUtc on AuthenticationProperties => server should fall back to MaxValue and omit TTL
            var ticket = new AuthenticationTicket(newUser, new AuthenticationProperties(), "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            Assert.Null(json["tokenLifetimeSeconds"]);
            Assert.Empty(json.Properties());
            Assert.Equal(DateTimeOffset.MaxValue, connection.AuthenticationExpiration);
            Assert.Same(newUser, connection.User);
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public async Task RefreshRejectsWindowsIdentityPrincipal()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.User = originalUser;

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            using var windowsIdentity = WindowsIdentity.GetAnonymous();
            var newUser = new WindowsPrincipal(windowsIdentity);
            var authProps = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) };
            var ticket = new AuthenticationTicket(newUser, authProps, "Test");
            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "windows_identity_not_supported");
            Assert.Same(originalUser, connection.User);
        }
    }

    [Fact]
    public async Task RefreshUsesAuthenticateResultFeatureWithoutReauthenticating()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            connection.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddMinutes(1);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var newExpires = DateTimeOffset.UtcNow.AddMinutes(30);

            var services = new ServiceCollection();
            // Simulates a multi-scheme app with no default authenticate scheme: AuthenticateAsync() would throw.
            // The refresh handler must not call it because the authorization middleware already authenticated
            // the request against the endpoint's scheme and populated IAuthenticateResultFeature.
            services.AddSingleton<IAuthenticationService>(new ThrowingAuthenticationService());
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

            var props = new AuthenticationProperties { ExpiresUtc = newExpires };
            var ticket = new AuthenticationTicket(newUser, props, "Test");
            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Same(newUser, connection.User);
            // ExpiresUtc round-trips through AuthenticationProperties' whole-second ("r") format, so the
            // stored expiration is truncated to the second. Allow headroom so the sub-second truncation
            // can't push this over the tolerance.
            Assert.Equal(newExpires, connection.AuthenticationExpiration, TimeSpan.FromSeconds(2));
            var json = ReadJson(context.Response.Body);
            var ttl = json.Value<int?>("tokenLifetimeSeconds");
            Assert.NotNull(ttl);
            Assert.InRange(ttl.Value, 1, 1801);
        }
    }

    [Fact]
    public async Task RefreshDoesNotRollBackNewerPrincipalWhenOlderRefreshCompletesLast()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true }, negotiateVersion: 1);

            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "original") }, "Test"));
            connection.User = originalUser;
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddMinutes(1);

            var newerUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "newer") }, "Test"));
            var newerExpires = DateTimeOffset.UtcNow.AddMinutes(30);

            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = _ =>
                {
                    connection.UpdateUser(newerUser, newerExpires);
                    return ValueTask.FromResult(true);
                },
            };

            var olderUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "older") }, "Test"));
            var olderExpires = DateTimeOffset.UtcNow.AddMinutes(5);
            var ticket = new AuthenticationTicket(olderUser, new AuthenticationProperties { ExpiresUtc = olderExpires }, "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });
            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Same(newerUser, connection.User);
            Assert.Equal(newerExpires, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task RefreshReturnsUnauthorizedWhenAuthenticateResultFeatureMissing()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            // No IAuthenticateResultFeature is present (no authorization middleware ran for the endpoint).
            // /refresh relies on the middleware-produced result like the other endpoints and does not
            // re-authenticate, so it cannot refresh and returns 401.
            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "invalid_token");
        }
    }

    [Fact]
    public async Task RefreshRejectsNegotiateVersionZeroConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
            var connection = manager.CreateConnection(options, negotiateVersion: 0);
            // v0: ConnectionId == ConnectionToken — no private token, so /refresh must be rejected
            // to prevent any client that knows the public ConnectionId from refreshing on behalf of the connection.
            Assert.Equal(connection.ConnectionId, connection.ConnectionToken);

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionId,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(
                AuthenticateResult.Success(new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity("Test")), "Test"))));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "unsupported_negotiate_version");
        }
    }

    [Fact]
    public async Task RefreshOnDisposedConnectionReturnsNotFound()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var options = new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true };
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
            AssertRefreshError(json, "connection_not_found");
        }
    }

    [Fact]
    public async Task NegotiateSetsTokenLifetimeWhenAuthenticationRefreshEnabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = BuildNegotiateContext(out var body);
            var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(30);
            SetAuthenticateResultFeature(context, expiresUtc);

            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true });

            var response = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(body.ToArray()));
            var ttl = response.Value<int?>("tokenLifetimeSeconds");
            Assert.NotNull(ttl);
            Assert.InRange(ttl.Value, 1, 1801);
        }
    }

    [Fact]
    public async Task NegotiateDoesNotSetTokenLifetimeWhenAuthenticationRefreshDisabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = BuildNegotiateContext(out var body);
            SetAuthenticateResultFeature(context, DateTimeOffset.UtcNow.AddMinutes(30));

            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = false });

            var response = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(body.ToArray()));
            Assert.Null(response["tokenLifetimeSeconds"]);
        }
    }

    [Fact]
    public async Task NegotiateDoesNotSetTokenLifetimeWhenExpiresUtcMissing()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = BuildNegotiateContext(out var body);
            SetAuthenticateResultFeature(context, expiresUtc: null);

            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true });

            var response = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(body.ToArray()));
            Assert.Null(response["tokenLifetimeSeconds"]);
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public async Task NegotiateDoesNotSetTokenLifetimeForWindowsIdentity()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);
            var context = BuildNegotiateContext(out var body);
            using var windowsIdentity = WindowsIdentity.GetAnonymous();
            var principal = new WindowsPrincipal(windowsIdentity);
            var props = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) };
            var ticket = new AuthenticationTicket(principal, props, "Test");
            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteNegotiateAsync(context, new HttpConnectionDispatcherOptions { EnableAuthenticationRefresh = true });

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
    public async Task ScanClosesExpiredConnectionWhenAuthenticationRefreshEnabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                CloseOnAuthenticationExpiration = true,
                EnableAuthenticationRefresh = true,
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            connection.AuthenticationExpiration = DateTimeOffset.UtcNow.AddSeconds(-1);

            manager.Scan();

            await connection.ConnectionClosedRequested.WaitForCancellationAsync().DefaultTimeout();
        }
    }

    [Fact]
    public void HttpConnectionContextExposesUserRefreshFeature()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var feature = connection.Features.Get<IConnectionUserRefreshFeature>();

            Assert.NotNull(feature);
        }
    }

    [Fact]
    public void UpdateUserInvokesUserRefreshedCallbackWithNewPrincipal()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "old") }, "Test"));
            connection.User = originalUser;

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "new") }, "Test"));
            ClaimsPrincipal capturedCurrent = null;
            var feature = connection.Features.Get<IConnectionUserRefreshFeature>();
            Assert.NotNull(feature);
            feature.OnUserRefreshed((current, state) =>
            {
                capturedCurrent = current;
            }, state: null);

            connection.UpdateUser(newUser, DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.Same(newUser, capturedCurrent);
        }
    }

    [Fact]
    public void DisposedUserRefreshedRegistrationIsNotInvoked()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var feature = connection.Features.Get<IConnectionUserRefreshFeature>();
            Assert.NotNull(feature);

            var invoked = false;
            var registration = feature.OnUserRefreshed((_, state) => invoked = true, state: null);
            registration.Dispose();

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "new") }, "Test"));
            connection.UpdateUser(newUser, DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.False(invoked);
        }
    }

    [Fact]
    public void UpdateUserSwallowsExceptionFromUserRefreshedCallback()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "UserRefreshedCallbackFailed"))
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var feature = connection.Features.Get<IConnectionUserRefreshFeature>();
            Assert.NotNull(feature);
            feature.OnUserRefreshed((_, state) => throw new InvalidOperationException("boom"), state: null);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
            var expiration = DateTimeOffset.UtcNow.AddMinutes(15);

            connection.UpdateUser(newUser, expiration);

            Assert.Same(newUser, connection.User);
            Assert.Equal(expiration, connection.AuthenticationExpiration);
        }
    }

    [Fact]
    public async Task RefreshInvokesOnAuthenticationRefreshCallbackAndAcceptsWhenTrue()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            AuthenticationRefreshContext captured = null;
            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = ctx =>
                {
                    captured = ctx;
                    return ValueTask.FromResult(true);
                },
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.User = originalUser;

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var newExpires = DateTimeOffset.UtcNow.AddMinutes(30);
            var ticket = new AuthenticationTicket(newUser, new AuthenticationProperties { ExpiresUtc = newExpires }, "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Same(newUser, connection.User);

            Assert.NotNull(captured);
            Assert.Same(context, captured.HttpContext);
            Assert.Equal(connection.ConnectionId, captured.ConnectionId);
            Assert.Same(originalUser, captured.PreviousUser);
            Assert.Same(newUser, captured.NewUser);
            Assert.Equal(newExpires, captured.NewExpiration, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task RefreshReturnsForbiddenWhenOnAuthenticationRefreshReturnsFalse()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = ctx =>
                {
                    return ValueTask.FromResult(false);
                },
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);

            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.User = originalUser;
            var originalExpiration = DateTimeOffset.UtcNow.AddMinutes(2);
            connection.AuthenticationExpiration = originalExpiration;

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var ticket = new AuthenticationTicket(newUser, new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) }, "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "permission_change_rejected");

            // Connection state must NOT have been swapped.
            Assert.Same(originalUser, connection.User);
            Assert.Equal(originalExpiration, connection.AuthenticationExpiration);
        }
    }

    [Fact]
    public async Task RefreshDenyReturnsGenericError()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = _ => ValueTask.FromResult(false),
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            connection.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var ticket = new AuthenticationTicket(newUser, new AuthenticationProperties(), "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await dispatcher.ExecuteRefreshAsync(context, options);

            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            var json = ReadJson(context.Response.Body);
            AssertRefreshError(json, "permission_change_rejected");
        }
    }

    [Fact]
    public async Task RefreshOnAuthenticationRefreshCallbackExceptionPropagates()
    {
        using (StartVerifiableLog(expectedErrorsFilter: _ => true))
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = _ => throw new InvalidOperationException("boom"),
            };
            var connection = manager.CreateConnection(options, negotiateVersion: 1);
            var originalUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "old") }, "Test"));
            connection.User = originalUser;

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "new") }, "Test"));
            var ticket = new AuthenticationTicket(newUser, new AuthenticationProperties(), "Test");

            var context = new DefaultHttpContext();
            context.Request.Path = "/foo/refresh";
            context.Request.Method = "POST";
            context.Response.Body = new MemoryStream();
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = connection.ConnectionToken,
            });

            context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => dispatcher.ExecuteRefreshAsync(context, options));

            // Connection user untouched.
            Assert.Same(originalUser, connection.User);
        }
    }

    [Fact]
    public void UpdateUserWithNoCallbacksDoesNotThrow()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            // No callback registered with IConnectionUserRefreshFeature.OnUserRefreshed.
            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
            connection.UpdateUser(newUser, DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.Same(newUser, connection.User);
        }
    }

    [Fact]
    public void UpdateUserSetsUser()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var newUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("n", "v") }, "Test"));
            connection.UpdateUser(newUser, DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.Same(newUser, connection.User);
        }
    }

    [Fact]
    public async Task LongPollingRefreshedPrincipalInvokesOnAuthenticationRefreshAndUpdatesUser()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            AuthenticationRefreshContext captured = null;
            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = ctx =>
                {
                    captured = ctx;
                    return ValueTask.FromResult(true);
                },
            };

            var app = BuildTestConnectionHandlerApp(out var sp);

            // First poll establishes the original principal on the connection.
            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var expA = DateTimeOffset.UtcNow.AddMinutes(5);
            var context1 = BuildAuthPollContext(connection, sp, userA, expA);
            await dispatcher.ExecuteAsync(context1, options, app).DefaultTimeout();
            Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);
            Assert.Same(userA, connection.User);

            // Second poll carries a refreshed token (different subject and later expiration).
            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userB") }, "Test"));
            var expB = DateTimeOffset.UtcNow.AddMinutes(30);
            var context2 = BuildAuthPollContext(connection, sp, userB, expB);
            var pollTask = dispatcher.ExecuteAsync(context2, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Unblock")).AsTask().DefaultTimeout();
            await pollTask.DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);

            // The OnAuthenticationRefresh callback ran with the refreshed principal, and the connection was updated.
            Assert.NotNull(captured);
            Assert.Same(userA, captured.PreviousUser);
            Assert.Same(userB, captured.NewUser);
            Assert.Equal(expB, captured.NewExpiration, TimeSpan.FromSeconds(1));
            Assert.Same(userB, connection.User);
            Assert.Equal(expB, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task LongPollingRefreshedPrincipalRejectedByOnAuthenticationRefreshTearsDownConnection()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = ctx =>
                {
                    return ValueTask.FromResult(false);
                },
            };

            var app = BuildTestConnectionHandlerApp(out var sp);

            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var expA = DateTimeOffset.UtcNow.AddMinutes(5);
            var context1 = BuildAuthPollContext(connection, sp, userA, expA);
            await dispatcher.ExecuteAsync(context1, options, app).DefaultTimeout();
            Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);
            Assert.Same(userA, connection.User);

            // Second poll carries a refreshed token the application rejects.
            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userB") }, "Test"));
            var expB = DateTimeOffset.UtcNow.AddMinutes(30);
            var context2 = BuildAuthPollContext(connection, sp, userB, expB);
            await dispatcher.ExecuteAsync(context2, options, app).DefaultTimeout();

            Assert.Equal(StatusCodes.Status403Forbidden, context2.Response.StatusCode);
            var json = ReadJson(context2.Response.Body);
            AssertRefreshError(json, "permission_change_rejected");

            // The connection principal must NOT have been swapped to the rejected one.
            Assert.Same(userA, connection.User);

            // The connection is torn down and removed from the manager.
            Assert.NotNull(connection.DisposeAndRemoveTask);
            await connection.DisposeAndRemoveTask.DefaultTimeout();
            Assert.False(manager.TryGetConnection(connection.ConnectionToken, out _));
        }
    }

    [Fact]
    public async Task LongPollingPollCarryingSameTokenDoesNotInvokeOnAuthenticationRefresh()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var refreshCount = 0;
            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = _ =>
                {
                    refreshCount++;
                    return ValueTask.FromResult(true);
                },
            };

            var app = BuildTestConnectionHandlerApp(out var sp);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var exp = DateTimeOffset.UtcNow.AddMinutes(5);
            var context1 = BuildAuthPollContext(connection, sp, user, exp);
            await dispatcher.ExecuteAsync(context1, options, app).DefaultTimeout();
            Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);
            Assert.Same(user, connection.User);

            // Second poll carries the SAME token (same subject and expiration) but as a distinct principal
            // instance. This is not a refresh, so the callback should not run and the connection must NOT be
            // swapped to the new instance (which would re-introduce the check-then-act rollback race against a
            // concurrent /refresh); connection.User stays the principal already applied.
            var samePrincipalDifferentInstance = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var context2 = BuildAuthPollContext(connection, sp, samePrincipalDifferentInstance, exp);
            var pollTask = dispatcher.ExecuteAsync(context2, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Unblock")).AsTask().DefaultTimeout();
            await pollTask.DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            Assert.Equal(0, refreshCount);
            Assert.Same(user, connection.User);
        }
    }

    [Fact]
    public async Task LongPollingPollWithChangedClaimsAndSameExpirationInvokesOnAuthenticationRefresh()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            AuthenticationRefreshContext captured = null;
            var refreshCount = 0;
            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = ctx =>
                {
                    refreshCount++;
                    captured = ctx;
                    return ValueTask.FromResult(true);
                },
            };

            var app = BuildTestConnectionHandlerApp(out var sp);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "userA"),
                new Claim(ClaimTypes.Role, "reader"),
            }, "Test"));
            var exp = DateTimeOffset.UtcNow.AddMinutes(5);
            var context1 = BuildAuthPollContext(connection, sp, user, exp);
            await dispatcher.ExecuteAsync(context1, options, app).DefaultTimeout();
            Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);
            Assert.Same(user, connection.User);

            var changedClaimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "userA"),
                new Claim(ClaimTypes.Role, "writer"),
            }, "Test"));
            var context2 = BuildAuthPollContext(connection, sp, changedClaimsUser, exp);
            var pollTask = dispatcher.ExecuteAsync(context2, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Unblock")).AsTask().DefaultTimeout();
            await pollTask.DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            Assert.Equal(1, refreshCount);
            Assert.NotNull(captured);
            Assert.Same(user, captured.PreviousUser);
            Assert.Same(changedClaimsUser, captured.NewUser);
            Assert.Same(changedClaimsUser, connection.User);
            Assert.Equal(exp, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task LongPollingChangedUserRejectedWhenAuthenticationRefreshDisabled()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var refreshCount = 0;
            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = false,
                OnAuthenticationRefresh = _ =>
                {
                    refreshCount++;
                    return ValueTask.FromResult(true);
                },
            };

            var app = BuildTestConnectionHandlerApp(out var sp);

            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var context1 = BuildAuthPollContext(connection, sp, userA, DateTimeOffset.UtcNow.AddMinutes(5));
            await dispatcher.ExecuteAsync(context1, options, app).DefaultTimeout();
            Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);
            Assert.Same(userA, connection.User);

            // With authentication-refresh disabled the connection-hardening reject applies: a poll whose
            // NameIdentifier differs from the connection's user is rejected (403). The callback never runs,
            // the original user is preserved, and the connection is left intact for a later poll.
            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userB") }, "Test"));
            var context2 = BuildAuthPollContext(connection, sp, userB, DateTimeOffset.UtcNow.AddMinutes(30));
            await dispatcher.ExecuteAsync(context2, options, app).DefaultTimeout();

            Assert.Equal(StatusCodes.Status403Forbidden, context2.Response.StatusCode);
            Assert.Equal(0, refreshCount);
            Assert.Same(userA, connection.User);
            Assert.Null(connection.DisposeAndRemoveTask);
            Assert.NotEqual(HttpConnectionStatus.Disposed, connection.Status);
        }
    }

    [Fact]
    public async Task LongPollingStalePollDoesNotRollBackPrincipalOrExpiration()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var refreshCount = 0;
            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
                OnAuthenticationRefresh = _ =>
                {
                    refreshCount++;
                    return ValueTask.FromResult(true);
                },
            };

            var app = BuildTestConnectionHandlerApp(out var sp);

            // First poll establishes the principal with a later expiration (mimics a token applied by /refresh).
            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var laterExpiration = DateTimeOffset.UtcNow.AddMinutes(30);
            var context1 = BuildAuthPollContext(connection, sp, userA, laterExpiration);
            await dispatcher.ExecuteAsync(context1, options, app).DefaultTimeout();
            Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);
            Assert.Same(userA, connection.User);
            Assert.Equal(laterExpiration, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));

            // A delayed poll arrives carrying an OLDER token (earlier expiration) than the one already applied
            // (e.g. it lost the race against an explicit /refresh). It must not roll the connection back.
            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userB") }, "Test"));
            var earlierExpiration = DateTimeOffset.UtcNow.AddMinutes(5);
            var context2 = BuildAuthPollContext(connection, sp, userB, earlierExpiration);
            var pollTask = dispatcher.ExecuteAsync(context2, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Unblock")).AsTask().DefaultTimeout();
            await pollTask.DefaultTimeout();

            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            // No refresh callback fired and the connection kept the newer principal/expiration.
            Assert.Equal(0, refreshCount);
            Assert.Same(userA, connection.User);
            Assert.Equal(laterExpiration, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task LongPollingRejectionDoesNotTearDownConnectionWhenTokenSupersededDuringCallback()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection();
            connection.TransportType = HttpTransportType.LongPolling;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var app = BuildTestConnectionHandlerApp(out var sp);

            // First poll establishes the principal.
            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var initialExpiration = DateTimeOffset.UtcNow.AddMinutes(5);
            var context1 = BuildAuthPollContext(connection, sp, userA, initialExpiration);

            var options = new HttpConnectionDispatcherOptions
            {
                EnableAuthenticationRefresh = true,
            };

            await dispatcher.ExecuteAsync(context1, options, app).DefaultTimeout();
            Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);

            // A delayed poll carrying an older token runs the callback, but while the (slow) callback runs an
            // explicit /refresh applies a NEWER token. The callback then rejects the older token. The
            // connection must not be torn down for a token it has already moved past.
            var newerUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userC") }, "Test"));
            var newerExpiration = DateTimeOffset.UtcNow.AddMinutes(30);
            options.OnAuthenticationRefresh = ctx =>
            {
                // Simulate a concurrent /refresh landing a newer token mid-callback.
                connection.UpdateUser(newerUser, newerExpiration);
                return ValueTask.FromResult(false);
            };

            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userB") }, "Test"));
            var context2 = BuildAuthPollContext(connection, sp, userB, DateTimeOffset.UtcNow.AddMinutes(10));
            var pollTask = dispatcher.ExecuteAsync(context2, options, app);
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Unblock")).AsTask().DefaultTimeout();
            await pollTask.DefaultTimeout();

            // The poll proceeds (200), the connection is not torn down, and it keeps the newer token.
            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            Assert.Null(connection.DisposeAndRemoveTask);
            Assert.Same(newerUser, connection.User);
            Assert.Equal(newerExpiration, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task StatefulReconnectRefreshedPrincipalInvokesOnAuthenticationRefreshAndUpdatesUser()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                AllowStatefulReconnects = true,
                EnableAuthenticationRefresh = true,
            };
            options.WebSockets.CloseTimeout = TimeSpan.FromMilliseconds(1);
            var connection = manager.CreateConnection(options, negotiateVersion: 1, useStatefulReconnect: true);
            connection.TransportType = HttpTransportType.WebSockets;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            AuthenticationRefreshContext captured = null;
            options.OnAuthenticationRefresh = ctx =>
            {
                captured = ctx;
                return ValueTask.FromResult(true);
            };

            var services = new ServiceCollection();
            var app = BuildReconnectConnectionHandlerApp(services);

            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var expA = DateTimeOffset.UtcNow.AddMinutes(5);
            var context1 = BuildAuthReconnectContext(connection, services, userA, expA);
            var initialWebSocketTask = dispatcher.ExecuteAsync(context1, options, app);
            var websocketFeature = (TestWebSocketConnectionFeature)context1.Features.Get<IHttpWebSocketFeature>();
            await websocketFeature.Accepted.DefaultTimeout();

            Assert.Same(userA, connection.User);
            Assert.Equal(expA, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));

            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "userA"),
                new Claim(ClaimTypes.Role, "writer"),
            }, "Test"));
            var expB = DateTimeOffset.UtcNow.AddMinutes(30);
            var context2 = BuildAuthReconnectContext(connection, services, userB, expB);
            var reconnectWebSocketTask = dispatcher.ExecuteAsync(context2, options, app);

            await initialWebSocketTask.DefaultTimeout();
            websocketFeature = (TestWebSocketConnectionFeature)context2.Features.Get<IHttpWebSocketFeature>();
            await websocketFeature.Accepted.DefaultTimeout();

            Assert.NotNull(captured);
            Assert.Same(userA, captured.PreviousUser);
            Assert.Same(userB, captured.NewUser);
            Assert.Equal(expB, captured.NewExpiration, TimeSpan.FromSeconds(1));
            Assert.Same(userB, connection.User);
            Assert.Equal(expB, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));

            connection.Abort();
            await reconnectWebSocketTask.DefaultTimeout();
        }
    }

    [Fact]
    public async Task StatefulReconnectCarryingSameTokenDoesNotInvokeOnAuthenticationRefresh()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var options = new HttpConnectionDispatcherOptions
            {
                AllowStatefulReconnects = true,
                EnableAuthenticationRefresh = true,
            };
            options.WebSockets.CloseTimeout = TimeSpan.FromMilliseconds(1);
            var connection = manager.CreateConnection(options, negotiateVersion: 1, useStatefulReconnect: true);
            connection.TransportType = HttpTransportType.WebSockets;
            var dispatcher = CreateDispatcher(manager, LoggerFactory);

            var refreshCount = 0;
            options.OnAuthenticationRefresh = _ =>
            {
                refreshCount++;
                return ValueTask.FromResult(true);
            };

            var services = new ServiceCollection();
            var app = BuildReconnectConnectionHandlerApp(services);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var exp = DateTimeOffset.UtcNow.AddMinutes(5);
            var context1 = BuildAuthReconnectContext(connection, services, user, exp);
            var initialWebSocketTask = dispatcher.ExecuteAsync(context1, options, app);
            var websocketFeature = (TestWebSocketConnectionFeature)context1.Features.Get<IHttpWebSocketFeature>();
            await websocketFeature.Accepted.DefaultTimeout();

            Assert.Same(user, connection.User);

            var samePrincipalDifferentInstance = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "userA") }, "Test"));
            var context2 = BuildAuthReconnectContext(connection, services, samePrincipalDifferentInstance, exp);
            var reconnectWebSocketTask = dispatcher.ExecuteAsync(context2, options, app);

            await initialWebSocketTask.DefaultTimeout();
            websocketFeature = (TestWebSocketConnectionFeature)context2.Features.Get<IHttpWebSocketFeature>();
            await websocketFeature.Accepted.DefaultTimeout();

            Assert.Equal(0, refreshCount);
            Assert.Same(user, connection.User);
            Assert.Same(user, connection.HttpContext.User);

            connection.Abort();
            await reconnectWebSocketTask.DefaultTimeout();
        }
    }

    [Fact]
    public void UpdateUserSkipsOlderToken()
    {
        using (StartVerifiableLog())
        {
            var manager = CreateConnectionManager(LoggerFactory);
            var connection = manager.CreateConnection(new HttpConnectionDispatcherOptions(), negotiateVersion: 1);

            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "A") }, "Test"));
            var laterExpiration = DateTimeOffset.UtcNow.AddMinutes(30);
            connection.UpdateUser(userA, laterExpiration);

            var feature = connection.Features.Get<IConnectionUserRefreshFeature>();
            var notified = 0;
            feature.OnUserRefreshed((_, state) => notified++, state: null);

            // An older token must be skipped (no swap, no notification).
            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "B") }, "Test"));
            connection.UpdateUser(userB, DateTimeOffset.UtcNow.AddMinutes(5));

            Assert.Same(userA, connection.User);
            Assert.Equal(laterExpiration, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
            Assert.Equal(0, notified);

            // A newer token is still applied.
            var userC = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("name", "C") }, "Test"));
            var newerExpiration = DateTimeOffset.UtcNow.AddMinutes(60);
            connection.UpdateUser(userC, newerExpiration);

            Assert.Same(userC, connection.User);
            Assert.Equal(newerExpiration, connection.AuthenticationExpiration, TimeSpan.FromSeconds(1));
            Assert.Equal(1, notified);
        }
    }

    private static ConnectionDelegate BuildTestConnectionHandlerApp(out IServiceProvider serviceProvider)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<TestConnectionHandler>();
        services.AddLogging();
        serviceProvider = services.BuildServiceProvider();
        var builder = new ConnectionBuilder(serviceProvider);
        builder.UseConnectionHandler<TestConnectionHandler>();
        return builder.Build();
    }

    private static ConnectionDelegate BuildReconnectConnectionHandlerApp(IServiceCollection services)
    {
        var builder = new ConnectionBuilder(services.BuildServiceProvider());
        builder.UseConnectionHandler<AuthenticationRefreshReconnectConnectionHandler>();
        return builder.Build();
    }

    private static DefaultHttpContext BuildAuthPollContext(HttpConnectionContext connection, IServiceProvider serviceProvider, ClaimsPrincipal user, DateTimeOffset expiration)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/foo";
        context.Request.Method = "GET";
        context.RequestServices = serviceProvider;
        context.Response.Body = new MemoryStream();
        var values = new Dictionary<string, StringValues>
        {
            ["id"] = connection.ConnectionToken,
            ["negotiateVersion"] = "1",
        };
        context.Request.Query = new QueryCollection(values);
        context.User = user;

        var props = new AuthenticationProperties { ExpiresUtc = expiration };
        var ticket = new AuthenticationTicket(user, props, "Test");
        context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));
        return context;
    }

    private static DefaultHttpContext BuildAuthReconnectContext(HttpConnectionContext connection, IServiceCollection services, ClaimsPrincipal user, DateTimeOffset expiration)
    {
        var context = MakeRequest("/foo", connection, services);
        SetTransport(context, HttpTransportType.WebSockets);
        context.User = user;

        var props = new AuthenticationProperties { ExpiresUtc = expiration };
        var ticket = new AuthenticationTicket(user, props, "Test");
        context.Features.Set<IAuthenticateResultFeature>(new TestAuthenticateResultFeature(AuthenticateResult.Success(ticket)));
        return context;
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

    private static void AssertRefreshError(JObject json, string error)
    {
        Assert.Equal(error, json.Value<string>("error"));
        Assert.Null(json["error_description"]);
    }

    private sealed class ThrowingAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) =>
            throw new InvalidOperationException("No authenticationScheme was specified, and there was no DefaultAuthenticateScheme found.");
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

    private sealed class AuthenticationRefreshReconnectConnectionHandler : ConnectionHandler
    {
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
#pragma warning disable CA2252 // This API requires opting into preview features
            connection.Features.Get<IStatefulReconnectFeature>()?.OnReconnected(_ => Task.CompletedTask);
#pragma warning restore CA2252 // This API requires opting into preview features

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, connection.ConnectionClosed);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
