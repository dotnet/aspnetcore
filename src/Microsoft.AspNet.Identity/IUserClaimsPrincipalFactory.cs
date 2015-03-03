// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface for creating a ClaimsPrincipal from an user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserClaimsPrincipalFactory<TUser>
        where TUser : class
    {
        /// <summary>
        ///     Create a ClaimsPrincipal from an user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <returns></returns>
        Task<ClaimsPrincipal> CreateAsync(TUser user);
    }
}