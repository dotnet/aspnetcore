// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes basic user management apis
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserStore<TUser> : IDisposable where TUser : class
    {
        /// <summary>
        ///     Returns the user id for a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the user's name
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Set the user name
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetUserNameAsync(TUser user, string userName,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Insert a new user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     UpdateAsync a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     DeleteAsync a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Finds a user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns the user associated with this name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TUser> FindByNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken));
    }
}