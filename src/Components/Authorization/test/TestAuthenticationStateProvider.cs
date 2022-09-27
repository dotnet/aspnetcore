// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Authorization;

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
