// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// An <see cref="AuthenticationStateProvider"/> intended for use in server-side Blazor.
    /// </summary>
    public class ServerAuthenticationStateProvider : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
    {
        private Task<AuthenticationState> _authenticationStateTask;

        /// <inheritdoc />
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => _authenticationStateTask
            ?? throw new InvalidOperationException($"{nameof(GetAuthenticationStateAsync)} was called before {nameof(SetAuthenticationState)}.");

        /// <inheritdoc />
        public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
        {
            _authenticationStateTask = authenticationStateTask ?? throw new ArgumentNullException(nameof(authenticationStateTask));
            NotifyAuthenticationStateChanged(_authenticationStateTask);
        }
    }
}
