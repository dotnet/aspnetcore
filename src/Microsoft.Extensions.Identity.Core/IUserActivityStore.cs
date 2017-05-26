// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for a store containing users' password expiration data.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public interface IUserActivityStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        /// Sets the last password change date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="changeDate">The last password change date.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task SetLastPasswordChangeDateAsync(TUser user, DateTimeOffset? changeDate, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the last password change date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the password hash for the specified <paramref name="user"/>.</returns>
        Task<DateTimeOffset?> GetLastPasswordChangeDateAsync(TUser user, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the creation date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="creationDate">The date the user was created.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task SetCreateDateAsync(TUser user, DateTimeOffset? creationDate, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the creation date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the password hash for the specified <paramref name="user"/>.</returns>
        Task<DateTimeOffset?> GetCreateDateAsync(TUser user, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the last signin date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lastSignIn">The date the user last signed in.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task SetLastSignInDateAsync(TUser user, DateTimeOffset? lastSignIn, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the last signin date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the password hash for the specified <paramref name="user"/>.</returns>
        Task<DateTimeOffset?> GetLastSignInDateAsync(TUser user, CancellationToken cancellationToken);
    }
}