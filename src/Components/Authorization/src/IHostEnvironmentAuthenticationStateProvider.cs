// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// An interface implemented by <see cref="AuthenticationStateProvider"/> classes that can receive authentication
/// state information from the host environment.
/// </summary>
public interface IHostEnvironmentAuthenticationStateProvider
{
    /// <summary>
    /// Supplies updated authentication state data to the <see cref="AuthenticationStateProvider"/>.
    /// </summary>
    /// <param name="authenticationStateTask">A task that resolves with the updated <see cref="AuthenticationState"/>.</param>
    void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask);
}
