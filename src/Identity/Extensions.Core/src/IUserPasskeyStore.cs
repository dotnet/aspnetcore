// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for storing passkey credentials for a user.
/// </summary>
/// <typeparam name="TUser">The type that represents a user.</typeparam>
public interface IUserPasskeyStore<TUser> : IUserStore<TUser> where TUser : class
{
    /// <summary>
    /// Adds a new passkey credential in the store for the specified <paramref name="user"/>,
    /// or updates an existing passkey.
    /// </summary>
    /// <param name="user">The user to create the passkey credential for.</param>
    /// <param name="passkey">The passkey to add.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task SetPasskeyAsync(TUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the passkey credentials for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user whose passkeys should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing a list of the user's passkeys.</returns>
    Task<IList<UserPasskeyInfo>> GetPasskeysAsync(TUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Finds and returns a user, if any, associated with the specified passkey credential identifier.
    /// </summary>
    /// <param name="credentialId">The passkey credential id to search for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, containing the user, if any, associated with the specified passkey credential id.
    /// </returns>
    Task<TUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds a passkey for the specified user with the specified credential id.
    /// </summary>
    /// <param name="user">The user whose passkey should be retrieved.</param>
    /// <param name="credentialId">The credential id to search for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user's passkey information.</returns>
    Task<UserPasskeyInfo?> FindPasskeyAsync(TUser user, byte[] credentialId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a passkey credential from the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to remove the passkey credential from.</param>
    /// <param name="credentialId">The credential id of the passkey to remove.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task RemovePasskeyAsync(TUser user, byte[] credentialId, CancellationToken cancellationToken);
}
