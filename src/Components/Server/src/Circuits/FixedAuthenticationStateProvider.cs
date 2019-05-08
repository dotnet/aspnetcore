// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class FixedAuthenticationStateProvider : IAuthenticationStateProvider
    {
        private Task<IAuthenticationState> _authenticationStateTask;

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
