// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores user specific claims
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserClaimStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Returns the claims for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<Claim>> GetClaimsAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Add a new user claim
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claims"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Updates the give claim information with the given new claim information
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claim"></param>
        /// <param name="newClaim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Remove a user claim
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claims"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get users having a specific claim
        /// </summary>
        /// <param name="claim">Claim to look up</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken));
    }
}