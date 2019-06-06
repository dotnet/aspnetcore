// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// An <see cref="AuthenticationStateProvider"/> intended for use in server-side
    /// Blazor. The circuit factory will supply a <see cref="ClaimsPrincipal"/> from
    /// the current <see cref="HttpContext.User"/>, which will stay fixed for the
    /// lifetime of the circuit since <see cref="HttpContext.User"/> cannot change.
    ///
    /// This can therefore only be used with redirect-style authentication flows,
    /// since it requires a new HTTP request in order to become a different user.
    /// </summary>
    internal class FixedAuthenticationStateProvider : AuthenticationStateProvider
    {
        private Task<AuthenticationState> _authenticationStateTask;

        public void Initialize(ClaimsPrincipal user)
        {
            _authenticationStateTask = Task.FromResult(new AuthenticationState(user));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => _authenticationStateTask
            ?? throw new InvalidOperationException($"{nameof(GetAuthenticationStateAsync)} was called before {nameof(Initialize)}.");
    }
}
