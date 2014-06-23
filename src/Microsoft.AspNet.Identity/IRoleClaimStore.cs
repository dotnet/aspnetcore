// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores role specific claims
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public interface IRoleClaimStore<TRole> : IRoleStore<TRole> where TRole : class
    {
        /// <summary>
        ///     Returns the claims for the role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<Claim>> GetClaimsAsync(TRole role, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Add a new role claim
        /// </summary>
        /// <param name="role"></param>
        /// <param name="claim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Remove a role claim
        /// </summary>
        /// <param name="role"></param>
        /// <param name="claim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RemoveClaimAsync(TRole role, Claim claim, 
            CancellationToken cancellationToken = default(CancellationToken));
    }
}