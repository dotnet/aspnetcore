// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Stores information which can be used to implement account lockout, including access failures and lockout status
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserLockoutStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        /// Returns the DateTimeOffset that represents the end of a user's lockout, any time in the past should be 
        /// considered not locked out.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Locks a user out until the specified end date (set to a past date, to unlock a user)
        /// </summary>
        /// <param name="user"></param>
        /// <param name="lockoutEnd"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Used to record when an attempt to access the user has failed
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> IncrementAccessFailedCountAsync(TUser user, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Used to reset the account access count, typically after the account is successfully accessed
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the current number of failed access attempts.  This number usually will be reset whenever the 
        /// password is verified or the account is locked out.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> GetAccessFailedCountAsync(TUser user, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns whether the user can be locked out.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> GetLockoutEnabledAsync(TUser user, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Sets whether the user can be locked out.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="enabled"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetLockoutEnabledAsync(TUser user, bool enabled, 
            CancellationToken cancellationToken = default(CancellationToken));
    }
}