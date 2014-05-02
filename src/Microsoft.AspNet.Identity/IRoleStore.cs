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
    ///     Interface that exposes basic role management
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public interface IRoleStore<TRole> : IDisposable where TRole : class
    {
        /// <summary>
        ///     Insert a new role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Update a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     DeleteAsync a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns a role's id
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns a role's name
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Set a role's name
        /// </summary>
        /// <param name="role"></param>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Finds a role by id
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Find a role by name
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TRole> FindByNameAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken));
    }
}