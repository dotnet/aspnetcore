// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// A base class for <see cref="AuthenticationStateProvider"/> services that receive an
    /// authentication state from the host environment, and revalidate it at regular intervals.
    /// </summary>
    public abstract class RevalidatingServerAuthenticationStateProvider
        : ServerAuthenticationStateProvider, IDisposable
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _revalidationInterval;
        private CancellationTokenSource _loopCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Constructs an instance of <see cref="RevalidatingServerAuthenticationStateProvider"/>.
        /// </summary>
        /// <param name="loggerFactory">A logger factory.</param>
        /// <param name="revalidationInterval">The interval between revalidation attempts.</param>
        public RevalidatingServerAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            TimeSpan revalidationInterval)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (revalidationInterval == default)
            {
                throw new ArgumentException("The interval must be a nonzero value", nameof(revalidationInterval));
            }

            _logger = loggerFactory.CreateLogger<RevalidatingServerAuthenticationStateProvider>();
            _revalidationInterval = revalidationInterval;

            // Whenever we receive notification of a new authentication state, cancel any
            // existing revalidation loop and start a new one
            AuthenticationStateChanged += authenticationStateTask =>
            {
                _loopCancellationTokenSource?.Cancel();
                _loopCancellationTokenSource = new CancellationTokenSource();
                _ = RevalidationLoop(authenticationStateTask, _loopCancellationTokenSource.Token);
            };
        }

        /// <summary>
        /// Determines whether the authentication state is still valid.
        /// </summary>
        /// <param name="authenticationState">The current <see cref="AuthenticationState"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves as true if the <paramref name="authenticationState"/> is still valid, or false if it is not.</returns>
        protected abstract Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState);

        private async Task RevalidationLoop(Task<AuthenticationState> authenticationStateTask, CancellationToken cancellationToken)
        {
            try
            {
                var authenticationState = await authenticationStateTask;
                if (authenticationState.User.Identity.IsAuthenticated)
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(_revalidationInterval, cancellationToken);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }

                        var isValid = await ValidateAuthenticationStateAsync(authenticationState);
                        if (!isValid)
                        {
                            ForceSignOut();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while revalidating authentication state");
                ForceSignOut();
            }
        }

        private void ForceSignOut()
        {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var anonymousState = new AuthenticationState(anonymousUser);
            SetAuthenticationState(Task.FromResult(anonymousState));
        }

        void IDisposable.Dispose()
        {
            _loopCancellationTokenSource?.Cancel();
            Dispose(disposing: true);
        }

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
