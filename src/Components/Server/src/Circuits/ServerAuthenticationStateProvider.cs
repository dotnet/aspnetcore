// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// An <see cref="AuthenticationStateProvider"/> intended for use in server-side Blazor.
    /// </summary>
    internal class ServerAuthenticationStateProvider : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
    {
        private Task<AuthenticationState> _authenticationStateTask;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => _authenticationStateTask
            ?? throw new InvalidOperationException($"{nameof(GetAuthenticationStateAsync)} was called before {nameof(SetAuthenticationState)}.");

        public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
        {
            _authenticationStateTask = authenticationStateTask ?? throw new ArgumentNullException(nameof(authenticationStateTask));
            NotifyAuthenticationStateChanged(_authenticationStateTask);
        }
    }
}
