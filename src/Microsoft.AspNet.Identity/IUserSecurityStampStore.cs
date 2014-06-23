// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's security stamp
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserSecurityStampStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Set the security stamp for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="stamp"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetSecurityStampAsync(TUser user, string stamp, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Get the user security stamp
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetSecurityStampAsync(TUser user, 
            CancellationToken cancellationToken = default(CancellationToken));
    }
}