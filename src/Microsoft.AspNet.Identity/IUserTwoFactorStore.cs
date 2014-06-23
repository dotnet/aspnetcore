// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores whether two factor is enabled for a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserTwoFactorStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Sets whether two factor is enabled for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="enabled"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetTwoFactorEnabledAsync(TUser user, bool enabled, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns whether two factor is enabled for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> GetTwoFactorEnabledAsync(TUser user, 
            CancellationToken cancellationToken = default(CancellationToken));
    }
}