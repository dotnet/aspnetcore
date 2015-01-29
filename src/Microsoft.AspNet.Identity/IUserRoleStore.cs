// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that maps users to their roles
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserRoleStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Adds a user to role
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddToRoleAsync(TUser user, string roleName,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Removes the role for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RemoveFromRoleAsync(TUser user, string roleName,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns the roles for this user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<string>> GetRolesAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns true if a user is in a role
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> IsInRoleAsync(TUser user, string roleName,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns all users in given role
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken));
    }
}