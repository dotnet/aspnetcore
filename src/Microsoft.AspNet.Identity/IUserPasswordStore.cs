// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's password hash
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserPasswordStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Set the user password hash
        /// </summary>
        /// <param name="user"></param>
        /// <param name="passwordHash"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetPasswordHashAsync(TUser user, string passwordHash,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Get the user password hash
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetPasswordHashAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns true if a user has a password set
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
    }
}