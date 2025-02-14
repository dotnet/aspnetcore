// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// An interface implemented by services to receive authentication state information from the host environment.
/// If this is implemented by the host's <see cref="AuthenticationStateProvider"/>, it will receive authentication state from the HttpContext.
/// Or if this implemented service that is registered directly as an <see cref="IHostEnvironmentAuthenticationStateProvider"/>,
/// it will receive the <see cref="AuthenticationState"/> returned by <see cref="AuthenticationStateProvider.GetAuthenticationStateAsync"/> 
/// </summary>
public interface IHostEnvironmentAuthenticationStateProvider
{
    /// <summary>
    /// Supplies updated authentication state data to the <see cref="AuthenticationStateProvider"/>.
    /// </summary>
    /// <param name="authenticationStateTask">A task that resolves with the updated <see cref="AuthenticationState"/>.</param>
    void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask);
}
