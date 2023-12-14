// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components;

public class RevalidatingServerAuthenticationStateProviderTest
{
    [Fact]
    public void AcceptsAndReturnsAuthStateFromHost()
    {
        // Arrange
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(TimeSpan.MaxValue);

        // Act/Assert: Host can supply a value
        var hostAuthStateTask = (new TaskCompletionSource<AuthenticationState>()).Task;
        provider.SetAuthenticationState(hostAuthStateTask);
        Assert.Same(hostAuthStateTask, provider.GetAuthenticationStateAsync());

        // Act/Assert: Host can supply a changed value
        var hostAuthStateTask2 = (new TaskCompletionSource<AuthenticationState>()).Task;
        provider.SetAuthenticationState(hostAuthStateTask2);
        Assert.Same(hostAuthStateTask2, provider.GetAuthenticationStateAsync());
    }

    [Fact]
    public async Task IfValidateAuthenticationStateAsyncReturnsTrue_ContinuesRevalidating()
    {
        // Arrange
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(
            TimeSpan.FromMilliseconds(50));
        provider.SetAuthenticationState(CreateAuthenticationStateTask("test user"));
        provider.NextValidationResult = Task.FromResult(true);
        var didNotifyAuthenticationStateChanged = false;
        provider.AuthenticationStateChanged += _ => { didNotifyAuthenticationStateChanged = true; };

        // Act
        for (var i = 0; i < 10; i++)
        {
            await provider.NextValidateAuthenticationStateAsyncCall;
        }

        // Assert
        Assert.Equal(10, provider.RevalidationCallLog.Count);
        Assert.False(didNotifyAuthenticationStateChanged);
        Assert.Equal("test user", (await provider.GetAuthenticationStateAsync()).User.Identity.Name);
    }

    [Fact]
    public async Task IfValidateAuthenticationStateAsyncReturnsFalse_ForcesSignOut()
    {
        // Arrange
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(
            TimeSpan.FromMilliseconds(50));
        provider.SetAuthenticationState(CreateAuthenticationStateTask("test user"));
        provider.NextValidationResult = Task.FromResult(false);

        var newAuthStateNotificationTcs = new TaskCompletionSource<Task<AuthenticationState>>();
        provider.AuthenticationStateChanged += newStateTask => newAuthStateNotificationTcs.SetResult(newStateTask);

        // Act
        var newAuthStateTask = await newAuthStateNotificationTcs.Task;
        var newAuthState = await newAuthStateTask;

        // Assert
        Assert.False(newAuthState.User.Identity.IsAuthenticated);

        // Assert: no longer revalidates
        await Task.Delay(200);
        Assert.Single(provider.RevalidationCallLog);
    }

    [Fact]
    public async Task IfValidateAuthenticationStateAsyncThrows_ForcesSignOut()
    {
        // Arrange
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(
            TimeSpan.FromMilliseconds(50));
        provider.SetAuthenticationState(CreateAuthenticationStateTask("test user"));
        provider.NextValidationResult = Task.FromException<bool>(new InvalidTimeZoneException());

        var newAuthStateNotificationTcs = new TaskCompletionSource<Task<AuthenticationState>>();
        provider.AuthenticationStateChanged += newStateTask => newAuthStateNotificationTcs.SetResult(newStateTask);

        // Act
        var newAuthStateTask = await newAuthStateNotificationTcs.Task;
        var newAuthState = await newAuthStateTask;

        // Assert
        Assert.False(newAuthState.User.Identity.IsAuthenticated);

        // Assert: no longer revalidates
        await Task.Delay(200);
        Assert.Single(provider.RevalidationCallLog);
    }

    [Fact]
    public async Task IfHostSuppliesNewAuthenticationState_RestartsRevalidationLoop()
    {
        // Arrange
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(
            TimeSpan.FromMilliseconds(50));
        provider.SetAuthenticationState(CreateAuthenticationStateTask("test user"));
        provider.NextValidationResult = Task.FromResult(true);
        await provider.NextValidateAuthenticationStateAsyncCall;
        Assert.Collection(provider.RevalidationCallLog,
            call => Assert.Equal("test user", call.AuthenticationState.User.Identity.Name));

        // Act/Assert 1: Can become signed out
        // Doesn't revalidate unauthenticated states
        provider.SetAuthenticationState(CreateAuthenticationStateTask(null));
        await Task.Delay(200);
        Assert.Empty(provider.RevalidationCallLog.Skip(1));

        // Act/Assert 2: Can become a different user; resumes revalidation
        provider.SetAuthenticationState(CreateAuthenticationStateTask("different user"));
        await provider.NextValidateAuthenticationStateAsyncCall;
        Assert.Collection(provider.RevalidationCallLog.Skip(1),
            call => Assert.Equal("different user", call.AuthenticationState.User.Identity.Name));
    }

    [Fact]
    public async Task StopsRevalidatingAfterDisposal()
    {
        // Arrange
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(
            TimeSpan.FromMilliseconds(50));
        provider.SetAuthenticationState(CreateAuthenticationStateTask("test user"));
        provider.NextValidationResult = Task.FromResult(true);

        // Act
        ((IDisposable)provider).Dispose();
        await Task.Delay(200);

        // Assert
        Assert.Empty(provider.RevalidationCallLog);
    }

