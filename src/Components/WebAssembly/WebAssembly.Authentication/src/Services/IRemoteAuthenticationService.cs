// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents a contract for services that perform authentication operations for a Blazor WebAssembly application.
/// </summary>
/// <typeparam name="TRemoteAuthenticationState">The state to be persisted across authentication operations.</typeparam>
public interface IRemoteAuthenticationService<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>
    where TRemoteAuthenticationState : RemoteAuthenticationState
{
    /// <summary>
    /// Signs in a user.
    /// </summary>
    /// <param name="context">The <see cref="RemoteAuthenticationContext{TRemoteAuthenticationState}"/> for authenticating the user.</param>
    /// <returns>The result of the authentication operation.</returns>
    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context);

    /// <summary>
    /// Completes the sign in operation for a user when it is performed outside of the application origin via a redirect operation followed
    /// by a redirect callback to a page in the application.
    /// </summary>
    /// <param name="context">The <see cref="RemoteAuthenticationContext{TRemoteAuthenticationState}"/> for authenticating the user.</param>
    /// <returns>The result of the authentication operation.</returns>
    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context);

    /// <summary>
    /// Signs out a user.
    /// </summary>
    /// <param name="context">The <see cref="RemoteAuthenticationContext{TRemoteAuthenticationState}"/> for authenticating the user.</param>
    /// <returns>The result of the authentication operation.</returns>
    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context);

    /// <summary>
    /// Completes the sign out operation for a user when it is performed outside of the application origin via a redirect operation followed
    /// by a redirect callback to a page in the application.
    /// </summary>
    /// <param name="context">The <see cref="RemoteAuthenticationContext{TRemoteAuthenticationState}"/> for authenticating the user.</param>
    /// <returns>The result of the authentication operation.</returns>
    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context);
}
