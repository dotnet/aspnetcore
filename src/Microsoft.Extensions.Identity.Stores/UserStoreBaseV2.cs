// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    /// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
    /// <typeparam name="TUserRole">The type representing a user role.</typeparam>
    /// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
    /// <typeparam name="TUserToken">The type representing a user token.</typeparam>
    /// <typeparam name="TRoleClaim">The type representing a role claim.</typeparam>
    public abstract class UserStoreBaseV2<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> :
        UserStoreBaseV1<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>,
        IUserActivityStore<TUser>
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserToken : IdentityUserToken<TKey>, new()
        where TRoleClaim : IdentityRoleClaim<TKey>, new()
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
        public UserStoreBaseV2(IdentityErrorDescriber describer) : base(describer) { }

        /// <summary>
        /// Sets the last password change date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="changeDate">The last password change date.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetLastPasswordChangeDateAsync(TUser user, DateTimeOffset? changeDate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.LastPasswordChangeDate = changeDate;
            return TaskCache.CompletedTask;
        }

        /// <summary>
        /// Gets the last password change date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the password hash for the specified <paramref name="user"/>.</returns>
        public virtual Task<DateTimeOffset?> GetLastPasswordChangeDateAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.LastPasswordChangeDate);
        }

        /// <summary>
        /// Sets the creation date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="creationDate">The date the user was created.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetCreateDateAsync(TUser user, DateTimeOffset? creationDate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.CreateDate = creationDate;
            return TaskCache.CompletedTask;
        }

        /// <summary>
        /// Gets the creation date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the password hash for the specified <paramref name="user"/>.</returns>
        public virtual Task<DateTimeOffset?> GetCreateDateAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.CreateDate);
        }

        /// <summary>
        /// Sets the last signin date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lastSignIn">The date the user last signed in.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetLastSignInDateAsync(TUser user, DateTimeOffset? lastSignIn, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.LastSignInDate = lastSignIn;
            return TaskCache.CompletedTask;
        }

        /// <summary>
        /// Gets the last signin date for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, returning the password hash for the specified <paramref name="user"/>.</returns>
        public virtual Task<DateTimeOffset?> GetLastSignInDateAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.LastSignInDate);
        }

    }
}
