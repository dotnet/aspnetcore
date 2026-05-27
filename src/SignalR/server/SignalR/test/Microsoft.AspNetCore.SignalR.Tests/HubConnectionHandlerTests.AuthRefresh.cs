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

                // Start a hub method that holds the per-connection invocation semaphore.
                var invokeTask = client.InvokeAsync(nameof(AuthRefreshHub.BlockUntilSignaled));
                await hubObserver.HubMethodStarted.Task.DefaultTimeout();

                // Refresh while the hub method is still in flight.
                var previousUser = client.Connection.User;
                Assert.NotNull(previousUser);
                var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "after") }, "Test"));
                client.Connection.User = refreshedUser;
                feature.Raise(previousUser, refreshedUser);

                // OnAuthRefreshedAsync must wait for the semaphore — the hub method still holds it.
                Assert.False(hubObserver.PreviousUserTask.IsCompleted);
                await Task.Delay(50);
                Assert.False(hubObserver.PreviousUserTask.IsCompleted);

                // Release the hub method; OnAuthRefreshedAsync should now run.
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

        public Task<ClaimsPrincipal?> PreviousUserTask => _tcs.Task;

        public TaskCompletionSource HubMethodStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseHubMethod { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Capture(ClaimsPrincipal? previousUser) => _tcs.TrySetResult(previousUser);
    }

    private sealed class AuthRefreshHub : Hub
    {
        private readonly AuthRefreshObserver _observer;

        public AuthRefreshHub(AuthRefreshObserver observer)
        {
            _observer = observer;
        }

        public string? GetUserName() => Context.User?.Identity?.Name;

        public async Task BlockUntilSignaled()
        {
            _observer.HubMethodStarted.TrySetResult();
            await _observer.ReleaseHubMethod.Task;
        }

        public override Task OnAuthRefreshedAsync(ClaimsPrincipal? previousUser)
        {
            _observer.Capture(previousUser);
            return Task.CompletedTask;
        }
    }
}
