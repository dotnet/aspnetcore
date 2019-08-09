// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    public class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        public Task<AuthenticationState> CurrentAuthStateTask { get; set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return CurrentAuthStateTask;
        }

        internal void TriggerAuthenticationStateChanged(Task<AuthenticationState> authState)
        {
            NotifyAuthenticationStateChanged(authState);
        }
    }
}
