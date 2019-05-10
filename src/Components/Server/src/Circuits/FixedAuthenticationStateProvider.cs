// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// An <see cref="IAuthenticationStateProvider"/> intended for use in server-side
    /// Blazor. The circuit factory will supply a <see cref="ClaimsPrincipal"/> from
    /// the current <see cref="HttpContext.User"/>, which will stay fixed for the
    /// lifetime of the circuit since <see cref="HttpContext.User"/> cannot change.
    ///
    /// This can therefore only be used with redirect-style authentication flows,
    /// since it requires a new HTTP request in order to become a different user.
    /// </summary>
    internal class FixedAuthenticationStateProvider : IAuthenticationStateProvider
    {
        private Task<IAuthenticationState> _authenticationStateTask;

        // Since the authentication state is fixed, we never raise this event
        #pragma warning disable 0067 // "Never used"
        public event AuthenticationStateChangedHandler AuthenticationStateChanged;
        #pragma warning restore 0067

        public void Initialize(ClaimsPrincipal user)
        {
            var authState = new FixedAuthenticationState(user);
            _authenticationStateTask = Task.FromResult((IAuthenticationState)authState);
        }

        public Task<IAuthenticationState> GetAuthenticationStateAsync(bool forceRefresh)
        {
            // Since the authentication state is fixed, forceRefresh makes no difference
            return _authenticationStateTask
                ?? throw new InvalidOperationException($"{nameof(GetAuthenticationStateAsync)} was called before {nameof(Initialize)}.");
        }

        private class FixedAuthenticationState : IAuthenticationState
        {
            public FixedAuthenticationState(ClaimsPrincipal user)
            {
                User = user ?? throw new ArgumentNullException(nameof(user));
            }

            public ClaimsPrincipal User { get; }
        }
    }
}
