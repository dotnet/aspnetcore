// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface for creating a ClaimsIdentity from an user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IClaimsIdentityFactory<TUser>
        where TUser : class
    {
        /// <summary>
        ///     Create a ClaimsIdentity from an user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ClaimsIdentity> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
    }
}