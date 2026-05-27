// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Security.Claims;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Fact]
    public async Task UserPropertyReflectsLatestPrincipalFromConnectionUserFeature()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthRefreshHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var firstNameClaim = await client.InvokeAsync(nameof(AuthRefreshHub.GetUserName)).DefaultTimeout();
                Assert.Null(firstNameClaim.Error);
                var originalName = (string?)firstNameClaim.Result;
                Assert.False(string.IsNullOrEmpty(originalName));

                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "refreshed-user"),
                }, "Test"));
                client.Connection.User = refreshedUser;

                var secondNameClaim = await client.InvokeAsync(nameof(AuthRefreshHub.GetUserName)).DefaultTimeout();
                Assert.Null(secondNameClaim.Error);
                Assert.Equal("refreshed-user", secondNameClaim.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task UserUpdatedFeatureEventDispatchesOnAuthRefreshedAsyncToHub()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserUpdateFeature();
                client.Connection.Features.Set<IConnectionUserUpdateFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, previousUser.Identity!.Name!),
                    new Claim("scope", "extra"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(previousUser, refreshedUser);

                var captured = await hubObserver.PreviousUserTask.DefaultTimeout();
                Assert.Same(previousUser, captured);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task OnAuthRefreshedAsyncSerializesWithInFlightHubInvocation()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserUpdateFeature();
                client.Connection.Features.Set<IConnectionUserUpdateFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var invokeTask = client.InvokeAsync(nameof(AuthRefreshHub.BlockUntilSignaled));
                await hubObserver.HubMethodStarted.Task.DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "after") }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(previousUser, refreshedUser);

                Assert.False(hubObserver.PreviousUserTask.IsCompleted);
                await Task.Delay(50);
                Assert.False(hubObserver.PreviousUserTask.IsCompleted);

                hubObserver.ReleaseHubMethod.SetResult();
                var completion = await invokeTask.DefaultTimeout();
                Assert.Null(completion.Error);

                var captured = await hubObserver.PreviousUserTask.DefaultTimeout();
                Assert.Same(previousUser, captured);

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
            var hubObserver = new AuthRefreshObserver();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthRefreshHub>>();

            using (var client = new TestClient(userIdentifier: "stable-user"))
            {
                var feature = new TestConnectionUserUpdateFeature();
                client.Connection.Features.Set<IConnectionUserUpdateFeature>(feature);

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
                feature.Raise(previousUser, refreshedUser);

                var captured = await hubObserver.PreviousUserTask.DefaultTimeout();
                Assert.Same(previousUser, captured);

                // Connection still alive — hub method should still work.
                var seenUser = await client.InvokeAsync(nameof(AuthRefreshHub.GetUserClaim), "role").DefaultTimeout();
                Assert.Null(seenUser.Error);
                Assert.Equal("admin", seenUser.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task ExceptionFromOnAuthRefreshedAsyncIsLoggedAndConnectionSurvives()
    {
        using (StartVerifiableLog(write => write.EventId.Name == "FailedInvokingHubMethod"))
        {
            var hubObserver = new AuthRefreshObserver { ThrowFromOnAuthRefreshed = true };
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserUpdateFeature();
                client.Connection.Features.Set<IConnectionUserUpdateFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "after"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(previousUser, refreshedUser);

                // Wait until the hub method ran (and threw).
                await hubObserver.PreviousUserTask.DefaultTimeout();

                // Semaphore must have been released — a subsequent invocation should complete.
                var nameResult = await client.InvokeAsync(nameof(AuthRefreshHub.GetUserName)).DefaultTimeout();
                Assert.Null(nameResult.Error);
                Assert.Equal("after", nameResult.Result);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task MultipleRefreshesEachDispatchWithCorrectPreviousUser()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthRefreshObserver { CaptureAll = true };
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserUpdateFeature();
                client.Connection.Features.Set<IConnectionUserUpdateFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var user0 = client.Connection.User;
                Assert.NotNull(user0);
                var user1 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "u1") }, "Test"));
                var user2 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "u2") }, "Test"));
                var user3 = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "u3") }, "Test"));

                client.Connection.User = user1;
                feature.Raise(user0, user1);
                client.Connection.User = user2;
                feature.Raise(user1, user2);
                client.Connection.User = user3;
                feature.Raise(user2, user3);

                await hubObserver.AllRefreshesCompleted(3).DefaultTimeout();
                var previousUsers = hubObserver.AllPreviousUsers;
                Assert.Equal(3, previousUsers.Count);
                Assert.Same(user0, previousUsers[0]);
                Assert.Same(user1, previousUsers[1]);
                Assert.Same(user2, previousUsers[2]);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task OnAuthRefreshedAsyncSeesNewUserInContextUser()
    {
        using (StartVerifiableLog())
        {
            var hubObserver = new AuthRefreshObserver { CaptureContextUser = true };
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(
                services => services.AddSingleton(hubObserver), LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<AuthRefreshHub>>();

            using (var client = new TestClient())
            {
                var feature = new TestConnectionUserUpdateFeature();
                client.Connection.Features.Set<IConnectionUserUpdateFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "fresh-name"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(previousUser, refreshedUser);

                await hubObserver.PreviousUserTask.DefaultTimeout();
                Assert.Equal("fresh-name", hubObserver.CapturedContextUserName);

                client.Dispose();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task ConnectionWithoutUserUpdateFeatureStillWorks()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(_ => { }, LoggerFactory);
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                Assert.Null(client.Connection.Features.Get<IConnectionUserUpdateFeature>());

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
                var feature = new TestConnectionUserUpdateFeature();
                client.Connection.Features.Set<IConnectionUserUpdateFeature>(feature);

                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var previousUser = client.Connection.User;
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-2"),
                }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(previousUser, refreshedUser);

                // Connection should be aborted by the handler; the dispatch loop completes without the client disposing.
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    private sealed class TestConnectionUserUpdateFeature : IConnectionUserUpdateFeature
    {
        public event Action<ClaimsPrincipal?, ClaimsPrincipal>? UserUpdated;

        public void Raise(ClaimsPrincipal? previous, ClaimsPrincipal current)
        {
            UserUpdated?.Invoke(previous, current);
        }
    }

    private sealed class AuthRefreshObserver
    {
        private readonly TaskCompletionSource<ClaimsPrincipal?> _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<ClaimsPrincipal?> _allPrevious = new();
        private readonly object _allLock = new();
        private TaskCompletionSource? _allTcs;
        private int _waitedCount;

        public Task<ClaimsPrincipal?> PreviousUserTask => _tcs.Task;

        public TaskCompletionSource HubMethodStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseHubMethod { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ThrowFromOnAuthRefreshed { get; set; }

        public bool CaptureAll { get; set; }

        public bool CaptureContextUser { get; set; }

        public string? CapturedContextUserName { get; private set; }

        public IReadOnlyList<ClaimsPrincipal?> AllPreviousUsers
        {
            get { lock (_allLock) { return _allPrevious.ToArray(); } }
        }

        public Task AllRefreshesCompleted(int count)
        {
            lock (_allLock)
            {
                _waitedCount = count;
                _allTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                if (_allPrevious.Count >= count)
                {
                    _allTcs.TrySetResult();
                }
                return _allTcs.Task;
            }
        }

        public void Capture(ClaimsPrincipal? previousUser, string? contextUserName)
        {
            if (CaptureContextUser)
            {
                CapturedContextUserName = contextUserName;
            }

            if (CaptureAll)
            {
                lock (_allLock)
                {
                    _allPrevious.Add(previousUser);
                    if (_allTcs is not null && _allPrevious.Count >= _waitedCount)
                    {
                        _allTcs.TrySetResult();
                    }
                }
            }

            _tcs.TrySetResult(previousUser);
        }
    }

    private sealed class AuthRefreshHub : Hub
    {
        private readonly AuthRefreshObserver _observer;

        public AuthRefreshHub(AuthRefreshObserver observer)
        {
            _observer = observer;
        }

        public string? GetUserName() => Context.User?.Identity?.Name;

        public string? GetUserClaim(string type) => Context.User?.FindFirst(type)?.Value;

        public async Task BlockUntilSignaled()
        {
            _observer.HubMethodStarted.TrySetResult();
            await _observer.ReleaseHubMethod.Task;
        }

        public override Task OnAuthRefreshedAsync(ClaimsPrincipal? previousUser)
        {
            _observer.Capture(previousUser, Context.User?.Identity?.Name);
            if (_observer.ThrowFromOnAuthRefreshed)
            {
                throw new InvalidOperationException("boom from OnAuthRefreshedAsync");
            }
            return Task.CompletedTask;
        }
    }
}
