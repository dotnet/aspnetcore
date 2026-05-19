// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// An <see cref="AuthenticationStateProvider"/> intended for use in server-side Blazor.
/// </summary>
public class ServerAuthenticationStateProvider : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
{
    private Task<AuthenticationState>? _authenticationStateTask;

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => _authenticationStateTask
        ?? throw new InvalidOperationException($"Do not call {nameof(GetAuthenticationStateAsync)} outside of the DI scope for a Razor component. Typically, this means you can call it only within a Razor component or inside another DI service that is resolved for a Razor component.");

    /// <inheritdoc />
    public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        _authenticationStateTask = authenticationStateTask ?? throw new ArgumentNullException(nameof(authenticationStateTask));
        NotifyAuthenticationStateChanged(_authenticationStateTask);
    }
}
