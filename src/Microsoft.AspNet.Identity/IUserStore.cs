// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes basic user management apis
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
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
        Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken));

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