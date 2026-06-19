// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Fact]
    public async Task UserPropertyReflectsLatestPrincipalAfterUserRefreshedCallback()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var firstNameClaim = await client.InvokeAsync(nameof(AuthenticationRefreshHub.GetUserName)).DefaultTimeout();
                Assert.Null(firstNameClaim.Error);
                var originalName = (string?)firstNameClaim.Result;
                Assert.False(string.IsNullOrEmpty(originalName));

                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "refreshed-user"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);
                await hubObserver.RefreshedTask.DefaultTimeout();

                var secondNameClaim = await client.InvokeAsync(nameof(AuthenticationRefreshHub.GetUserName)).DefaultTimeout();
                Assert.Null(secondNameClaim.Error);
                Assert.Equal("refreshed-user", secondNameClaim.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task UserIdentifierIsAvailableBeforeAuthenticationRefresh()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient(userIdentifier: "initial-user"))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var pair = await client.InvokeAsync(nameof(AuthenticationRefreshHub.GetUserNameIdentifierAndUserIdentifier)).DefaultTimeout();
                Assert.Null(pair.Error);
                Assert.Equal("initial-user:initial-user", pair.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task UserRefreshedFeatureCallbackDispatchesOnAuthenticationRefreshedAsyncToHub()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "refreshed"),
                    new Claim("scope", "extra"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);

                var captured = await hubObserver.RefreshedTask.DefaultTimeout();
                Assert.Equal("refreshed", captured);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task OnAuthenticationRefreshedAsyncSerializesWithInFlightHubInvocation()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var invokeTask = client.InvokeAsync(nameof(AuthenticationRefreshHub.BlockUntilSignaled));
                await hubObserver.HubMethodStarted.Task.DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "after") }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);

                Assert.False(hubObserver.RefreshedTask.IsCompleted);
                await Task.Delay(50);
                Assert.False(hubObserver.RefreshedTask.IsCompleted);

                hubObserver.ReleaseHubMethod.SetResult();
                var completion = await invokeTask.DefaultTimeout();
                Assert.Null(completion.Error);
                Assert.Equal(previousUser.Identity?.Name, await hubObserver.BlockedMethodUserNameAfterRelease.Task.DefaultTimeout());

                var captured = await hubObserver.RefreshedTask.DefaultTimeout();
                Assert.Equal("after", captured);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task RefreshWithSameUserIdentifierDoesNotAbortAndDispatches()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient(userIdentifier: "stable-user"))
            {
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                // Same NameIdentifier (so DefaultUserIdProvider returns the same id), different other claims.
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "stable-user"),
                    new Claim("role", "admin"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);

                await hubObserver.RefreshedTask.DefaultTimeout();

                var seenUser = await client.InvokeAsync(nameof(AuthenticationRefreshHub.GetUserClaim), "role").DefaultTimeout();
                Assert.Null(seenUser.Error);
                Assert.Equal("admin", seenUser.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task ExceptionFromOnAuthenticationRefreshedAsyncIsLoggedAndConnectionSurvives()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "FailedInvokingHubMethod"))
        {
            var hubObserver = new AuthenticationRefreshObserver { ThrowFromOnAuthenticationRefreshed = true };
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "after"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);

                // Wait until the hub method ran (and threw).
                await hubObserver.RefreshedTask.DefaultTimeout();

                // Semaphore must have been released — a subsequent invocation should complete.
                var nameResult = await client.InvokeAsync(nameof(AuthenticationRefreshHub.GetUserName)).DefaultTimeout();
                Assert.Null(nameResult.Error);
                Assert.Equal("after", nameResult.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task MultipleRefreshesEachDispatchInOrder()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver { CaptureAll = true };
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var user1 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "u1") }, "Test"));
                var user2 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "u2") }, "Test"));
                var user3 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "u3") }, "Test"));

                client.Connection.User = user1;
                feature.Raise(user1);
                client.Connection.User = user2;
                feature.Raise(user2);
                client.Connection.User = user3;
                feature.Raise(user3);

                await hubObserver.AllRefreshesCompleted(3).DefaultTimeout();
                var captured = hubObserver.AllCapturedUserNames;
                Assert.Equal(3, captured.Count);
                Assert.Equal("u1", captured[0]);
                Assert.Equal("u2", captured[1]);
                Assert.Equal("u3", captured[2]);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task ConnectionWithoutUserRefreshFeatureStillWorks()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(_ => { }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                Assert.Null(client.Connection.Features.Get<IConnectionUserRefreshFeature>());

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var completion = await client.InvokeAsync(nameof(MethodHub.Echo), "ping").DefaultTimeout();
                Assert.Null(completion.Error);
                Assert.Equal("ping", completion.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task UserIdentifierChangeOnRefreshAbortsConnection()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "UserIdentifierChangedOnRefresh"))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(_ => { }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient(userIdentifier: "user-1"))
            {
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-2"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task RefreshAddingRequiredClaimAllowsAuthorizedHubMethod()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(hubObserver);
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("scope-policy", policy =>
                    {
                        policy.RequireClaim("scope", "admin");
                        policy.AddAuthenticationSchemes("Default");
                    });
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient(userIdentifier: "alice"))
            {
                client.Connection.User!.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "alice") }));
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                // No "scope" claim yet -> policy denies.
                var denied = await client.InvokeAsync(nameof(AuthenticationRefreshHub.ScopeProtected)).DefaultTimeout();
                Assert.NotNull(denied.Error);
                Assert.Contains("Failed to invoke", denied.Error);

                // Refresh adds the required claim while preserving NameIdentifier so UserIdentifier doesn't change.
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "alice"),
                    new Claim("scope", "admin"),
                }, "Default"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);
                await hubObserver.RefreshedTask.DefaultTimeout();

                var allowed = await client.InvokeAsync(nameof(AuthenticationRefreshHub.ScopeProtected)).DefaultTimeout();
                Assert.Null(allowed.Error);
                Assert.Equal("ok", allowed.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task RefreshRemovingRequiredClaimBlocksAuthorizedHubMethod()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthenticationRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSingleton(hubObserver);
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("scope-policy", policy =>
                    {
                        policy.RequireClaim("scope", "admin");
                        policy.AddAuthenticationSchemes("Default");
                    });
                });
            }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthenticationRefreshHub>>();

            using (var client = new TestClient(userIdentifier: "alice"))
            {
                client.Connection.User!.AddIdentity(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "alice"),
                    new Claim("scope", "admin"),
                }));
                var feature = new TestConnectionUserRefreshFeature();
                client.Connection.Features.Set<IConnectionUserRefreshFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var allowed = await client.InvokeAsync(nameof(AuthenticationRefreshHub.ScopeProtected)).DefaultTimeout();
                Assert.Null(allowed.Error);
                Assert.Equal("ok", allowed.Result);

                // Refresh drops the scope claim but keeps NameIdentifier so the connection isn't aborted.
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "alice"),
                }, "Default"));
                client.Connection.User = refreshedUser;
                feature.Raise(refreshedUser);
                await hubObserver.RefreshedTask.DefaultTimeout();

                var denied = await client.InvokeAsync(nameof(AuthenticationRefreshHub.ScopeProtected)).DefaultTimeout();
                Assert.NotNull(denied.Error);
                Assert.Contains("Failed to invoke", denied.Error);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    private sealed class TestConnectionUserRefreshFeature : IConnectionUserRefreshFeature
    {
        private Action<ClaimsPrincipal, object?>? _callback;
        private object? _state;

        public IDisposable OnUserRefreshed(Action<ClaimsPrincipal, object?> callback, object? state)
        {
            _callback = callback;
            _state = state;

            return new CallbackRegistration(this);
        }

        public void Raise(ClaimsPrincipal current)
        {
            _callback?.Invoke(current, _state);
        }

        private sealed class CallbackRegistration(TestConnectionUserRefreshFeature feature) : IDisposable
        {
            public void Dispose()
            {
                feature._callback = null;
                feature._state = null;
            }
        }
    }

    private sealed class AuthenticationRefreshObserver
    {
        private readonly TaskCompletionSource<string?> _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<string?> _allCaptured = new();
        private readonly object _allLock = new();
        private TaskCompletionSource? _allTcs;
        private int _waitedCount;

        public Task<string?> RefreshedTask => _tcs.Task;

        public TaskCompletionSource HubMethodStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseHubMethod { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<string?> BlockedMethodUserNameAfterRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ThrowFromOnAuthenticationRefreshed { get; set; }

        public bool CaptureAll { get; set; }

        public IReadOnlyList<string?> AllCapturedUserNames
        {
            get { lock (_allLock) { return _allCaptured.ToArray(); } }
        }

        public Task AllRefreshesCompleted(int count)
        {
            lock (_allLock)
            {
                _waitedCount = count;
                _allTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                if (_allCaptured.Count >= count)
                {
                    _allTcs.TrySetResult();
                }
                return _allTcs.Task;
            }
        }

        public void Capture(string? contextUserName)
        {
            if (CaptureAll)
            {
                lock (_allLock)
                {
                    _allCaptured.Add(contextUserName);
                    if (_allTcs is not null && _allCaptured.Count >= _waitedCount)
                    {
                        _allTcs.TrySetResult();
                    }
                }
            }

            _tcs.TrySetResult(contextUserName);
        }
    }

    private sealed class AuthenticationRefreshHub : Hub
    {
        private readonly AuthenticationRefreshObserver _observer;

        public AuthenticationRefreshHub(AuthenticationRefreshObserver observer)
        {
            _observer = observer;
        }

        public string? GetUserName() => Context.User?.Identity?.Name;

        public string? GetUserClaim(string type) => Context.User?.FindFirst(type)?.Value;

        public string? GetUserNameIdentifierAndUserIdentifier()
        {
            return $"{Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value}:{Context.UserIdentifier}";
        }

        [Authorize("scope-policy")]
        public string ScopeProtected() => "ok";

        public async Task BlockUntilSignaled()
        {
            _observer.HubMethodStarted.TrySetResult();
            await _observer.ReleaseHubMethod.Task;
            _observer.BlockedMethodUserNameAfterRelease.TrySetResult(Context.User?.Identity?.Name);
        }

        public override Task OnAuthenticationRefreshedAsync()
        {
            _observer.Capture(Context.User?.Identity?.Name);
            if (_observer.ThrowFromOnAuthenticationRefreshed)
            {
                throw new InvalidOperationException("boom from OnAuthenticationRefreshedAsync");
            }
            return Task.CompletedTask;
        }
    }
}
