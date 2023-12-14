// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// Provides information about the authentication state of the current user.
/// </summary>
public abstract class AuthenticationStateProvider
{
    /// <summary>
    /// Asynchronously gets an <see cref="AuthenticationState"/> that describes the current user.
    /// </summary>
    /// <returns>A task that, when resolved, gives an <see cref="AuthenticationState"/> instance that describes the current user.</returns>
    public abstract Task<AuthenticationState> GetAuthenticationStateAsync();

    /// <summary>
    /// An event that provides notification when the <see cref="AuthenticationState"/>
    /// has changed. For example, this event may be raised if a user logs in or out.
    /// </summary>
    public event AuthenticationStateChangedHandler? AuthenticationStateChanged;

    /// <summary>
    /// Raises the <see cref="AuthenticationStateChanged"/> event.
    /// </summary>
    /// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="AuthenticationState"/>.</param>
    protected void NotifyAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        AuthenticationStateChanged?.Invoke(task);
    }
}

/// <summary>
/// A handler for the <see cref="AuthenticationStateProvider.AuthenticationStateChanged"/> event.
/// </summary>
/// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="AuthenticationState"/>.</param>
public delegate void AuthenticationStateChangedHandler(Task<AuthenticationState> task);
