// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Update a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     DeleteAsync a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));

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
        Task SetRoleNameAsync(TRole role, string roleName, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get a role's normalized name
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetNormalizedRoleNameAsync(TRole role, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Set a role's normalized name
        /// </summary>
        /// <param name="role"></param>
        /// <param name="normalizedName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, 
            CancellationToken cancellationToken = default(CancellationToken));


        /// <summary>
        ///     Finds a role by id
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Find a role by normalized name
        /// </summary>
        /// <param name="normalizedRoleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));
    }
}