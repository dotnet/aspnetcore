// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class RevalidatingServerAuthenticationStateProviderTest
    {
        [Fact]
        public void RejectsZeroRevalidationInterval()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new TestRevalidatingServerAuthenticationStateProvider(TimeSpan.Zero));
            Assert.Equal("revalidationInterval", ex.ParamName);
            Assert.StartsWith("The interval must be a nonzero value", ex.Message);
        }

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
                authState => Assert.Equal("test user", authState.User.Identity.Name));

            // Act/Assert 1: Can become signed out
            // Doesn't revalidate unauthenticated states
            provider.SetAuthenticationState(CreateAuthenticationStateTask(null));
            await Task.Delay(200);
            Assert.Empty(provider.RevalidationCallLog.Skip(1));

            // Act/Assert 2: Can become a different user; resumes revalidation
            provider.SetAuthenticationState(CreateAuthenticationStateTask("different user"));
            await provider.NextValidateAuthenticationStateAsyncCall;
            Assert.Collection(provider.RevalidationCallLog.Skip(1),
                authState => Assert.Equal("different user", authState.User.Identity.Name));
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
            private TaskCompletionSource<object> _nextValidateAuthenticationStateAsyncCallSource
                = new TaskCompletionSource<object>();

            public TestRevalidatingServerAuthenticationStateProvider(TimeSpan revalidationInterval)
                : base(NullLoggerFactory.Instance, revalidationInterval)
            {
            }

            public Task<bool> NextValidationResult { get; set; }

            public Task NextValidateAuthenticationStateAsyncCall
                => _nextValidateAuthenticationStateAsyncCallSource.Task;

            public List<AuthenticationState> RevalidationCallLog { get; }
                = new List<AuthenticationState>();

            protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState)
            {
                RevalidationCallLog.Add(authenticationState);
                var result = NextValidationResult;
                var prevCts = _nextValidateAuthenticationStateAsyncCallSource;
                _nextValidateAuthenticationStateAsyncCallSource = new TaskCompletionSource<object>();
                prevCts.SetResult(true);
                return result;
            }
        }
    }
}