    [Fact]
    public async Task SuppliesCancellationTokenThatSignalsWhenRevalidationLoopIsBeingDiscarded()
    {
        // Arrange
        var validationTcs = new TaskCompletionSource<bool>();
        var authenticationStateChangedCount = 0;
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(
            TimeSpan.FromMilliseconds(50));
        provider.NextValidationResult = validationTcs.Task;
        provider.SetAuthenticationState(CreateAuthenticationStateTask("test user"));
        provider.AuthenticationStateChanged += _ => { authenticationStateChangedCount++; };

        // Act/Assert 1: token isn't cancelled initially
        await provider.NextValidateAuthenticationStateAsyncCall;
        var firstRevalidationCall = provider.RevalidationCallLog.Single();
        Assert.False(firstRevalidationCall.CancellationToken.IsCancellationRequested);
        Assert.Equal(0, authenticationStateChangedCount);

        // Have the task throw a TCE to show this doesn't get treated as a failure
        firstRevalidationCall.CancellationToken.Register(() => validationTcs.TrySetCanceled(firstRevalidationCall.CancellationToken));

        // Act/Assert 2: token is cancelled when the loop is superseded
        provider.NextValidationResult = Task.FromResult(true);
        provider.SetAuthenticationState(CreateAuthenticationStateTask("different user"));
        Assert.True(firstRevalidationCall.CancellationToken.IsCancellationRequested);

        // Since we asked for that operation to be cancelled, we don't treat it as a failure and
        // don't force a logout
        Assert.Equal(1, authenticationStateChangedCount);
        Assert.Equal("different user", (await provider.GetAuthenticationStateAsync()).User.Identity.Name);

        // Subsequent revalidation can complete successfully
        await provider.NextValidateAuthenticationStateAsyncCall;
        Assert.Collection(provider.RevalidationCallLog.Skip(1),
             call => Assert.Equal("different user", call.AuthenticationState.User.Identity.Name));
    }

    [Fact]
    public async Task IfValidateAuthenticationStateAsyncReturnsUnrelatedCancelledTask_TreatAsFailure()
    {
        // Arrange
        var validationTcs = new TaskCompletionSource<bool>();
        var incrementExecuted = new TaskCompletionSource();
        var authenticationStateChangedCount = 0;
        using var provider = new TestRevalidatingServerAuthenticationStateProvider(
            TimeSpan.FromMilliseconds(50));
        provider.NextValidationResult = validationTcs.Task;
        provider.SetAuthenticationState(CreateAuthenticationStateTask("test user"));
        provider.AuthenticationStateChanged += _ =>
        {
            authenticationStateChangedCount++;
            incrementExecuted.TrySetResult();
        };

        // Be waiting for the first ValidateAuthenticationStateAsync to complete
        await provider.NextValidateAuthenticationStateAsyncCall;
        var firstRevalidationCall = provider.RevalidationCallLog.Single();
        Assert.Equal(0, authenticationStateChangedCount);

        // Act: ValidateAuthenticationStateAsync returns canceled task, but the cancellation
        // is unrelated to the CT we supplied
        validationTcs.TrySetCanceled(new CancellationTokenSource().Token);

        // Assert: Since we didn't ask for that operation to be canceled, this is treated as
        // a failure to validate, so we force a logout
        await incrementExecuted.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
        Assert.Equal(1, authenticationStateChangedCount);
        var newAuthState = await provider.GetAuthenticationStateAsync();
        Assert.False(newAuthState.User.Identity.IsAuthenticated);
        Assert.Null(newAuthState.User.Identity.Name);
    }

    static Task<AuthenticationState> CreateAuthenticationStateTask(string username)
    {
        var identity = !string.IsNullOrEmpty(username)
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "testauth")
            : new ClaimsIdentity();
        var authenticationState = new AuthenticationState(new ClaimsPrincipal(identity));
        return Task.FromResult(authenticationState);
    }

    class TestRevalidatingServerAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
    {
        private readonly TimeSpan _revalidationInterval;
        private TaskCompletionSource _nextValidateAuthenticationStateAsyncCallSource
            = new TaskCompletionSource();

        public TestRevalidatingServerAuthenticationStateProvider(TimeSpan revalidationInterval)
            : base(NullLoggerFactory.Instance)
        {
            _revalidationInterval = revalidationInterval;
        }

        public Task<bool> NextValidationResult { get; set; }

        public Task NextValidateAuthenticationStateAsyncCall
            => _nextValidateAuthenticationStateAsyncCallSource.Task;

        public List<(AuthenticationState AuthenticationState, CancellationToken CancellationToken)> RevalidationCallLog { get; }
            = new List<(AuthenticationState, CancellationToken)>();

        protected override TimeSpan RevalidationInterval => _revalidationInterval;

        protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            RevalidationCallLog.Add((authenticationState, cancellationToken));
            var result = NextValidationResult;
            var prevCts = _nextValidateAuthenticationStateAsyncCallSource;
            _nextValidateAuthenticationStateAsyncCallSource = new TaskCompletionSource();
            prevCts.SetResult();
            return result;
        }
    }
}
