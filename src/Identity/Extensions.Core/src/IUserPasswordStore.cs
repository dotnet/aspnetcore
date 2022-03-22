// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for a store containing users' password hashes.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public interface IUserPasswordStore<TUser> : IUserStore<TUser> where TUser : class
{
    /// <summary>
    /// Sets the password hash for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user whose password hash to set.</param>
    /// <param name="passwordHash">The password hash to set.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the password hash for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user whose password hash to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the password hash for the specified <paramref name="user"/>.</returns>
    Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a flag indicating whether the specified <paramref name="user"/> has a password.
    /// </summary>
    /// <param name="user">The user to return a flag for, indicating whether they have a password or not.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="Task"/> that represents the asynchronous operation, returning true if the specified <paramref name="user"/> has a password
    /// otherwise false.
    /// </returns>
    Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken);
}
