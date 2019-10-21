// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
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
        private CancellationTokenSource _loopCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Constructs an instance of <see cref="RevalidatingServerAuthenticationStateProvider"/>.
        /// </summary>
        /// <param name="loggerFactory">A logger factory.</param>
        public RevalidatingServerAuthenticationStateProvider(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<RevalidatingServerAuthenticationStateProvider>();

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
        /// Gets the interval between revalidation attempts.
        /// </summary>
        protected abstract TimeSpan RevalidationInterval { get; }

        /// <summary>
        /// Determines whether the authentication state is still valid.
        /// </summary>
        /// <param name="authenticationState">The current <see cref="AuthenticationState"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
        /// <returns>A <see cref="Task"/> that resolves as true if the <paramref name="authenticationState"/> is still valid, or false if it is not.</returns>
        protected abstract Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken);

        private async Task RevalidationLoop(Task<AuthenticationState> authenticationStateTask, CancellationToken cancellationToken)
        {
            try
            {
                var authenticationState = await authenticationStateTask;
                if (authenticationState.User.Identity.IsAuthenticated)
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        bool isValid;

                        try
                        {
                            await Task.Delay(RevalidationInterval, cancellationToken);
                            isValid = await ValidateAuthenticationStateAsync(authenticationState, cancellationToken);
                        }
                        catch (TaskCanceledException tce)
                        {
                            // If it was our cancellation token, then this revalidation loop gracefully completes
                            // Otherwise, treat it like any other failure
                            if (tce.CancellationToken == cancellationToken)
                            {
                                break;
                            }

                            throw;
                        }
                        
                        if (!isValid)
                        {
                            ForceSignOut();
                            break;
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
